//
//  Copyright 2011-2013, Xamarin Inc.
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//

using System;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Plugin.Geolocator.Abstractions;
using System.Threading;

namespace Plugin.Geolocator
{
    /// <summary>
    /// Implementation for Geolocator
    /// </summary>
    public class GeolocatorImplementation : IGeolocator
    {

        bool isListening;
        double desiredAccuracy;
        Windows.Devices.Geolocation.Geolocator locator = new Windows.Devices.Geolocation.Geolocator();


        public GeolocatorImplementation()
        {
            DesiredAccuracy = 100;
        }

        /// <summary>
        /// Position error event handler
        /// </summary>
        public event EventHandler<PositionEventArgs> PositionChanged;

        /// <summary>
        /// Position changed event handler
        /// </summary>
        public event EventHandler<PositionErrorEventArgs> PositionError;

        /// <summary>
        /// Gets if device supports heading
        /// </summary>
        public bool SupportsHeading => false;

        /// <summary>
        /// Gets if geolocation is available on device
        /// </summary>
        public bool IsGeolocationAvailable
        {
            get
            {
                PositionStatus status = GetGeolocatorStatus();

                while (status == PositionStatus.Initializing)
                {
                    Task.Delay(10).Wait();
                    status = GetGeolocatorStatus();
                }

                return status != PositionStatus.NotAvailable;
            }
        }

        /// <summary>
        /// Gets if geolocation is enabled on device
        /// </summary>
        public bool IsGeolocationEnabled
        {
            get
            {
                PositionStatus status = GetGeolocatorStatus();

                while (status == PositionStatus.Initializing)
                {
                    Task.Delay(10).Wait();
                    status = GetGeolocatorStatus();
                }

                return status != PositionStatus.Disabled && status != PositionStatus.NotAvailable;
            }
        }

        /// <summary>
        /// Desired accuracy in meters
        /// </summary>
        public double DesiredAccuracy
        {
            get { return desiredAccuracy; }
            set
            {
                desiredAccuracy = value;
                GetGeolocator().DesiredAccuracy = (value < 100) ? PositionAccuracy.High : PositionAccuracy.Default;
            }
        }

        /// <summary>
        /// Gets if you are listening for location changes
        /// </summary>
        public bool IsListening => isListening;


        /// <summary>
        /// Gets the last known and most accurate location.
        /// This is usually cached and best to display first before querying for full position.
        /// </summary>
        /// <returns>Best and most recent location or null if none found</returns>
        public Task<Position> GetLastKnownLocationAsync()
        {
            return null;
        }

        /// <summary>
        /// Gets position async with specified parameters
        /// </summary>
        /// <param name="timeout">Timeout to wait, Default Infinite</param>
        /// <param name="token">Cancelation token</param>
        /// <param name="includeHeading">If you would like to include heading</param>
        /// <returns>Position</returns>
        public Task<Position> GetPositionAsync(TimeSpan? timeout, CancellationToken? cancelToken = null, bool includeHeading = false)
        {
            var timeoutMilliseconds = timeout.HasValue ? (int)timeout.Value.TotalMilliseconds : Timeout.Infite;

            if (timeoutMilliseconds < 0 && timeoutMilliseconds != Timeout.Infite)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            if (!cancelToken.HasValue)
                cancelToken = CancellationToken.None;

            IAsyncOperation<Geoposition> pos = GetGeolocator().GetGeopositionAsync(TimeSpan.FromTicks(0), TimeSpan.FromDays(365));
            cancelToken.Value.Register(o => ((IAsyncOperation<Geoposition>)o).Cancel(), pos);


            var timer = new Timeout(timeoutMilliseconds, pos.Cancel);

            var tcs = new TaskCompletionSource<Position>();

            pos.Completed = (op, s) =>
            {
                timer.Cancel();

                switch (s)
                {
                    case AsyncStatus.Canceled:
                        tcs.SetCanceled();
                        break;
                    case AsyncStatus.Completed:
                        tcs.SetResult(GetPosition(op.GetResults()));
                        break;
                    case AsyncStatus.Error:
                        Exception ex = op.ErrorCode;
                        if (ex is UnauthorizedAccessException)
                            ex = new GeolocationException(GeolocationError.Unauthorized, ex);

                        tcs.SetException(ex);
                        break;
                }
            };

            return tcs.Task;
        }

        /// <summary>
		/// Start listening for changes
		/// </summary>
		/// <param name="minimumTime">Time</param>
		/// <param name="minimumDistance">Distance</param>
		/// <param name="includeHeading">Include heading or not</param>
		/// <param name="listenerSettings">Optional settings (iOS only)</param>
		public Task<bool> StartListeningAsync(TimeSpan minTime, double minDistance, bool includeHeading = false, ListenerSettings settings = null)
        {
            
            if (minTime.TotalMilliseconds < 0)
                throw new ArgumentOutOfRangeException("minTime");
            if (minDistance < 0)
                throw new ArgumentOutOfRangeException("minDistance");
            if (isListening)
                throw new InvalidOperationException();

            isListening = true;

            var loc = GetGeolocator();
            loc.ReportInterval = (uint)minTime.TotalMilliseconds;
            loc.MovementThreshold = minDistance;
            loc.PositionChanged += OnLocatorPositionChanged;
            loc.StatusChanged += OnLocatorStatusChanged;

            return Task.FromResult(true);
        }

        /// <summary>
        /// Stop listening
        /// </summary>
        public Task<bool> StopListeningAsync()
        {
            if (!isListening)
                return Task.FromResult(true);

            locator.PositionChanged -= OnLocatorPositionChanged;
            locator.StatusChanged -= OnLocatorStatusChanged;
            isListening = false;

            return Task.FromResult(true);
        }

      
        private async void OnLocatorStatusChanged(Windows.Devices.Geolocation.Geolocator sender, StatusChangedEventArgs e)
        {
            GeolocationError error;
            switch (e.Status)
            {
                case PositionStatus.Disabled:
                    error = GeolocationError.Unauthorized;
                    break;

                case PositionStatus.NoData:
                    error = GeolocationError.PositionUnavailable;
                    break;

                default:
                    return;
            }

            if (isListening)
            {
                await StopListeningAsync();
                OnPositionError(new PositionErrorEventArgs(error));
            }

            locator = null;
        }

        private void OnLocatorPositionChanged(Windows.Devices.Geolocation.Geolocator sender, PositionChangedEventArgs e)
        {
            OnPositionChanged(new PositionEventArgs(GetPosition(e.Position)));
        }

        private void OnPositionChanged(PositionEventArgs e) => PositionChanged?.Invoke(this, e);
        

        private void OnPositionError(PositionErrorEventArgs e) => PositionError?.Invoke(this, e);
        

        private Windows.Devices.Geolocation.Geolocator GetGeolocator()
        {
            var loc = locator;
            if (loc == null)
            {
                locator = new Windows.Devices.Geolocation.Geolocator();
                locator.StatusChanged += OnLocatorStatusChanged;
                loc = locator;
            }

            return loc;
        }

        private PositionStatus GetGeolocatorStatus()
        {
            var loc = GetGeolocator();
            return loc.LocationStatus;
        }

        private static Position GetPosition(Geoposition position)
        {
            var pos = new Position
            {
                Accuracy = position.Coordinate.Accuracy,
                Latitude = position.Coordinate.Point.Position.Latitude,
                Longitude = position.Coordinate.Point.Position.Longitude,
                Timestamp = position.Coordinate.Timestamp.ToUniversalTime(),
            };

            if (position.Coordinate.Heading != null)
                pos.Heading = position.Coordinate.Heading.Value;

            if (position.Coordinate.Speed != null)
                pos.Speed = position.Coordinate.Speed.Value;

            if (position.Coordinate.AltitudeAccuracy.HasValue)
                pos.AltitudeAccuracy = position.Coordinate.AltitudeAccuracy.Value;

            pos.Altitude = position.Coordinate.Point.Position.Altitude;

            return pos;
        }
    }
}
