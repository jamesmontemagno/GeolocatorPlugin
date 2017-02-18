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

        /// <inheritdoc/>
        public bool IsGeolocationAvailable => true;

        /// <inheritdoc/>
        public bool IsGeolocationEnabled
        {
            get
            {
                if (watcher != null)
                    isEnabled = (this.watcher.Permission == GeoPositionPermission.Granted && watcher.Status != GeoPositionStatus.Disabled);
                else
                    isEnabled = GetEnabled();

                return isEnabled;
            }
        }
        /// <inheritdoc/>
        public double DesiredAccuracy
        {
            get;
            set;
        }

        /// <inheritdoc/>
        public bool SupportsHeading => true;

        /// <inheritdoc/>
        public bool IsListening => watcher != null;


        /// <inheritdoc/>
        public Task<Position> GetPositionAsync(TimeSpan? timeout, CancellationToken? cancelToken = null, bool includeHeading = false)
        {
            var timeoutMilliseconds = timeout.HasValue ? (int)timeout.Value.TotalMilliseconds : Timeout.Infinite;

            if (!cancelToken.HasValue)
                cancelToken = CancellationToken.None;

            if (timeoutMilliseconds <= 0 && timeoutMilliseconds != Timeout.Infinite)
                throw new ArgumentOutOfRangeException(nameof(timeout), "timeout must be greater than or equal to 0");

            return new SinglePositionListener(DesiredAccuracy, timeoutMilliseconds, cancelToken.Value).Task;
        }
        /// <inheritdoc/>
        public Task<bool> StartListeningAsync(TimeSpan minTime, double minDistance, bool includeHeading = false, ListenerSettings settings = null)
        {
            if (minDistance < 0)
                throw new ArgumentOutOfRangeException("minDistance");
            if (IsListening)
                throw new InvalidOperationException("This Geolocator is already listening");

            watcher = new GeoCoordinateWatcher(GetAccuracy(DesiredAccuracy));
            watcher.MovementThreshold = minDistance;
            watcher.PositionChanged += WatcherOnPositionChanged;
            watcher.StatusChanged += WatcherOnStatusChanged;
            watcher.Start();

            return Task.FromResult(true);
        }
        /// <inheritdoc/>
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


        private static bool GetEnabled()
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

        private async void WatcherOnStatusChanged(object sender, GeoPositionStatusChangedEventArgs e)
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

        private void WatcherOnPositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            Position p = GetPosition(e.Position);
            if (p == null)
                return;

            PositionChanged?.Invoke(this, new PositionEventArgs(p));

        }

        internal static GeoPositionAccuracy GetAccuracy(double desiredAccuracy)
        {
            if (desiredAccuracy < 100)
                return GeoPositionAccuracy.High;

            return GeoPositionAccuracy.Default;
        }

        internal static Position GetPosition(GeoPosition<GeoCoordinate> position)
        {
            if (position.Location.IsUnknown)
                return null;

            var p = new Position();
            p.Accuracy = position.Location.HorizontalAccuracy;
            p.Longitude = position.Location.Longitude;
            p.Latitude = position.Location.Latitude;

            if (!Double.IsNaN(position.Location.VerticalAccuracy) && !Double.IsNaN(position.Location.Altitude))
            {
                p.AltitudeAccuracy = position.Location.VerticalAccuracy;
                p.Altitude = position.Location.Altitude;
            }

            if (!Double.IsNaN(position.Location.Course))
                p.Heading = position.Location.Course;

            if (!Double.IsNaN(position.Location.Speed))
                p.Speed = position.Location.Speed;

            p.Timestamp = position.Timestamp.ToUniversalTime();

            return p;
        }
    }

}