using Plugin.Geolocator.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.Locations;
using System.Threading;
using Android.App;
using Android.OS;
using System.Linq;
using Android.Content;
using Android.Content.PM;
using Plugin.Permissions;
using Android.Runtime;
using Address = Plugin.Geolocator.Abstractions.Address;

namespace Plugin.Geolocator
{
	/// <summary>
	/// Implementation for Feature
	/// </summary>
	[Preserve(AllMembers = true)]
	public class GeolocatorImplementation : IGeolocator
	{
		// Mock location rejection
		private Location lastMockLocation;
		private int numGoodReadings;

        private bool mockLocationsEnabled;

		string[] allProviders;
		LocationManager locationManager;

		GeolocationContinuousListener listener;

		readonly object positionSync = new object();
		Position lastPosition;

		/// <summary>
		/// Default constructor
		/// </summary>
		public GeolocatorImplementation()
		{
			DesiredAccuracy = 100;
		}

		string[] Providers => Manager.GetProviders(enabledOnly: false).ToArray();
		string[] IgnoredProviders => new string[] { LocationManager.PassiveProvider, "local_database" };

		LocationManager Manager
		{
			get
			{
				if (locationManager == null)
					locationManager = (LocationManager)Application.Context.GetSystemService(Context.LocationService);

				return locationManager;
			}
		}

		/// <inheritdoc/>
		public event EventHandler<PositionErrorEventArgs> PositionError;
		/// <inheritdoc/>
		public event EventHandler<PositionEventArgs> PositionChanged;
		/// <inheritdoc/>
		public bool IsListening => listener != null;


		/// <inheritdoc/>
		public double DesiredAccuracy
		{
			get;
			set;
		}

		/// <inheritdoc/>
		public bool SupportsHeading => true;


		/// <inheritdoc/>
		public bool IsGeolocationAvailable => Providers.Length > 0;


		/// <inheritdoc/>
		public bool IsGeolocationEnabled => Providers.Any(p => !IgnoredProviders.Contains(p) && Manager.IsProviderEnabled(p));


		/// <summary>
		/// Gets the last known and most accurate location.
		/// This is usually cached and best to display first before querying for full position.
		/// </summary>
		/// <returns>Best and most recent location or null if none found</returns>
		public async Task<Position> GetLastKnownLocationAsync()
		{
			var hasPermission = await CheckPermissions();
			if (!hasPermission)
				throw new GeolocationException(GeolocationError.Unauthorized);

			Location bestLocation = null;
			foreach (var provider in Providers)
			{
				var location = Manager.GetLastKnownLocation(provider);
				if (location != null && GeolocationUtils.IsBetterLocation(location, bestLocation))
					bestLocation = location;
			}

			return bestLocation?.ToPosition();

		}

		async Task<bool> CheckPermissions()
		{
			var status = await CrossPermissions.Current.CheckPermissionStatusAsync(Permissions.Abstractions.Permission.Location);
			if (status != Permissions.Abstractions.PermissionStatus.Granted)
			{
				Console.WriteLine("Currently does not have Location permissions, requesting permissions");

				var request = await CrossPermissions.Current.RequestPermissionsAsync(Permissions.Abstractions.Permission.Location);

				if (request[Permissions.Abstractions.Permission.Location] != Permissions.Abstractions.PermissionStatus.Granted)
				{
					Console.WriteLine("Location permission denied, can not get positions async.");
					return false;
				}
			}

			return true;
		}


		/// <summary>
		/// Gets position async with specified parameters
		/// </summary>
		/// <param name="timeout">Timeout to wait, Default Infinite</param>
		/// <param name="cancelToken">Cancelation token</param>
		/// <param name="includeHeading">If you would like to include heading</param>
		/// <returns>Position</returns>
		public async Task<Position> GetPositionAsync(TimeSpan? timeout, CancellationToken? cancelToken = null, bool includeHeading = false)
		{
			var timeoutMilliseconds = timeout.HasValue ? (int)timeout.Value.TotalMilliseconds : Timeout.Infinite;

			if (timeoutMilliseconds <= 0 && timeoutMilliseconds != Timeout.Infinite)
				throw new ArgumentOutOfRangeException(nameof(timeout), "timeout must be greater than or equal to 0");

			if (!cancelToken.HasValue)
				cancelToken = CancellationToken.None;

			var hasPermission = await CheckPermissions();
			if (!hasPermission)
				throw new GeolocationException(GeolocationError.Unauthorized);

			var tcs = new TaskCompletionSource<Position>();

			if (!IsListening)
			{
				var providers = Providers;
				GeolocationSingleListener singleListener = null;
				singleListener = new GeolocationSingleListener(Manager, (float)DesiredAccuracy, timeoutMilliseconds, providers.Where(Manager.IsProviderEnabled),
					finishedCallback: () =>
				{
					for (int i = 0; i < providers.Length; ++i)
						Manager.RemoveUpdates(singleListener);
				});

				if (cancelToken != CancellationToken.None)
				{
					cancelToken.Value.Register(() =>
					{
						singleListener.Cancel();

						for (var i = 0; i < providers.Length; ++i)
							Manager.RemoveUpdates(singleListener);
					}, true);
				}

				try
				{
					var looper = Looper.MyLooper() ?? Looper.MainLooper;

					int enabled = 0;
					for (int i = 0; i < providers.Length; ++i)
					{
						if (Manager.IsProviderEnabled(providers[i]))
							enabled++;

						Manager.RequestLocationUpdates(providers[i], 0, 0, singleListener, looper);
					}

					if (enabled == 0)
					{
						for (int i = 0; i < providers.Length; ++i)
							Manager.RemoveUpdates(singleListener);

						tcs.SetException(new GeolocationException(GeolocationError.PositionUnavailable));
						return await tcs.Task;
					}
				}
				catch (Java.Lang.SecurityException ex)
				{
					tcs.SetException(new GeolocationException(GeolocationError.Unauthorized, ex));
					return await tcs.Task;
				}

				return await singleListener.Task;
			}

			// If we're already listening, just use the current listener
			lock (positionSync)
			{
				if (lastPosition == null)
				{
					if (cancelToken != CancellationToken.None)
					{
						cancelToken.Value.Register(() => tcs.TrySetCanceled());
					}

					EventHandler<PositionEventArgs> gotPosition = null;
					gotPosition = (s, e) =>
					{
						tcs.TrySetResult(e.Position);
						PositionChanged -= gotPosition;
					};

					PositionChanged += gotPosition;
				}
				else
				{
					tcs.SetResult(lastPosition);
				}
			}

			return await tcs.Task;
		}

		/// <summary>
		/// Retrieve addresses for position.
		/// </summary>
		/// <param name="position">Desired position (latitude and longitude)</param>
		/// <returns>Addresses of the desired position</returns>
		public async Task<IEnumerable<Address>> GetAddressesForPositionAsync(Position position, string mapKey = null)
		{
			if (position == null)
				return null;

			var geocoder = new Geocoder(Application.Context);
			var addressList = await geocoder.GetFromLocationAsync(position.Latitude, position.Longitude, 10);
			return addressList.ToAddresses();
		}

		/// <summary>
		/// Start listening for changes
		/// </summary>
		/// <param name="minimumTime">Time</param>
		/// <param name="minimumDistance">Distance</param>
		/// <param name="includeHeading">Include heading or not</param>
		/// <param name="listenerSettings">Optional settings (iOS only)</param>
		public async Task<bool> StartListeningAsync(TimeSpan minimumTime, double minimumDistance, bool includeHeading = false, ListenerSettings listenerSettings = null)
		{
			var hasPermission = await CheckPermissions();
			if (!hasPermission)
				throw new GeolocationException(GeolocationError.Unauthorized);


			var minTimeMilliseconds = minimumTime.TotalMilliseconds;
			if (minTimeMilliseconds < 0)
				throw new ArgumentOutOfRangeException(nameof(minimumTime));
			if (minimumDistance < 0)
				throw new ArgumentOutOfRangeException(nameof(minimumDistance));
			if (IsListening)
				throw new InvalidOperationException("This Geolocator is already listening");

			var providers = Providers;
			listener = new GeolocationContinuousListener(Manager, minimumTime, providers);
			listener.PositionChanged += OnListenerPositionChanged;
			listener.PositionError += OnListenerPositionError;

			var looper = Looper.MyLooper() ?? Looper.MainLooper;
			for (var i = 0; i < providers.Length; ++i)
				Manager.RequestLocationUpdates(providers[i], (long)minTimeMilliseconds, (float)minimumDistance, listener, looper);

			return true;
		}
		/// <inheritdoc/>
		public Task<bool> StopListeningAsync()
		{
			if (listener == null)
				return Task.FromResult(true);

			var providers = Providers;
			listener.PositionChanged -= OnListenerPositionChanged;
			listener.PositionError -= OnListenerPositionError;

			for (var i = 0; i < providers.Length; ++i)
				Manager.RemoveUpdates(listener);

			listener = null;
			return Task.FromResult(true);
		}


		/// <inheritdoc/>
		private void OnListenerPositionChanged(object sender, PositionEventArgs e)
		{
			if (!IsListening) // ignore anything that might come in afterwards
				return;

			lock (positionSync)
			{
				lastPosition = e.Position;

				PositionChanged?.Invoke(this, e);
			}
		}
		/// <inheritdoc/>
		private async void OnListenerPositionError(object sender, PositionErrorEventArgs e)
		{
			await StopListeningAsync();

			PositionError?.Invoke(this, e);
		}

        /// <summary>
        /// Checks the mock locations.
        /// </summary>
		private void CheckMockLocations()
		{
			// Starting with API level >= 18 we can (partially) rely on .isFromMockProvider()
			// (http://developer.android.com/reference/android/location/Location.html#isFromMockProvider%28%29)
			// For API level < 18 we have to check the Settings.Secure flag
            if (Build.VERSION.SdkInt < BuildVersionCodes.JellyBeanMr2 &&
                !Android.Provider.Settings.Secure.GetString(Application.Context.ContentResolver,
                    Android.Provider.Settings.Secure.AllowMockLocation).Equals("0"))
			{
				mockLocationsEnabled = true;
			}
			else
            {
                mockLocationsEnabled = false;
            }
		}

		/// <summary>
		/// Check for Mock Location (prevent gps spoofing)
		/// detailed blog post about this
		/// http://www.klaasnotfound.com/2016/05/27/location-on-android-stop-mocking-me/
		/// </summary>
		/// <returns>The location plausible.</returns>
		public async Task<bool> IsLocationPlausible()
		{
			var hasPermission = await CheckPermissions();

			if (!hasPermission)
				throw new GeolocationException(GeolocationError.Unauthorized);

			Location bestLocation = null;
			foreach (var provider in Providers)
			{
				var location = Manager.GetLastKnownLocation(provider);
				if (location != null && GeolocationUtils.IsBetterLocation(location, bestLocation))
					bestLocation = location;
			}

			if (bestLocation == null) return false;

            CheckMockLocations();

            bool isMock = mockLocationsEnabled || (Build.VERSION.SdkInt >= BuildVersionCodes.JellyBeanMr2 
                                                   && bestLocation.IsFromMockProvider);
			if (isMock)
			{
				lastMockLocation = bestLocation;
				numGoodReadings = 0;
			}
			else
				numGoodReadings = Math.Min(numGoodReadings + 1, 1000000); // Prevent overflow

			// We only clear that incident record after a significant show of good behavior
			if (numGoodReadings >= 20) lastMockLocation = null;

			// If there's nothing to compare against, we have to trust it
			if (lastMockLocation == null) 
            {
                return true;
            }
                
			// And finally, if it's more than 1km away from the last known mock, we'll trust it
			double d = bestLocation.DistanceTo(lastMockLocation);
			return (d > 1000);
		}
	}
}