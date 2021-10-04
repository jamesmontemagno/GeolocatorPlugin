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
using Android.Runtime;
using Address = Plugin.Geolocator.Abstractions.Address;
using Xamarin.Essentials;
using Location = Android.Locations.Location;

namespace Plugin.Geolocator
{
	/// <summary>
	/// Implementation for Feature
	/// </summary>
	[Preserve(AllMembers = true)]
	public class GeolocatorImplementation : IGeolocator
	{
		readonly string[] allProviders;
		LocationManager locationManager;

		GeolocationContinuousListener listener;
		GeolocationSingleListener singleListener = null;

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

		/// <summary>
		/// Gets or sets the location manager providers to ignore when getting postition
		/// </summary>
		public static string[] ProvidersToUse { get; set; } = new string[] { };

		/// <summary>
		/// Gets or sets the location manager providers to ignore when doing
		/// continuous listening
		/// </summary>
		public static string[] ProvidersToUseWhileListening { get; set; } = new string[] { };

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
		public bool IsGeolocationEnabled => Providers.Any(p => !IgnoredProviders.Contains(p) &&
			Manager.IsProviderEnabled(p));


		/// <summary>
		/// Gets the last known and most accurate location.
		/// This is usually cached and best to display first before querying for full position.
		/// </summary>
		/// <returns>Best and most recent location or null if none found</returns>
		public async Task<Position> GetLastKnownLocationAsync()
		{
			var hasPermission = await CheckWhenInUsePermission();
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

		async Task<bool> CheckWhenInUsePermission()
		{
			var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
			if (status != PermissionStatus.Granted)
			{
				Console.WriteLine("Currently does not have Location permissions, requesting permissions");

				status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

				if (status != PermissionStatus.Granted)
				{
					Console.WriteLine("Location permission denied, can not get positions async.");
					return false;
				}
			}

			return true;

		}
		async Task<bool> CheckAlwaysPermissions()
		{
			var status = await Permissions.CheckStatusAsync<Permissions.LocationAlways>();
			if (status != PermissionStatus.Granted)
			{
				Console.WriteLine("Currently does not have Location permissions, requesting permissions");

				status = await Permissions.CheckStatusAsync<Permissions.LocationAlways>();

				if (status != PermissionStatus.Granted)
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

			var hasPermission = await CheckWhenInUsePermission();
			if (!hasPermission)
				throw new GeolocationException(GeolocationError.Unauthorized);

			var tcs = new TaskCompletionSource<Position>();

			if (!IsListening)
			{
				var providers = new List<string>();
				if (ProvidersToUse == null || ProvidersToUse.Length == 0)
					providers.AddRange(Providers);
				else
				{
					//only add providers requested.
					foreach (var provider in Providers)
					{
						if (ProvidersToUse == null || !ProvidersToUse.Contains(provider))
							continue;

						providers.Add(provider);
					}
				}


				void SingleListnerFinishCallback()
				{
					if (singleListener == null)
						return;

					for (var i = 0; i < providers.Count; ++i)
						Manager.RemoveUpdates(singleListener);
				}

				singleListener = new GeolocationSingleListener(Manager,
					(float)DesiredAccuracy,
					timeoutMilliseconds,
					providers.Where(Manager.IsProviderEnabled),
					finishedCallback: SingleListnerFinishCallback);

				if (cancelToken != CancellationToken.None)
				{
					cancelToken.Value.Register(() =>
					{
						singleListener.Cancel();

						for (var i = 0; i < providers.Count; ++i)
							Manager.RemoveUpdates(singleListener);
					}, true);
				}

				try
				{
					var looper = Looper.MyLooper() ?? Looper.MainLooper;

					var enabled = 0;
					for (var i = 0; i < providers.Count; ++i)
					{
						if (Manager.IsProviderEnabled(providers[i]))
							enabled++;

						Manager.RequestLocationUpdates(providers[i], 0, 0, singleListener, looper);
					}

					if (enabled == 0)
					{
						for (int i = 0; i < providers.Count; ++i)
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
				throw new ArgumentNullException(nameof(position));

			using (var geocoder = new Geocoder(Application.Context))
			{
				var addressList = await geocoder.GetFromLocationAsync(position.Latitude, position.Longitude, 10);
				return addressList?.ToAddresses() ?? null;
			}
		}

		/// <summary>
		/// Retrieve positions for address.
		/// </summary>
		/// <param name="address">Desired address</param>
		/// <param name="mapKey">Map Key required only on UWP</param>
		/// <returns>Positions of the desired address</returns>
		public async Task<IEnumerable<Position>> GetPositionsForAddressAsync(string address, string mapKey = null)
		{
			if (address == null)
				throw new ArgumentNullException(nameof(address));

			using (var geocoder = new Geocoder(Application.Context))
			{
				var addressList = await geocoder.GetFromLocationNameAsync(address, 10);
				return addressList.Select(p => new Position
				{
					Latitude = p.Latitude,
					Longitude = p.Longitude
				});
			}
		}

		List<string> ListeningProviders { get; } = new List<string>();
		/// <summary>
		/// Start listening for changes
		/// </summary>
		/// <param name="minimumTime">Time</param>
		/// <param name="minimumDistance">Distance</param>
		/// <param name="includeHeading">Include heading or not</param>
		/// <param name="listenerSettings">Optional settings (iOS only)</param>
		public async Task<bool> StartListeningAsync(TimeSpan minimumTime, double minimumDistance, bool includeHeading = false, ListenerSettings listenerSettings = null)
		{
			var hasPermission = false;
			if (listenerSettings?.RequireLocationAlwaysPermission ?? false)
				hasPermission = await CheckAlwaysPermissions();
			else
				hasPermission = await CheckWhenInUsePermission();

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
			ListeningProviders.Clear();
			for (var i = 0; i < providers.Length; ++i)
			{
				var provider = providers[i];

				//we have limited set of providers
				if (ProvidersToUseWhileListening != null &&
					ProvidersToUseWhileListening.Length > 0)
				{
					//the provider is not in the list, so don't use it.
					if (!ProvidersToUseWhileListening.Contains(provider))
						continue;
				}


				ListeningProviders.Add(provider);
				Manager.RequestLocationUpdates(provider, (long)minTimeMilliseconds, (float)minimumDistance, listener, looper);
			}
			return true;
		}
		/// <inheritdoc/>
		public Task<bool> StopListeningAsync()
		{
			if (listener == null)
				return Task.FromResult(true);

			if (ListeningProviders == null)
				return Task.FromResult(true);

			var providers = ListeningProviders;
			listener.PositionChanged -= OnListenerPositionChanged;
			listener.PositionError -= OnListenerPositionError;

			for (var i = 0; i < providers.Count; i++)
			{
				try
				{
					Manager.RemoveUpdates(listener);
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine("Unable to remove updates: " + ex);
				}
			}

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
	}
}