using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.Gms.Common.Apis;
using Android.Gms.Location;
using Android.OS;
using Android.Provider;
using Plugin.Geolocator.Abstractions;
using Android.Runtime;
using Android.App;
using Android.Gms.Extensions;
using Android.Locations;

namespace Plugin.Geolocator
{
	/// <summary>
	/// Implementation for Feature
	/// </summary>
	[Preserve(AllMembers = true)]
	public class FusedGeolocatorImplementation : IGeolocator
    {
		
		/// <summary>
		/// Default constructor
		/// </summary>
        public FusedGeolocatorImplementation()
        {
            context = Application.Context;
        }

		MyLocationCallback listeningCallback, singleCallback;

        readonly object positionSync = new object();
        readonly Context context;
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
        public Task<bool> GetIsGeolocationEnabledAsync() => Task.FromResult(GeolocationUtils.IsLocationServicesEnabled(context));


		class MyLocationCallback : LocationCallback
		{
			public EventHandler<Location> LocationUpdated;
			public override void OnLocationResult(LocationResult result)
			{
				base.OnLocationResult(result);
				LocationUpdated?.Invoke(this, result.LastLocation);
			}
		}

		void OnLocationResult(object sender, Location location)
		{
			if (location == null)
				return;

			var position = location.ToPosition();
			lock (positionSync)
			{
				lastKnownPosition = position;
			}
			PositionChanged?.Invoke(this, new PositionEventArgs(position));
		}


		/// <summary>
		/// Gets if geolocation is enabled on device
		/// </summary>
		public async Task<bool> GetIsGeolocationAvailableAsync()
		{
			var status = await FusedClient.GetLocationAvailabilityAsync();
			return status.IsLocationAvailable;
        }

		FusedLocationProviderClient fusedClient;
		FusedLocationProviderClient FusedClient => fusedClient ?? (fusedClient = LocationServices.GetFusedLocationProviderClient(Application.Context));


		SettingsClient settingsClient;
		SettingsClient SettingsClient => settingsClient ?? (settingsClient = LocationServices.GetSettingsClient(Application.Context));

		/// <summary>
		/// Gets position async with specified parameters
		/// </summary>
		/// <param name="timeout">Timeout to wait, Default Infinite</param>
		/// <param name="cancelToken">Cancelation token</param>
		/// <param name="includeHeading">If you would like to include heading</param>
		/// <returns>Position</returns>
		public async Task<Position> GetPositionAsync(TimeSpan? timeout, CancellationToken? cancelToken = null, bool includeHeading = false)
		{
			var hasPermission = await GeolocationUtils.CheckPermissions();
			if (!hasPermission)
				throw new GeolocationException(GeolocationError.Unauthorized);

			var timeoutMilliseconds = timeout.HasValue ? (int)timeout.Value.TotalMilliseconds : Timeout.Infinite;

            if (timeoutMilliseconds <= 0 && timeoutMilliseconds != Timeout.Infinite)
                throw new ArgumentOutOfRangeException(nameof(timeout), "timeout must be greater than or equal to 0");

            if (!cancelToken.HasValue)
				cancelToken = CancellationToken.None;

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
			
            var minTime = (long)timeout.Value.TotalMilliseconds;
			var locationRequest = new LocationRequest();
			locationRequest.SetInterval(minTime);
			locationRequest.SetFastestInterval(minTime / 2);
			locationRequest.SetMaxWaitTime(minTime);
			locationRequest.SetPriority(Priority);

			Position position = null;
			try
			{
				


				//check availability
				await CheckLocationSettings(locationRequest);

				var client = FusedClient;


				if (singleCallback == null)
					singleCallback = new MyLocationCallback();


				var locationSource = new TaskCompletionSource<Position>();
				EventHandler<Location> handler = null;

				handler = (sender, location) =>
				{
					singleCallback.LocationUpdated -= handler;
					locationSource.SetResult(location.ToPosition());
				};

				singleCallback.LocationUpdated += handler;
				


				//set new request
				await client.RequestLocationUpdatesAsync(locationRequest, singleCallback, Looper.MyLooper());

				position = await locationSource.Task;

				client.RemoveLocationUpdatesAsync(singleCallback).ContinueWith((r)=>
				{
					if(r.Exception != null)
						System.Diagnostics.Debug.WriteLine(r.Exception);
				}, TaskScheduler.FromCurrentSynchronizationContext());
			}
			catch (ApiException apiEx)
			{
				System.Diagnostics.Debug.WriteLine($"ApiException: {apiEx.StatusCode} - {apiEx.StatusMessage}");
				

				throw new GeolocationException(GeolocationError.LocationServicesNotAvailable, apiEx);
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Unable to start location updates: {ex}");
				throw ex;
			}

			return position;
        }


		Task CheckLocationSettings(LocationRequest request)
		{
			var settingsRequest = new LocationSettingsRequest.Builder().AddLocationRequest(request).Build();
			return SettingsClient.CheckLocationSettingsAsync(settingsRequest);
		}

        /// <summary>
        /// Gets the last known and most accurate location.
        /// This is usually cached and best to display first before querying for full position.
        /// </summary>
        /// <returns>Best and most recent location or null if none found</returns>
        public async Task<Position> GetLastKnownPositionAsync()
        {
            var hasPermission = await GeolocationUtils.CheckPermissions();
            if (!hasPermission)
                throw new GeolocationException(GeolocationError.Unauthorized);


            var location = await FusedClient.GetLastLocationAsync();
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
        /// Retrieve positions for address.
        /// </summary>
        /// <param name="address">Desired address</param>
        /// <param name="mapKey">Map Key required only on UWP</param>
        /// <returns>Positions of the desired address</returns>
        public Task<IEnumerable<Position>> GetPositionsForAddressAsync(string address, string mapKey = null) =>
            GeolocationUtils.GetPositionsForAddressAsync(address);

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

			
            var locationRequest = new LocationRequest();

			locationRequest.SetSmallestDisplacement((float)minimumDistance);
			locationRequest.SetInterval((long)minimumTime.TotalMilliseconds);

			switch(listenerSettings?.Priority)
			{
				case ListenerPriority.HighAccuracy:
					locationRequest.SetPriority(LocationRequest.PriorityHighAccuracy);
					break;
				case ListenerPriority.BalancedPowerAccuracy:
					locationRequest.SetPriority(LocationRequest.PriorityBalancedPowerAccuracy);
					break;
				case ListenerPriority.LowPower:
					locationRequest.SetPriority(LocationRequest.PriorityLowPower);
					break;
				case ListenerPriority.NoPower:
					locationRequest.SetPriority(LocationRequest.PriorityNoPower);
					break;
				case null:
					locationRequest.SetPriority(Priority);
					break;
			}

			if (listenerSettings?.MaxWaitTime.HasValue ?? false)
				locationRequest.SetMaxWaitTime((int)listenerSettings.MaxWaitTime.Value.TotalMilliseconds);

			if (listenerSettings?.FastestInterval.HasValue ?? false)
				locationRequest.SetFastestInterval((int)listenerSettings.FastestInterval.Value.TotalMilliseconds);


			try
			{
				//check availability
				await CheckLocationSettings(locationRequest);

				if (listeningCallback == null)
					listeningCallback = new MyLocationCallback();

				listeningCallback.LocationUpdated += OnLocationResult;

				await FusedClient.RequestLocationUpdatesAsync(locationRequest, listeningCallback);

				IsListening = true;

				return true;
			}
			catch (ApiException apiEx)
			{
				System.Diagnostics.Debug.WriteLine($"ApiException: {apiEx.StatusCode} - {apiEx.StatusMessage}");
				

				throw new GeolocationException(GeolocationError.LocationServicesNotAvailable, apiEx);
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Unable to start location updates: {ex}");
				throw ex;
			}
        }

        /// <summary>
        /// Stop listening
        /// </summary>
        /// <returns>If successfully stopped</returns>
        public async Task<bool> StopListeningAsync()
        {

			if (!IsListening)
				return true;

			try
			{
				FusedClient.RemoveLocationUpdatesAsync(listeningCallback).ContinueWith((r) =>
				{
					if (r.Exception != null)
						System.Diagnostics.Debug.WriteLine(r.Exception);
				}, TaskScheduler.FromCurrentSynchronizationContext());

				listeningCallback.LocationUpdated -= OnLocationResult;

				IsListening = false;

				return true;
			}
			catch(Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Unable to stop location updates: {ex}");
			}

			return false;
        }

        int Priority
        {
			get
			{
				if (DesiredAccuracy < 100)
					return LocationRequest.PriorityHighAccuracy;

				if (DesiredAccuracy < 500)
					return LocationRequest.PriorityBalancedPowerAccuracy;

				if (DesiredAccuracy < 20000)
					return LocationRequest.PriorityLowPower;

				return LocationRequest.PriorityNoPower;
			}
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


		/// <summary>
		/// Gets the next location event from the current listener.
		/// </summary>
		/// <returns>The location async.</returns>
		Task<Position> NextLocationSingleAsync()
		{
			var locationSource = new TaskCompletionSource<Position>();
			EventHandler<Location> handler = null;

			handler = (sender, location) =>
			{
				singleCallback.LocationUpdated -= handler;
				locationSource.SetResult(location.ToPosition());
			};

			singleCallback.LocationUpdated += handler;
			
			return locationSource.Task;
		}
		
    }
}