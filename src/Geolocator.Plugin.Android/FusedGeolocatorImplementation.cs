using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.Gms.Common.Apis;
using Android.Gms.Location;
using Android.Locations;
using Android.OS;
using Android.Provider;
using Plugin.Geolocator.Abstractions;
using GmsLocation = Android.Gms.Location;
using Android.Runtime;
using Android.App;
using Android.Gms.Extensions;
using Plugin.CurrentActivity;
using Android.Support.V4.App;

namespace Plugin.Geolocator
{
    class FusedGeolocatorImplementation : Java.Lang.Object, GmsLocation.ILocationListener,
                                        IGeolocator
    {

        public FusedGeolocatorImplementation(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer)
        {
            context = Application.Context;
        }

        public FusedGeolocatorImplementation()
        {
            context = Application.Context;
        }



        readonly object positionSync = new object();
        readonly Context context;
        GoogleApiClient googleApiClient;
        Position lastKnownPosition;

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
        public double DesiredAccuracy { get; set; } = 100;

        /// <summary>
        /// Gets if you are listening for location changes
        /// </summary>
        public bool IsListening { get; private set; }

        /// <summary>
        /// Gets if device supports heading
        /// </summary>
        public bool SupportsHeading => true;

        /// <summary>
        /// Gets if geolocation is available on device
        /// </summary>
        public bool IsGeolocationEnabled => IsLocationServicesEnabled();


       
        /// <summary>
        /// Gets if geolocation is enabled on device
        /// </summary>
        public bool IsGeolocationAvailable
        {
            get
            {
                if (googleApiClient?.IsConnected == true)
                {
                    var avalibility = LocationServices.FusedLocationApi.GetLocationAvailability(googleApiClient);
                    if (avalibility != null)
                        return avalibility.IsLocationAvailable;
                }

                return IsLocationServicesEnabled();
            }
        }

        async Task Initialize()
        {
            if (googleApiClient?.IsConnected ?? false)
                return;

            var builder = new GoogleApiClient.Builder(context)
                                             .EnableAutoManage(CrossCurrentActivity.Current.Activity as FragmentActivity,
                                                               (obj) => 
							            {
							                
							            })
                                         .AddApi(LocationServices.API)
                                         .AddConnectionCallbacks((obj) =>
                                           {
                                               System.Diagnostics.Debug.WriteLine("Geolocator: Connected to google api client.");
                                           })
                                         .AddOnConnectionFailedListener((result) =>
                                            {
                                                PositionError?.Invoke(this, new PositionErrorEventArgs(GeolocationError.PositionUnavailable));

                                                googleApiClient?.Reconnect();
                                                System.Diagnostics.Debug.WriteLine($"Geolocator: Connection Failes: {result.ErrorMessage}");
                                            });

            googleApiClient = await builder.BuildAndConnectAsync((i) =>
            {
                System.Diagnostics.Debug.WriteLine($"Geolocator: Connection paused: {i}");

            });
        }

        public void OnLocationChanged(Location location)
        {
            var position = location.ToPosition();
            lock (positionSync)
            {
                lastKnownPosition = position;
            }
            PositionChanged?.Invoke(this, new PositionEventArgs(position));
        }

        /// <summary>
        /// Gets position async with specified parameters
        /// </summary>
        /// <param name="timeout">Timeout to wait, Default Infinite</param>
        /// <param name="token">Cancelation token</param>
        /// <param name="includeHeading">If you would like to include heading</param>
        /// <returns>Position</returns>
        public async Task<Position> GetPositionAsync(TimeSpan? timeout = default(TimeSpan?), CancellationToken? token = default(CancellationToken?), bool includeHeading = false)
        {
            var timeoutMilliseconds = timeout.HasValue ? (int)timeout.Value.TotalMilliseconds : Timeout.Infinite;

            if (timeoutMilliseconds <= 0 && timeoutMilliseconds != Timeout.Infinite)
                throw new ArgumentOutOfRangeException(nameof(timeout), "timeout must be greater than or equal to 0");

            if (!token.HasValue)
                token = CancellationToken.None;

            // If we're already listening, just use the current listener
            if (IsListening)
            {
                Position lastPosition = null;
                lock(positionSync)
                {
                    lastPosition = lastKnownPosition;
                }


                if (lastPosition == null)
                {
                   return await NextLocationAsync();
                }

                return lastPosition;
            }

            await Initialize();
             var minTime = (long)minimumTime.TotalMilliseconds;

            var locationRequest = new LocationRequest();
         
            locationRequest.SetSmallestDisplacement(Convert.ToSingle(DesiredAccuracy))
                .SetFastestInterval(minTime)
                .SetInterval(minTime * 3)
                .SetMaxWaitTime(minTime * 6)
               .SetPriority(GetPriority());

            var result = await LocationServices.FusedLocationApi.RequestLocationUpdatesAsync(googleApiClient, locationRequest, this);

            if (result.IsSuccess)

            return null;
        }

        /// <summary>
        /// Gets the last known and most accurate location.
        /// This is usually cached and best to display first before querying for full position.
        /// </summary>
        /// <returns>Best and most recent location or null if none found</returns>
        public async Task<Position> GetLastKnownLocationAsync()
        {
            var hasPermission = await GeolocationUtils.CheckPermissions();
            if (!hasPermission)
                throw new GeolocationException(GeolocationError.Unauthorized);

            await Initialize();

            if (!(googleApiClient?.IsConnected ?? false))
                throw new GeolocationException(GeolocationError.CanNotConnect);

            var location = LocationServices.FusedLocationApi.GetLastLocation(googleApiClient);
            return location.ToPosition();
        }

        /// <summary>
        /// Retrieve addresses for position.
        /// </summary>
        /// <param name="position">Desired position (latitude and longitude)</param>
        /// <returns>Addresses of the desired position</returns>
        public Task<IEnumerable<Abstractions.Address>> GetAddressesForPositionAsync(Position position, string mapKey = null) =>
                GeolocationUtils.GetAddressesForPositionAsync(position);

        /// <summary>
        /// Start listening for changes
        /// </summary>
        /// <param name="minimumTime">Time</param>
        /// <param name="minimumDistance">Distance</param>
        /// <param name="includeHeading">Include heading or not</param>
        /// <param name="listenerSettings">Optional settings (iOS only)</param>
        public async Task<bool> StartListeningAsync(TimeSpan minimumTime, double minimumDistance, bool includeHeading = false, ListenerSettings listenerSettings = null)
        {
            
            var hasPermission = await GeolocationUtils.CheckPermissions();
            if (!hasPermission)
                throw new GeolocationException(GeolocationError.Unauthorized);

            await Initialize();

            if (!(googleApiClient?.IsConnected ?? false))
                throw new GeolocationException(GeolocationError.CanNotConnect);

            var minTime = (long)minimumTime.TotalMilliseconds;

            var locationRequest = new LocationRequest();
         
            locationRequest.SetSmallestDisplacement(Convert.ToSingle(minimumDistance))
                .SetFastestInterval(minTime)
                .SetInterval(minTime * 3)
                .SetMaxWaitTime(minTime * 6)
               .SetPriority(GetPriority());

            var result = await LocationServices.FusedLocationApi.RequestLocationUpdatesAsync(googleApiClient, locationRequest, this);

            if (result.IsSuccess)
                IsListening = true;

            return result.IsSuccess;
        }

        /// <summary>
        /// Stop listening
        /// </summary>
        /// <returns>If successfully stopped</returns>
        public async Task<bool> StopListeningAsync()
        {
            if (googleApiClient == null)
                return true;

            if (googleApiClient.IsConnected)
            {
                await LocationServices.FusedLocationApi.RemoveLocationUpdatesAsync(googleApiClient, this);
            }

            IsListening = false;
            googleApiClient.StopAutoManage(CrossCurrentActivity.Current.Activity as FragmentActivity);
            googleApiClient.Disconnect();
            googleApiClient.Dispose();
            googleApiClient = null;

            return true;
        }

        int GetPriority()
        {
            if (DesiredAccuracy < 50)
                return LocationRequest.PriorityHighAccuracy;

            if (DesiredAccuracy < 100)
                return LocationRequest.PriorityBalancedPowerAccuracy;

            if (DesiredAccuracy < 200)
                return LocationRequest.PriorityLowPower;

            return LocationRequest.PriorityNoPower;
        }


        /// <summary>
        /// Gets the next location event from the current listener.
        /// </summary>
        /// <returns>The location async.</returns>
        Task<Position> NextLocationAsync()
        {
            var locationSource = new TaskCompletionSource<Position>();
            EventHandler<PositionEventArgs> handler = null;

            handler = (sender, args) =>
            {
                PositionChanged -= handler;
                locationSource.SetResult(args.Position);
            };

            PositionChanged += handler;
            return locationSource.Task;
        }

        bool IsLocationServicesEnabled()
        {
            int locationMode = 0;
            string locationProviders;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat)
            {
                try
                {
                    locationMode = Settings.Secure.GetInt(
                        context.ContentResolver,
                        Settings.Secure.LocationMode);
                }
                catch
                {
                    return false;
                }

                return locationMode != (int)SecurityLocationMode.Off;
            }

            locationProviders = Settings.Secure.GetString(
                context.ContentResolver,
                Settings.Secure.LocationProvidersAllowed);

            return !string.IsNullOrEmpty(locationProviders);
        }

    }
}