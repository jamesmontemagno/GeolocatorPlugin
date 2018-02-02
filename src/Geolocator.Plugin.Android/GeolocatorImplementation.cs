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
		string[] allProviders;
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

        /// <summary>
        /// Position error event handler
        /// </summary>
        public event EventHandler<PositionErrorEventArgs> PositionError;

        /// <summary>
        /// Position changed event handler
        /// </summary>
        public event EventHandler<PositionEventArgs> PositionChanged;

        /// <summary>
        /// Gets if you are listening for location changes
        /// </summary>
        public bool IsListening => listener != null;


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
        /// Gets if geolocation is available on device
        /// </summary>
        public Task<bool> GetIsGeolocationAvailableAsync() => Task.FromResult(Providers.Length > 0);


        /// <summary>
        /// Gets if geolocation is enabled on device
        /// </summary>
        public Task<bool> GetIsGeolocationEnabledAsync() => Task.FromResult(Providers.Any(p => !IgnoredProviders.Contains(p) && Manager.IsProviderEnabled(p)));



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

			Location bestLocation = null;
			foreach (var provider in Providers)
			{
				var location = Manager.GetLastKnownLocation(provider);
				if (location != null && GeolocationUtils.IsBetterLocation(location, bestLocation))
					bestLocation = location;
			}

			return bestLocation?.ToPosition();

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

			var hasPermission = await GeolocationUtils.CheckPermissions();
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
						if (ProvidersToUse?.Contains(provider) ?? false)
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


		List<string> listeningProviders { get; } = new List<string>();
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
			listeningProviders.Clear();
			for (var i = 0; i < providers.Length; ++i)
			{
				var provider = providers[i];
				
				//we have limited set of providers
				if(ProvidersToUseWhileListening != null &&
					ProvidersToUseWhileListening.Length > 0)
				{
					//the provider is not in the list, so don't use it.
					if (!ProvidersToUseWhileListening.Contains(provider))
						continue;
				}

				listeningProviders.Add(provider);
				Manager.RequestLocationUpdates(provider, (long)minTimeMilliseconds, (float)minimumDistance, listener, looper);
			}
			return true;
		}

        /// <summary>
        /// Stop listening
        /// </summary>
        /// <returns>If successfully stopped</returns>
        public Task<bool> StopListeningAsync()
		{
			if (listener == null)
				return Task.FromResult(true);

			if(listeningProviders == null)
				return Task.FromResult(true);

			var providers = listeningProviders;
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


		void OnListenerPositionChanged(object sender, PositionEventArgs e)
		{
			if (!IsListening) // ignore anything that might come in afterwards
				return;

			lock (positionSync)
			{
				lastPosition = e.Position;

				PositionChanged?.Invoke(this, e);
			}
		}

        async void OnListenerPositionError(object sender, PositionErrorEventArgs e)
		{
			await StopListeningAsync();

			PositionError?.Invoke(this, e);
		}
	}
}