using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using CoreLocation;
using Foundation;
using UIKit;
using Plugin.Geolocator.Abstractions;


namespace Plugin.Geolocator
{
    /// <summary>
    /// Implementation for Geolocator
    /// </summary>
    [Preserve(AllMembers = true)]
    public class GeolocatorImplementation : IGeolocator
    {
        bool deferringUpdates;
        readonly CLLocationManager manager;
        bool isListening;
        Position position;
        ListenerSettings listenerSettings;

        public GeolocatorImplementation()
        {
            DesiredAccuracy = 100;
            manager = GetManager();
            manager.AuthorizationChanged += OnAuthorizationChanged;
            manager.Failed += OnFailed;

            if (UIDevice.CurrentDevice.CheckSystemVersion(6, 0))
                manager.LocationsUpdated += OnLocationsUpdated;
            else
                manager.UpdatedLocation += OnUpdatedLocation;

            manager.UpdatedHeading += OnUpdatedHeading;

            manager.DeferredUpdatesFinished += OnDeferredUpdatedFinished;

            RequestAuthorization();
        }

        void OnDeferredUpdatedFinished(object sender, NSErrorEventArgs e) => deferringUpdates = false;

        bool CanDeferLocationUpdate => UIDevice.CurrentDevice.CheckSystemVersion(6, 0);


        /// <summary>
        /// Position error event handler
        /// </summary>
        public event EventHandler<PositionErrorEventArgs> PositionError;

        /// <summary>
        /// Position changed event handler
        /// </summary>
        public event EventHandler<PositionEventArgs> PositionChanged;

        /// <summary>
        /// Desired accuracy in meters
        /// </summary>
        public double DesiredAccuracy
        {
            get;
            set;
        }

        /// <summary>
        /// Gets if you are listening for location changes
        ///
        public bool IsListening => isListening;

        /// <summary>
        /// Gets if device supports heading
        /// </summary>
        public bool SupportsHeading => CLLocationManager.HeadingAvailable;


        /// <summary>
        /// Gets if geolocation is available on device
        /// </summary>
        public bool IsGeolocationAvailable => true; //all iOS devices support Geolocation

        /// <summary>
        /// Gets if geolocation is enabled on device
        /// </summary>
        public bool IsGeolocationEnabled
        {
            get
            {
                var status = CLLocationManager.Status;

                if (UIDevice.CurrentDevice.CheckSystemVersion(8, 0))
                {
                    return status == CLAuthorizationStatus.AuthorizedAlways
                    || status == CLAuthorizationStatus.AuthorizedWhenInUse;
                }

                return status == CLAuthorizationStatus.Authorized;
            }
        }

        void RequestAuthorization()
        {
            var info = NSBundle.MainBundle.InfoDictionary;

            if (UIDevice.CurrentDevice.CheckSystemVersion(8, 0))
            {
                if (info.ContainsKey(new NSString("NSLocationWhenInUseUsageDescription")))
                    manager.RequestWhenInUseAuthorization();
                else if (info.ContainsKey(new NSString("NSLocationAlwaysUsageDescription")))
                    manager.RequestAlwaysAuthorization();
                else
                    throw new UnauthorizedAccessException("On iOS 8.0 and higher you must set either NSLocationWhenInUseUsageDescription or NSLocationAlwaysUsageDescription in your Info.plist file to enable Authorization Requests for Location updates!");
            }
        }

        /// <summary>
        /// Gets the last known and most accurate location.
        /// This is usually cached and best to display first before querying for full position.
        /// </summary>
        /// <returns>Best and most recent location or null if none found</returns>
        public Task<Position> GetLastKnownLocationAsync()
        {
            var m = GetManager();
            var newLocation = m?.Location;

            if (newLocation == null)
                return null;

            var position = new Position();
            position.Accuracy = newLocation.HorizontalAccuracy;
            position.Altitude = newLocation.Altitude;
            position.AltitudeAccuracy = newLocation.VerticalAccuracy;
            position.Latitude = newLocation.Coordinate.Latitude;
            position.Longitude = newLocation.Coordinate.Longitude;
            position.Speed = newLocation.Speed;
            position.Timestamp = new DateTimeOffset((DateTime)newLocation.Timestamp);

            return Task.FromResult(position);
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

            if (timeoutMilliseconds <= 0 && timeoutMilliseconds != Timeout.Infinite)
                throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be positive or Timeout.Infinite");

            if (!cancelToken.HasValue)
                cancelToken = CancellationToken.None;

            TaskCompletionSource<Position> tcs;
            if (!IsListening)
            {
                var m = GetManager();

                // permit background updates if background location mode is enabled
                if (UIDevice.CurrentDevice.CheckSystemVersion(9, 0))
                {
                    NSArray backgroundModes = NSBundle.MainBundle.InfoDictionary[(NSString)"UIBackgroundModes"] as NSArray;
                    m.AllowsBackgroundLocationUpdates = backgroundModes != null && (backgroundModes.Contains((NSString)"Location") || backgroundModes.Contains((NSString)"location"));
                }

                // always prevent location update pausing since we're only listening for a single update.
                if (UIDevice.CurrentDevice.CheckSystemVersion(6, 0))
                    m.PausesLocationUpdatesAutomatically = false;

                tcs = new TaskCompletionSource<Position>(m);
                var singleListener = new GeolocationSingleUpdateDelegate(m, DesiredAccuracy, includeHeading, timeoutMilliseconds, cancelToken.Value);
                m.Delegate = singleListener;

                m.StartUpdatingLocation();
                if (includeHeading && SupportsHeading)
                    m.StartUpdatingHeading();

                return singleListener.Task;
            }


            tcs = new TaskCompletionSource<Position>();
            if (position == null)
            {
                if (cancelToken != CancellationToken.None)
                {
                    cancelToken.Value.Register(() => tcs.TrySetCanceled());
                }

                EventHandler<PositionErrorEventArgs> gotError = null;
                gotError = (s, e) =>
                {
                    tcs.TrySetException(new GeolocationException(e.Error));
                    PositionError -= gotError;
                };

                PositionError += gotError;

                EventHandler<PositionEventArgs> gotPosition = null;
                gotPosition = (s, e) =>
                {
                    tcs.TrySetResult(e.Position);
                    PositionChanged -= gotPosition;
                };

                PositionChanged += gotPosition;
            }
            else
                tcs.SetResult(position);


            return tcs.Task;
        }

        /// <summary>
        /// Retrieve addresses for position.
        /// </summary>
        /// <param name="location">Desired position (latitude and longitude)</param>
        /// <returns>Addresses of the desired position</returns>
        public async Task<IEnumerable<Address>> GetAddressesForPositionAsync(Position location)
        {
            if (location == null)
                return null;

            var geocoder = new CLGeocoder();
            var addressList = await geocoder.ReverseGeocodeLocationAsync(new CLLocation(location.Latitude, location.Longitude));
            return addressList.ToAddresses();
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
            if (isListening)
                throw new InvalidOperationException("Already listening");

            // if no settings were passed in, instantiate the default settings. need to check this and create default settings since
            // previous calls to StartListeningAsync might have already configured the location manager in a non-default way that the
            // caller of this method might not be expecting. the caller should expect the defaults if they pass no settings.
            if (settings == null)
                settings = new ListenerSettings();

            // keep reference to settings so that we can stop the listener appropriately later
            listenerSettings = settings;

            var desiredAccuracy = DesiredAccuracy;

            #region apply settings to location manager
            // set background flag
            if (UIDevice.CurrentDevice.CheckSystemVersion(9, 0))
                manager.AllowsBackgroundLocationUpdates = settings.AllowBackgroundUpdates;

            // configure location update pausing
            if (UIDevice.CurrentDevice.CheckSystemVersion(6, 0))
            {
                manager.PausesLocationUpdatesAutomatically = settings.PauseLocationUpdatesAutomatically;

                if (settings.ActivityType == ActivityType.AutomotiveNavigation)
                    manager.ActivityType = CLActivityType.AutomotiveNavigation;
                else if (settings.ActivityType == ActivityType.Fitness)
                    manager.ActivityType = CLActivityType.Fitness;
                else if (settings.ActivityType == ActivityType.Other)
                    manager.ActivityType = CLActivityType.Other;
                else if (settings.ActivityType == ActivityType.OtherNavigation)
                    manager.ActivityType = CLActivityType.OtherNavigation;
            }

            // to use deferral, CLLocationManager.DistanceFilter must be set to CLLocationDistance.None, and CLLocationManager.DesiredAccuracy must be 
            // either CLLocation.AccuracyBest or CLLocation.AccuracyBestForNavigation. deferral only available on iOS 6.0 and above.
            if (CanDeferLocationUpdate && settings.DeferLocationUpdates)
            {
                minDistance = CLLocationDistance.FilterNone;
                desiredAccuracy = CLLocation.AccuracyBest;
            }
            #endregion

            isListening = true;
            manager.DesiredAccuracy = desiredAccuracy;
            manager.DistanceFilter = minDistance;

            if (settings.ListenForSignificantChanges)
                manager.StartMonitoringSignificantLocationChanges();
            else
                manager.StartUpdatingLocation();

            if (includeHeading && CLLocationManager.HeadingAvailable)
                manager.StartUpdatingHeading();

            return Task.FromResult(true);
        }

        /// <summary>
        /// Stop listening
        /// </summary>
        public Task<bool> StopListeningAsync()
        {
            if (!isListening)
                return Task.FromResult(true);

            isListening = false;
            if (CLLocationManager.HeadingAvailable)
                manager.StopUpdatingHeading();

            // it looks like deferred location updates can apply to the standard service or significant change service. disallow deferral in either case.
            if ((listenerSettings?.DeferLocationUpdates ?? false) && CanDeferLocationUpdate)
                manager.DisallowDeferredLocationUpdates();

            if (listenerSettings?.ListenForSignificantChanges ?? false)
                manager.StopMonitoringSignificantLocationChanges();
            else
                manager.StopUpdatingLocation();

            listenerSettings = null;
            position = null;

            return Task.FromResult(true);
        }

        CLLocationManager GetManager()
        {
            CLLocationManager m = null;
            new NSObject().InvokeOnMainThread(() => m = new CLLocationManager());
            return m;
        }

        void OnUpdatedHeading(object sender, CLHeadingUpdatedEventArgs e)
        {
            if (e.NewHeading.TrueHeading == -1)
                return;

            var p = (position == null) ? new Position() : new Position(position);

            p.Heading = e.NewHeading.TrueHeading;

            position = p;

            OnPositionChanged(new PositionEventArgs(p));
        }

        void OnLocationsUpdated(object sender, CLLocationsUpdatedEventArgs e)
        {
            foreach (CLLocation location in e.Locations)
                UpdatePosition(location);

            // defer future location updates if requested
            if ((listenerSettings?.DeferLocationUpdates ?? false) && !deferringUpdates && CanDeferLocationUpdate)
            {
                manager.AllowDeferredLocationUpdatesUntil(listenerSettings.DeferralDistanceMeters == null ? CLLocationDistance.MaxDistance : listenerSettings.DeferralDistanceMeters.GetValueOrDefault(),
                    listenerSettings.DeferralTime == null ? CLLocationManager.MaxTimeInterval : listenerSettings.DeferralTime.GetValueOrDefault().TotalSeconds);

                deferringUpdates = true;
            }
        }

        void OnUpdatedLocation(object sender, CLLocationUpdatedEventArgs e) => UpdatePosition(e.NewLocation);


        void UpdatePosition(CLLocation location)
        {
            var p = (position == null) ? new Position() : new Position(this.position);

            if (location.HorizontalAccuracy > -1)
            {
                p.Accuracy = location.HorizontalAccuracy;
                p.Latitude = location.Coordinate.Latitude;
                p.Longitude = location.Coordinate.Longitude;
            }

            if (location.VerticalAccuracy > -1)
            {
                p.Altitude = location.Altitude;
                p.AltitudeAccuracy = location.VerticalAccuracy;
            }

            if (location.Speed > -1)
                p.Speed = location.Speed;

            var dateTime = (DateTime)location.Timestamp;
            p.Timestamp = new DateTimeOffset(dateTime);

            position = p;

            OnPositionChanged(new PositionEventArgs(p));

            location.Dispose();
        }


        void OnFailed(object sender, NSErrorEventArgs e)
        {
            if ((CLError)(int)e.Error.Code == CLError.Network)
                OnPositionError(new PositionErrorEventArgs(GeolocationError.PositionUnavailable));
        }

        void OnAuthorizationChanged(object sender, CLAuthorizationChangedEventArgs e)
        {
            if (e.Status == CLAuthorizationStatus.Denied || e.Status == CLAuthorizationStatus.Restricted)
                OnPositionError(new PositionErrorEventArgs(GeolocationError.Unauthorized));
        }

        void OnPositionChanged(PositionEventArgs e) => PositionChanged?.Invoke(this, e);


        async void OnPositionError(PositionErrorEventArgs e)
        {
            await StopListeningAsync();
            PositionError?.Invoke(this, e);
        }
    }
}