
using System;
using CoreLocation;
using Foundation;
using System.Threading.Tasks;
using System.Threading;
using Plugin.Geolocator.Abstractions;
using System.Linq;

namespace Plugin.Geolocator
{
    [Preserve(AllMembers = true)]
    internal class GeolocationSingleUpdateDelegate : CLLocationManagerDelegate
    {


        bool haveHeading;
        bool haveLocation;
        readonly Position position = new Position();
#if __IOS__
        CLHeading bestHeading;
#endif

        readonly double desiredAccuracy;
        readonly bool includeHeading;
        readonly TaskCompletionSource<Position> tcs;
        readonly CLLocationManager manager;

        public GeolocationSingleUpdateDelegate(CLLocationManager manager, double desiredAccuracy, bool includeHeading, int timeout, CancellationToken cancelToken)
        {
            this.manager = manager;
            tcs = new TaskCompletionSource<Position>(manager);
            this.desiredAccuracy = desiredAccuracy;
            this.includeHeading = includeHeading;

            if (timeout != Timeout.Infinite)
            {
                Timer t = null;
                t = new Timer(s =>
                {
                    if (haveLocation)
                        tcs.TrySetResult(new Position(this.position));
                    else
                        tcs.TrySetCanceled();

                    StopListening();
                    t.Dispose();
                }, null, timeout, 0);
            }

            cancelToken.Register(() =>
            {
                StopListening();
                tcs.TrySetCanceled();
            });
        }

        public Task<Position> Task => tcs?.Task;


        public override void AuthorizationChanged(CLLocationManager manager, CLAuthorizationStatus status)
        {
            // If user has services disabled, we're just going to throw an exception for consistency.
            if (status == CLAuthorizationStatus.Denied || status == CLAuthorizationStatus.Restricted)
            {
                StopListening();
                tcs.TrySetException(new GeolocationException(GeolocationError.Unauthorized));
            }
        }

        public override void Failed(CLLocationManager manager, NSError error)
        {
            switch ((CLError)(int)error.Code)
            {
                case CLError.Network:
                    StopListening();
                    tcs.SetException(new GeolocationException(GeolocationError.PositionUnavailable));
                    break;
                case CLError.LocationUnknown:
                    StopListening();
                    tcs.TrySetException(new GeolocationException(GeolocationError.PositionUnavailable));
                    break;
            }
        }


#if __IOS__
        public override bool ShouldDisplayHeadingCalibration(CLLocationManager manager) => true;
#endif

#if __TVOS__
        public override void LocationsUpdated(CLLocationManager manager, CLLocation[] locations)
        {
            var newLocation = locations.FirstOrDefault();
            if (newLocation == null)
                return;

#else
        public override void UpdatedLocation(CLLocationManager manager, CLLocation newLocation, CLLocation oldLocation)
        {
#endif
            if (newLocation.HorizontalAccuracy < 0)
                return;

            if (haveLocation && newLocation.HorizontalAccuracy > position.Accuracy)
                return;

            position.Accuracy = newLocation.HorizontalAccuracy;
            position.Altitude = newLocation.Altitude;
            position.AltitudeAccuracy = newLocation.VerticalAccuracy;
            position.Latitude = newLocation.Coordinate.Latitude;
            position.Longitude = newLocation.Coordinate.Longitude;
#if __IOS__ || __MACOS__
            position.Speed = newLocation.Speed;
#endif
            try
            {
                position.Timestamp = new DateTimeOffset(newLocation.Timestamp.ToDateTime());
            }
            catch(Exception ex)
            {
                position.Timestamp = DateTimeOffset.UtcNow;
            }
            haveLocation = true;

            if ((!includeHeading || haveHeading) && position.Accuracy <= desiredAccuracy)
            {
                tcs.TrySetResult(new Position(position));
                StopListening();
            }
        }

#if __IOS__
        public override void UpdatedHeading(CLLocationManager manager, CLHeading newHeading)
        {
            if (newHeading.HeadingAccuracy < 0)
                return;
            if (bestHeading != null && newHeading.HeadingAccuracy >= bestHeading.HeadingAccuracy)
                return;

            bestHeading = newHeading;
            position.Heading = newHeading.TrueHeading;
            haveHeading = true;

            if (haveLocation && position.Accuracy <= desiredAccuracy)
            {
                tcs.TrySetResult(new Position(position));
                StopListening();
            }
        }
#endif


        private void StopListening()
        {
#if __IOS__
            if (CLLocationManager.HeadingAvailable)
                manager.StopUpdatingHeading();
#endif

            manager.StopUpdatingLocation();
        }
    }
}