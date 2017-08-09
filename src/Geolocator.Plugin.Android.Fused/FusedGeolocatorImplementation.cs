﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.Gms.Common;
using Android.Gms.Common.Apis;
using Android.Gms.Location;
using Android.Locations;
using Android.OS;
using Android.Provider;
using Plugin.Geolocator.Abstractions;
using Plugin.Permissions.Abstractions;
using Plugin.Permissions;
using GmsLocation = Android.Gms.Location;

namespace Plugin.Android.Fused
{
	internal class FusedGeolocatorImplementation : Java.Lang.Object, GoogleApiClient.IConnectionCallbacks,
										GoogleApiClient.IOnConnectionFailedListener, GmsLocation.ILocationListener,
                                        IGeolocator, IDisposable
    {
		private readonly Context _context;

        private GoogleApiClient _client;
		private Position _lastKnownPosition;

		public event EventHandler<PositionErrorEventArgs> PositionError;
		public event EventHandler<PositionEventArgs> PositionChanged;

        public double DesiredAccuracy { get; set; } = 90;
        public bool IsListening { get; private set; }
        public bool SupportsHeading => true;
        public bool AllowsBackgroundUpdates { get; set; }
        public bool PausesLocationUpdatesAutomatically { get; set; }
		public bool IsGeolocationEnabled => IsLocationServicesEnabled();

		public FusedGeolocatorImplementation(Context context)
		{
            _context = context ?? throw new NullReferenceException(nameof(context));
		}

		private void Initialize()
		{
			_client = new GoogleApiClient.Builder(_context)
										 .AddApi(LocationServices.API)
										 .AddConnectionCallbacks(this)
										 .AddOnConnectionFailedListener(this)
										 .Build();
			_client.Connect();
		}

		public bool IsGeolocationAvailable
        {
            get
            {
                if (_client?.IsConnected == true)
                {
                    var avalibility = LocationServices.FusedLocationApi.GetLocationAvailability(_client);
                    if (avalibility != null)
                        return avalibility.IsLocationAvailable;
                }

                return IsLocationServicesEnabled();
            }
        }

		public void OnLocationChanged(Location location)
		{
            var position = new Position
            {
                Latitude = location.Latitude,
                Longitude = location.Longitude,
                Accuracy = location.Accuracy,
                AltitudeAccuracy = location.Accuracy,
                Altitude = location.Altitude,
                Heading = location.Bearing,
                Speed = location.Speed,
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(location.Time)
            };

            _lastKnownPosition = position;
            PositionChanged?.Invoke(this, new PositionEventArgs(position));
        }

		public void OnConnected(Bundle connectionHint) => Console.WriteLine("Connected");

		public void OnConnectionSuspended(int cause) => Console.WriteLine($"ConnectionSuspended: {cause}");

        public void OnConnectionFailed(ConnectionResult result)
        {
            Console.WriteLine($"ConnectionFailed: {result.ErrorMessage}");
            PositionError?.Invoke(this, new PositionErrorEventArgs(GeolocationError.PositionUnavailable));

            _client?.Reconnect();
        }

        public async Task<Position> GetPositionAsync(TimeSpan? timeout = default(TimeSpan?), CancellationToken? token = default(CancellationToken?), bool includeHeading = false)
        {
			var timeoutMilliseconds = timeout.HasValue ? (int)timeout.Value.TotalMilliseconds : Timeout.Infinite;

            if (timeoutMilliseconds <= 0 && timeoutMilliseconds != Timeout.Infinite)
                throw new ArgumentOutOfRangeException(nameof(timeout), "timeout must be greater than or equal to 0");
			
			if (!token.HasValue)
                token = CancellationToken.None;

            if (IsListening)
            {
                if (_lastKnownPosition != null)
                    return _lastKnownPosition;

                return await NextLocationAsync();
            }

            await StartListeningAsync(TimeSpan.FromMilliseconds(500), 10, includeHeading);
            var position = await NextLocationAsync();
            await StopListeningAsync();
            return position;
        }

        public async Task<Position> GetLastKnownLocationAsync()
        {
			var hasPermission = await CheckPermissions();
			if (!hasPermission)
				throw new GeolocationException(GeolocationError.Unauthorized);
			
            return _lastKnownPosition;
        }

        public async Task<IEnumerable<global::Plugin.Geolocator.Abstractions.Address>> GetAddressesForPositionAsync(Position position, string mapKey = null)
        {
            if (position == null)
                return null;
            
            using (var geocoder = new Geocoder(_context))
            {
                return (await geocoder.GetFromLocationAsync(position.Latitude, position.Longitude, 10))
                    .Select(address => new global::Plugin.Geolocator.Abstractions.Address
						{
							Longitude = address.Longitude,
							Latitude = address.Latitude,
							FeatureName = address.FeatureName,
							PostalCode = address.PostalCode,
							SubLocality = address.SubLocality,
							CountryCode = address.CountryCode,
							CountryName = address.CountryName,
							Thoroughfare = address.Thoroughfare,
							SubThoroughfare = address.SubThoroughfare,
							Locality = address.Locality
						});
            }
        }

        public async Task<bool> StartListeningAsync(TimeSpan minimumTime, double minimumDistance, bool includeHeading = false, ListenerSettings listenerSettings = null)
        {
			var hasPermission = await CheckPermissions();
			if (!hasPermission)
				throw new GeolocationException(GeolocationError.Unauthorized);
			
            if (_client == null)
			{
				Initialize();
			}

            if (!_client.IsConnected)
                _client.Connect();

            if (!_client.IsConnected)
                return await Task.FromResult(false);
            
            var minTime = (long)minimumTime.TotalMilliseconds;
            var locationRequest = new LocationRequest();
            locationRequest.SetSmallestDisplacement(Convert.ToSingle(minimumDistance))
                .SetFastestInterval(minTime)
                .SetInterval(minTime * 3)
                .SetMaxWaitTime(minTime * 6)
                .SetPriority(GetPriority());

            var result = await LocationServices.FusedLocationApi.RequestLocationUpdatesAsync(_client, locationRequest, this);

            if (result.IsSuccess)
                IsListening = true;

            return result.IsSuccess;
        }

        public async Task<bool> StopListeningAsync()
        {
			if (_client == null)
				return true;

            if (_client.IsConnected)
            {
                var result = await LocationServices.FusedLocationApi.RemoveLocationUpdatesAsync(_client, this);
                if (!result.IsSuccess)
                    return false;
                
				IsListening = false;
            }

			if (_client.IsConnected || _client.IsConnecting)
			{
				_client.Disconnect();
				_client = null;
			}

            return true;
        }

        private int GetPriority()
        {
            if (DesiredAccuracy < 50)
                return LocationRequest.PriorityHighAccuracy;

            if (DesiredAccuracy < 100)
                return LocationRequest.PriorityBalancedPowerAccuracy;

            if (DesiredAccuracy < 200)
                return LocationRequest.PriorityLowPower;

            return LocationRequest.PriorityNoPower;
        }

        private Task<Position> NextLocationAsync()
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

        private bool IsLocationServicesEnabled()
        {
            int locationMode = 0;
            string locationProviders;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat)
            {
                try
                {
                    locationMode = Settings.Secure.GetInt(
                        _context.ContentResolver,
                        Settings.Secure.LocationMode);
                }
                catch
                {
                    return false;
                }

                return locationMode != (int)SecurityLocationMode.Off;
            }

            locationProviders = Settings.Secure.GetString(
                _context.ContentResolver,
                Settings.Secure.LocationProvidersAllowed);

            return !string.IsNullOrEmpty(locationProviders);
        }


        async Task<bool> CheckPermissions()
        {
            var status = await CrossPermissions.Current.CheckPermissionStatusAsync(Permission.Location);
            if (status != PermissionStatus.Granted)
            {
                Console.WriteLine("Currently does not have Location permissions, requesting permissions");

                var request = await CrossPermissions.Current.RequestPermissionsAsync(Permission.Location);

                if (request[Permission.Location] != PermissionStatus.Granted)
                {
                    Console.WriteLine("Location permission denied, can not get positions async.");
                    return false;
                }
            }

            return true;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing || _client == null) return;

            if (_client.IsConnected)
                _client.Disconnect();

            _client.Dispose();
        }
    }
}
