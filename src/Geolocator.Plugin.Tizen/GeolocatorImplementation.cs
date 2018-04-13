using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Plugin.Geolocator.Abstractions;
using Tizen.Location;
using Tizen.System;
using Tizen.Maps;
using System.Linq;

namespace Plugin.Geolocator
{
	/// <summary>
	/// Implementation for Geolocator
	/// </summary>
	public class GeolocatorImplementation : IGeolocator
	{
		private static string locationFeature = "http://tizen.org/feature/location";
		private static string gpsFeature = "http://tizen.org/feature/location.gps";
		private static string wpsFeature = "http://tizen.org/feature/location.wps";
		private static bool locationSupported = false;
		private static bool locationEnabled = false;
		private bool isListening;
		private static bool gpsSupported = false;
		private static bool wpsSupported = false;
		private static Locator locator;
		private static Location location;
		private static Location lastLocation;

		private MapService maps = null;
		public static string ServiceProvider;
		public static string ServiceProviderKey;

		public GeolocatorImplementation()
		{
			locationSupported = CheckCapability(locationFeature);
			gpsSupported = CheckCapability(gpsFeature);
			wpsSupported = CheckCapability(wpsFeature);

			if (locationSupported)
			{
				location = new Location();
				locator = new Locator(LocationType.Hybrid);
				locator.ServiceStateChanged += (s, e) => {
					if (e.ServiceState == ServiceState.Enabled)
					{
						locationEnabled = true;
					}
					else
					{
						locationEnabled = false;
					}
				};
				locator.LocationChanged += OnLocationChanged;
			}
			DesiredAccuracy = 100;

			if (string.IsNullOrWhiteSpace(ServiceProvider) || string.IsNullOrWhiteSpace(ServiceProviderKey))
			{
				throw new Exception($"Must set service provider and key of MapService");
			}
			else
			{
				maps = new MapService(ServiceProvider, ServiceProviderKey);
			}
		}

		private static bool CheckCapability(string feature)
		{
			bool ret = false;
			if (Information.TryGetValue<bool>(feature, out ret))
				return ret;

			return false;
		}

		public double DesiredAccuracy
		{
			get
			{
				return location.Accuracy;
			}
			set
			{
				location.Accuracy = value;
			}
		}

		public bool IsListening => isListening;

		public bool SupportsHeading => true;

		public bool IsGeolocationAvailable => locationSupported;

		public bool IsGeolocationEnabled => locationEnabled;

		public event EventHandler<PositionErrorEventArgs> PositionError;
		public event EventHandler<PositionEventArgs> PositionChanged;

		public async Task<IEnumerable<Address>> GetAddressesForPositionAsync(Position position, string mapKey = null)
		{
			if (position == null)
				return null;

			GeolocatorUtils.Positions(position);

			if (maps == null)
				maps = new MapService(ServiceProvider, ServiceProviderKey);

			var requestResult = await maps.RequestUserConsent();
			if (!requestResult)
				return null;

			var request = await maps.CreateReverseGeocodeRequest(position.Latitude, position.Longitude).GetResponseAsync();
			
			return request.ToAddresses();
		}

		public async Task<IEnumerable<Position>> GetPositionsForAddressAsync(string address, string mapKey = null)
		{
			if (address == null)
				throw new ArgumentNullException(nameof(address));

			if (maps == null)
				maps = new MapService(ServiceProvider, ServiceProviderKey);

			var request = await maps.CreateGeocodeRequest(address).GetResponseAsync();
			return request.Select(p => new Position
			{
				Latitude = p.Latitude,
				Longitude = p.Longitude
			});
		}

		public async Task<Position> GetLastKnownLocationAsync()
		{
			if (lastLocation != null)
			{
				var tcs = new TaskCompletionSource<Position>();
				tcs.SetResult(GetPosition(lastLocation));
				return await tcs.Task;
			}
			return null;
		}

		public async Task<Position> GetPositionAsync(TimeSpan? timeout = default(TimeSpan?), CancellationToken? cancelToken = default(CancellationToken?), bool includeHeading = false)
		{
			var timeoutMilliseconds = timeout.HasValue ? (int)timeout.Value.TotalMilliseconds : Timeout.Infinite;

			if (timeoutMilliseconds <= 0 && timeoutMilliseconds != Timeout.Infinite)
				throw new ArgumentOutOfRangeException(nameof(timeout), "timeout must be greater than or equal to 0");

			if (!cancelToken.HasValue)
				cancelToken = CancellationToken.None;
			
			var tcs = new TaskCompletionSource<Position>();
			
			Task<Location> task = locator.GetLocationAsync(60);
			Location location = await task;
			tcs.SetResult(GetPosition(location));
			return await tcs.Task;
		}

		public Task<bool> StartListeningAsync(TimeSpan minimumTime, double minimumDistance, bool includeHeading = false, ListenerSettings listenerSettings = null)
		{
			var minTimeMilliseconds = minimumTime.TotalMilliseconds;
			if (minTimeMilliseconds < 0)
				throw new ArgumentOutOfRangeException(nameof(minimumTime));
			if (minimumDistance < 0)
				throw new ArgumentOutOfRangeException(nameof(minimumDistance));
			if (IsListening)
				throw new InvalidOperationException("This Geolocator is already listening");

			isListening = true;

			locator.Interval = minimumTime.Seconds;
			locator.Distance = minimumDistance;
			locator.LocationChanged += OnLocationChanged;
			return Task.FromResult(true);
		}

		public Task<bool> StopListeningAsync()
		{
			if (!isListening)
				return Task.FromResult(true);

			locator.LocationChanged -= OnLocationChanged;
			isListening = false;

			return Task.FromResult(true);
		}

		private void OnLocationChanged(object s, LocationChangedEventArgs e)
		{
			if (!IsListening)
				return;

			location = e.Location;
			isListening = true;

			OnPositionChanged(new PositionEventArgs(GetPosition(e.Location)));
		}

		private void OnPositionChanged(PositionEventArgs e) => PositionChanged?.Invoke(this, e);

		private void OnPositionError(PositionErrorEventArgs e) => PositionError?.Invoke(this, e);

		private static Position GetPosition(Location location)
		{
			lastLocation = location;
			var pos = new Position
			{
				Latitude = location.Latitude,
				Longitude = location.Longitude,
				Altitude = location.Altitude,
				Speed = location.Speed,
				Heading = location.Direction,
				Accuracy = location.Accuracy,
				Timestamp = location.Timestamp,
				AltitudeAccuracy = 0,
			};

			return pos;
		}
	}
}
