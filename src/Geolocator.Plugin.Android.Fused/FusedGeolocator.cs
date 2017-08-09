using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.Gms.Common;
using Plugin.Geolocator;
using Plugin.Geolocator.Abstractions;

namespace Plugin.Android.Fused
{
    public sealed class FusedGeolocator : IGeolocator
    {
        private readonly IGeolocator _geolocator;

        public FusedGeolocator(Context context)
		{
			if (GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(context) == ConnectionResult.Success)
			{
				_geolocator = new FusedGeolocatorImplementation(context);
			}
			else
			{
				Console.WriteLine($"FusedGeolocator: Google Play Services Unavailable - Falling back to default Geolocator implementation");
				_geolocator = new GeolocatorImplementation();
			}
		}

		public Task<Position> GetPositionAsync(TimeSpan? timeoutMilliseconds = null, CancellationToken? token = null, bool includeHeading = false) =>
			_geolocator.GetPositionAsync(timeoutMilliseconds, token, includeHeading);

		public Task<bool> StartListeningAsync(TimeSpan minimumTime, double minimumDistance, bool includeHeading = false, ListenerSettings listenerSettings = null) =>
			_geolocator.StartListeningAsync(minimumTime, minimumDistance, includeHeading, listenerSettings);

		public Task<bool> StopListeningAsync() => _geolocator.StopListeningAsync();

		public Task<Position> GetLastKnownLocationAsync() => _geolocator.GetLastKnownLocationAsync();

		public Task<IEnumerable<Address>> GetAddressesForPositionAsync(Position position, string mapKey = null) =>
			_geolocator.GetAddressesForPositionAsync(position, mapKey);

		public double DesiredAccuracy
		{
			get => _geolocator.DesiredAccuracy;
			set => _geolocator.DesiredAccuracy = value;
		}

		public bool IsListening => _geolocator.IsListening;

		public bool SupportsHeading => _geolocator.SupportsHeading;

		public bool IsGeolocationAvailable => _geolocator.IsGeolocationAvailable;

		public bool IsGeolocationEnabled => _geolocator.IsGeolocationEnabled;

		public event EventHandler<PositionErrorEventArgs> PositionError
		{
			add => _geolocator.PositionError += value;
			remove => _geolocator.PositionError -= value;
		}

		public event EventHandler<PositionEventArgs> PositionChanged
		{
			add => _geolocator.PositionChanged += value;
			remove => _geolocator.PositionChanged -= value;
		}
    }
}
