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
using System.Device.Location;
using System.Threading;
using System.Threading.Tasks;
using Plugin.Geolocator.Abstractions;


namespace Plugin.Geolocator
{
    /// <summary>
    /// Implementation for Geolocator
    /// </summary>
    public class GeolocatorImplementation : IGeolocator
    {

        GeoCoordinateWatcher watcher;
        bool isEnabled;

        public GeolocatorImplementation()
        {
            DesiredAccuracy = 100;
        }

        public event EventHandler<PositionErrorEventArgs> PositionError;
        public event EventHandler<PositionEventArgs> PositionChanged;

        /// <summary>
        /// Gets if geolocation is available on device
        /// </summary>
        public bool IsGeolocationAvailable => true;

        /// <summary>
        /// Gets if geolocation is enabled on device
        /// </summary>
        public bool IsGeolocationEnabled
        {
            get
            {
                if (watcher != null)
                    isEnabled = (watcher.Permission == GeoPositionPermission.Granted && watcher.Status != GeoPositionStatus.Disabled);
                else
                    isEnabled = GetEnabled();

                return isEnabled;
            }
        }

        /// <summary>
        /// Desired accuracy in meters
        /// </summary>
        public double DesiredAccuracy
        {
            get;
            set;
        }

        /// <summary>
        /// Gets if device supports heading
        /// </summary>
        public bool SupportsHeading => true;

        /// <summary>
        /// Gets if you are listening for location changes
        /// </summary>
        public bool IsListening => watcher != null;


        /// <summary>
        /// Gets the last known and most accurate location.
        /// This is usually cached and best to display first before querying for full position.
        /// </summary>
        /// <returns>Best and most recent location or null if none found</returns>
        public Task<Position> GetLastKnownLocationAsync()
        {
            var watch = new GeoCoordinateWatcher();
            return Task.FromResult(watch.Position?.ToPosition() ?? null);
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
            var timeoutMilliseconds = timeout.HasValue ? (int)timeout.Value.TotalMilliseconds : Timeout.Infinite;

            if (!cancelToken.HasValue)
                cancelToken = CancellationToken.None;

            if (timeoutMilliseconds <= 0 && timeoutMilliseconds != Timeout.Infinite)
                throw new ArgumentOutOfRangeException(nameof(timeout), "timeout must be greater than or equal to 0");

            return new SinglePositionListener(DesiredAccuracy, timeoutMilliseconds, cancelToken.Value).Task;
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
            if (minDistance < 0)
                throw new ArgumentOutOfRangeException("minDistance");
            if (IsListening)
                throw new InvalidOperationException("This Geolocator is already listening");

            watcher = new GeoCoordinateWatcher(DesiredAccuracy.ToAccuracy());
            watcher.MovementThreshold = minDistance;
            watcher.PositionChanged += WatcherOnPositionChanged;
            watcher.StatusChanged += WatcherOnStatusChanged;
            watcher.Start();

            return Task.FromResult(true);
        }

        /// <summary>
        /// Stop listening
        /// </summary>
        public Task<bool> StopListeningAsync()
        {
            if (watcher == null)
                return Task.FromResult(true);

            watcher.PositionChanged -= WatcherOnPositionChanged;
            watcher.StatusChanged -= WatcherOnStatusChanged;
            watcher.Stop();
            watcher.Dispose();
            watcher = null;

            return Task.FromResult(true);
        }


        static bool GetEnabled()
        {
            var w = new GeoCoordinateWatcher();
            try
            {
                w.Start(true);
                bool enabled = (w.Permission == GeoPositionPermission.Granted && w.Status != GeoPositionStatus.Disabled);
                w.Stop();

                return enabled;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                w.Dispose();
            }
        }

        async void WatcherOnStatusChanged(object sender, GeoPositionStatusChangedEventArgs e)
        {
            isEnabled = (watcher.Permission == GeoPositionPermission.Granted && watcher.Status != GeoPositionStatus.Disabled);

            GeolocationError error;
            switch (e.Status)
            {
                case GeoPositionStatus.Disabled:
                    error = GeolocationError.Unauthorized;
                    break;

                case GeoPositionStatus.NoData:
                    error = GeolocationError.PositionUnavailable;
                    break;

                default:
                    return;
            }

            await StopListeningAsync();

            PositionError?.Invoke(this, new PositionErrorEventArgs(error));
        }

        void WatcherOnPositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            var p = e.Position.ToPosition();
            if (p == null)
                return;

            PositionChanged?.Invoke(this, new PositionEventArgs(p));

        }
    }

}