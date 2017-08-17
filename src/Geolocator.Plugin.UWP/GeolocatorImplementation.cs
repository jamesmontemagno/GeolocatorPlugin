using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Plugin.Geolocator.Abstractions;
using System.Threading;
using Windows.Services.Maps;

namespace Plugin.Geolocator
{
    /// <summary>
    /// Implementation for Geolocator
    /// </summary>
    public class GeolocatorImplementation : IGeolocator
    {

        bool isListening;
        double desiredAccuracy;
        Windows.Devices.Geolocation.Geolocator locator = new Windows.Devices.Geolocation.Geolocator();

		/// <summary>
		/// Constructor for Implementation
		/// </summary>
        public GeolocatorImplementation()
        {
            DesiredAccuracy = 100;
        }

        /// <summary>
        /// Position error event handler
        /// </summary>
        public event EventHandler<PositionEventArgs> PositionChanged;

        /// <summary>
        /// Position changed event handler
        /// </summary>
        public event EventHandler<PositionErrorEventArgs> PositionError;

        /// <summary>
        /// Gets if device supports heading
        /// </summary>
        public bool SupportsHeading => false;

        /// <summary>
        /// Gets if geolocation is available on device
        /// </summary>
        public bool IsGeolocationAvailable
        {
            get
            {
                var status = GetGeolocatorStatus();

                while (status == PositionStatus.Initializing)
                {
                    Task.Delay(10).Wait();
                    status = GetGeolocatorStatus();
                }

                return status != PositionStatus.NotAvailable;
            }
        }

        /// <summary>
        /// Gets if geolocation is enabled on device
        /// </summary>
        public bool IsGeolocationEnabled
        {
            get
            {
                var status = GetGeolocatorStatus();

                while (status == PositionStatus.Initializing)
                {
                    Task.Delay(10).Wait();
                    status = GetGeolocatorStatus();
                }

                return status != PositionStatus.Disabled && status != PositionStatus.NotAvailable;
            }
        }

        /// <summary>
        /// Desired accuracy in meters
        /// </summary>
        public double DesiredAccuracy
        {
            get { return desiredAccuracy; }
            set
            {
                desiredAccuracy = value;
                GetGeolocator().DesiredAccuracy = (value < 100) ? PositionAccuracy.High : PositionAccuracy.Default;
            }
        }

        /// <summary>
        /// Gets if you are listening for location changes
        /// </summary>
        public bool IsListening => isListening;


        /// <summary>
        /// Gets the last known and most accurate location.
        /// This is usually cached and best to display first before querying for full position.
        /// </summary>
        /// <returns>Best and most recent location or null if none found</returns>
        public Task<Position> GetLastKnownLocationAsync() =>
			Task.Factory.StartNew<Position>(()=> { return null; });


		/// <summary>
		/// Gets position async with specified parameters
		/// </summary>
		/// <param name="timeout">Timeout to wait, Default Infinite</param>
		/// <param name="cancelToken">Cancelation token</param>
		/// <param name="includeHeading">If you would like to include heading</param>
		/// <returns>Position</returns>
		public Task<Position> GetPositionAsync(TimeSpan? timeout, CancellationToken? cancelToken = null, bool includeHeading = false)
        {
            var timeoutMilliseconds = timeout.HasValue ? (int)timeout.Value.TotalMilliseconds : Timeout.Infite;

            if (timeoutMilliseconds < 0 && timeoutMilliseconds != Timeout.Infite)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            if (!cancelToken.HasValue)
                cancelToken = CancellationToken.None;

            var pos = GetGeolocator().GetGeopositionAsync(TimeSpan.FromTicks(0), TimeSpan.FromDays(365));
            cancelToken.Value.Register(o => ((IAsyncOperation<Geoposition>)o).Cancel(), pos);


            var timer = new Timeout(timeoutMilliseconds, pos.Cancel);

            var tcs = new TaskCompletionSource<Position>();

            pos.Completed = (op, s) =>
            {
                timer.Cancel();

                switch (s)
                {
                    case AsyncStatus.Canceled:
                        tcs.SetCanceled();
                        break;
                    case AsyncStatus.Completed:
                        tcs.SetResult(GetPosition(op.GetResults()));
                        break;
                    case AsyncStatus.Error:
                        var ex = op.ErrorCode;
                        if (ex is UnauthorizedAccessException)
                            ex = new GeolocationException(GeolocationError.Unauthorized, ex);

                        tcs.SetException(ex);
                        break;
                }
            };

            return tcs.Task;
        }

        void SetMapKey(string mapKey)
        {
            if (string.IsNullOrWhiteSpace(mapKey) && string.IsNullOrWhiteSpace(MapService.ServiceToken))
            {
                System.Diagnostics.Debug.WriteLine("Map API key is required on UWP to reverse geolocate.");
                throw new ArgumentNullException(nameof(mapKey));

            }

            if (!string.IsNullOrWhiteSpace(mapKey))
                MapService.ServiceToken = mapKey;
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

            SetMapKey(mapKey);

            var queryResults =
                await MapLocationFinder.FindLocationsAtAsync(
                        new Geopoint(new BasicGeoposition { Latitude = position.Latitude, Longitude = position.Longitude })).AsTask();

            return queryResults?.Locations.ToAddresses();
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

            SetMapKey(mapKey);

            var queryResults = await MapLocationFinder.FindLocationsAsync(address, null, 10);
            var positions = new List<Position>();
            if (queryResults?.Locations == null)
                return positions;

			foreach (var p in queryResults.Locations)
				positions.Add(new Position
				{
					Latitude = p.Point.Position.Latitude,
					Longitude = p.Point.Position.Longitude
				});

            return positions;
        }


		/// <summary>
		/// Start listening for changes
		/// </summary>
		/// <param name="minimumTime">Time</param>
		/// <param name="minimumDistance">Distance</param>
		/// <param name="includeHeading">Include heading or not</param>
		/// <param name="listenerSettings">Optional settings (iOS only)</param>
		public Task<bool> StartListeningAsync(TimeSpan minimumTime, double minimumDistance, bool includeHeading = false, ListenerSettings listenerSettings = null)
        {
			if (minimumTime.TotalMilliseconds <= 0 && minimumDistance <= 0)
				throw new ArgumentException("You must specify either a minimumTime or minimumDistance, setting a minimumDistance will always take precedence over minTime");

            if (minimumTime.TotalMilliseconds < 0)
                throw new ArgumentOutOfRangeException(nameof(minimumTime));

            if (minimumDistance < 0)
                throw new ArgumentOutOfRangeException(nameof(minimumDistance));

            if (isListening)
                throw new InvalidOperationException();

            isListening = true;

            var loc = GetGeolocator();

			loc.ReportInterval = (uint)minimumTime.TotalMilliseconds;
            loc.MovementThreshold = minimumDistance;
            loc.PositionChanged += OnLocatorPositionChanged;
            loc.StatusChanged += OnLocatorStatusChanged;

            return Task.FromResult(true);
        }

        /// <summary>
        /// Stop listening
        /// </summary>
        public Task<bool> StopListeningAsync()
        {
            if (!isListening)
                return Task.FromResult(true);

            locator.PositionChanged -= OnLocatorPositionChanged;
            locator.StatusChanged -= OnLocatorStatusChanged;
            isListening = false;

            return Task.FromResult(true);
        }


        private async void OnLocatorStatusChanged(Windows.Devices.Geolocation.Geolocator sender, StatusChangedEventArgs e)
        {
            GeolocationError error;
            switch (e.Status)
            {
                case PositionStatus.Disabled:
                    error = GeolocationError.Unauthorized;
                    break;

                case PositionStatus.NoData:
                    error = GeolocationError.PositionUnavailable;
                    break;

                default:
                    return;
            }

            if (isListening)
            {
                await StopListeningAsync();
                OnPositionError(new PositionErrorEventArgs(error));
            }

            locator = null;
        }

        private void OnLocatorPositionChanged(Windows.Devices.Geolocation.Geolocator sender, PositionChangedEventArgs e)
        {
            OnPositionChanged(new PositionEventArgs(GetPosition(e.Position)));
        }

        private void OnPositionChanged(PositionEventArgs e) => PositionChanged?.Invoke(this, e);


        private void OnPositionError(PositionErrorEventArgs e) => PositionError?.Invoke(this, e);


        private Windows.Devices.Geolocation.Geolocator GetGeolocator()
        {
            var loc = locator;
            if (loc == null)
            {
                locator = new Windows.Devices.Geolocation.Geolocator();
                locator.StatusChanged += OnLocatorStatusChanged;
                loc = locator;
            }

            return loc;
        }

        private PositionStatus GetGeolocatorStatus()
        {
            var loc = GetGeolocator();
            return loc.LocationStatus;
        }

        private static Position GetPosition(Geoposition position)
        {
            var pos = new Position
            {
                Accuracy = position.Coordinate.Accuracy,
                Latitude = position.Coordinate.Point.Position.Latitude,
                Longitude = position.Coordinate.Point.Position.Longitude,
                Timestamp = position.Coordinate.Timestamp.ToUniversalTime(),
            };

            if (position.Coordinate.Heading != null)
                pos.Heading = position.Coordinate.Heading.Value;

            if (position.Coordinate.Speed != null)
                pos.Speed = position.Coordinate.Speed.Value;

            if (position.Coordinate.AltitudeAccuracy.HasValue)
                pos.AltitudeAccuracy = position.Coordinate.AltitudeAccuracy.Value;

            pos.Altitude = position.Coordinate.Point.Position.Altitude;

            return pos;
        }
    }
}
