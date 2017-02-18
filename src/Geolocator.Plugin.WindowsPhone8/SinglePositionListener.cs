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

using Plugin.Geolocator.Abstractions;
using System;
using System.Device.Location;
using System.Threading;
using System.Threading.Tasks;


namespace Plugin.Geolocator
{
    internal class SinglePositionListener
    {
        GeoPosition<GeoCoordinate> bestPosition;
        GeoCoordinateWatcher watcher;
        readonly double desiredAccuracy;
        readonly DateTimeOffset start;
        readonly Timer timer;
        readonly int timeout;
        readonly TaskCompletionSource<Position> tcs = new TaskCompletionSource<Position>();


        internal SinglePositionListener(double accuracy, int timeout, CancellationToken cancelToken)
        {
            cancelToken.Register(HandleTimeout, true);
            desiredAccuracy = accuracy;
            start = DateTime.Now;
            this.timeout = timeout;

            System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                watcher = new GeoCoordinateWatcher(accuracy.ToAccuracy());
                watcher.PositionChanged += WatcherOnPositionChanged;
                watcher.StatusChanged += WatcherOnStatusChanged;

                watcher.Start();
            });

            if (timeout != Timeout.Infinite)
                timer = new Timer(HandleTimeout, null, timeout, Timeout.Infinite);

            Task.ContinueWith(Cleanup);
        }

        public Task<Position> Task =>  tcs.Task;

      
        private void Cleanup(Task task)
        {
            watcher.PositionChanged -= WatcherOnPositionChanged;
            watcher.StatusChanged -= WatcherOnStatusChanged;

            watcher.Stop();
            watcher.Dispose();

            timer?.Dispose();
        }

        private void HandleTimeout(object state)
        {
            if (state != null && (bool)state)
                tcs.TrySetCanceled();

            if (bestPosition != null)
                tcs.TrySetResult(bestPosition.ToPosition());
            else
                tcs.TrySetCanceled();
        }

        private void WatcherOnStatusChanged(object sender, GeoPositionStatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case GeoPositionStatus.NoData:
                    tcs.TrySetException(new GeolocationException(GeolocationError.PositionUnavailable));
                    break;

                case GeoPositionStatus.Disabled:
                    tcs.TrySetException(new GeolocationException(GeolocationError.Unauthorized));
                    break;
            }
        }

        private void WatcherOnPositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            if (e.Position.Location.IsUnknown)
                return;

            bool isRecent = timeout == Timeout.Infinite || (e.Position.Timestamp - this.start).TotalMilliseconds < this.timeout;

            if (e.Position.Location.HorizontalAccuracy <= desiredAccuracy && isRecent)
                tcs.TrySetResult(e.Position.ToPosition());

            if (bestPosition == null || e.Position.Location.HorizontalAccuracy < this.bestPosition.Location.HorizontalAccuracy)
                bestPosition = e.Position;
        }
    }
}
