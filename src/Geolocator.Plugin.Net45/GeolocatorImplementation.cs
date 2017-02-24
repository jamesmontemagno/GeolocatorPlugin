using Plugin.Geolocator.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Plugin.Geolocator
{
    /// <summary>
    /// Geolocator implementation
    /// </summary>
    public class GeolocatorImplementation : IGeolocator
    {
        /// <summary>
        /// Desired accuracy in meters
        /// </summary>
        public double DesiredAccuracy { get; set; } = 100;

        /// <summary>
        /// Gets if geolocation is available on device
        /// </summary>
        public bool IsGeolocationAvailable => false;

        /// <summary>
        /// Gets if geolocation is enabled on device
        /// </summary>
        public bool IsGeolocationEnabled => false;

        /// <summary>
        /// Gets if you are listening for location changes
        /// </summary>
        public bool IsListening => false;

        /// <summary>
        /// Gets if device supports heading
        /// </summary>
        public bool SupportsHeading => false;

        /// <summary>
        /// Position changed
        /// </summary>
        public event EventHandler<PositionEventArgs> PositionChanged;
        /// <summary>
        /// Position error
        /// </summary>
        public event EventHandler<PositionErrorEventArgs> PositionError;

        /// <summary>
        /// Gets position async with specified parameters
        /// </summary>
        /// <param name="timeout">Timeout to wait, Default Infinite</param>
        /// <param name="token">Cancelation token</param>
        /// <param name="includeHeading">If you would like to include heading</param>
        /// <returns>Position</returns>
        public Task<Position> GetPositionAsync(TimeSpan? timeout, CancellationToken? token = default(CancellationToken?), bool includeHeading = false)
            => Task.FromResult<Position>(null);

        /// <summary>
        /// Gets position async and reverse geocode
        /// </summary>
        /// <returns>Address of the current position</returns>
        public Task<Address> ReverseGeocodeCurrentLocation()
            => Task.FromResult<Address>(null);

        /// <summary>
        /// Reverse geocode a position
        /// </summary>
        /// <param name="latitude">Desired Latitude</param>
        /// <param name="longitude">Desired Longitude</param>
        /// <returns>Address of the desired position</returns>
        public Task<Address> ReverseGeocodeLocation(double latitude, double longitude)
            => Task.FromResult<Address>(null);

        /// <summary>
        /// Start listenting
        /// </summary>
        /// <param name="minTime"></param>
        /// <param name="minDistance"></param>
        /// <param name="includeHeading"></param>
        /// <returns></returns>
        public Task<bool> StartListeningAsync(TimeSpan minTime, double minDistance, bool includeHeading = false, ListenerSettings settings = null)
            => Task.FromResult(false);

        /// <summary>
        /// Stop listening
        /// </summary>
        /// <returns></returns>
        public Task<bool> StopListeningAsync()
            => Task.FromResult(false);

        /// <summary>
        /// Gets the last known and most accurate location.
        /// This is usually cached and best to display first before querying for full position.
        /// </summary>
        /// <returns>Best and most recent location or null if none found</returns>
        public Task<Position> GetLastKnownLocationAsync() => null;
    }
}
