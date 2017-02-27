//
//  Copyright 2011-2013, Xamarin Inc.
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//
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

        readonly object positionSync = new object();
        Position lastPosition;

        /// <summary>
        /// Default constructor
        /// </summary>
        public GeolocatorImplementation()
        {
            DesiredAccuracy = 100;
        }

        string[] Providers
        {
            get
            {
                if ((allProviders?.Length ?? 0) == 0)
                    allProviders = Manager.GetProviders(enabledOnly: false).ToArray();

                return allProviders;
            }
        }

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
        public bool IsGeolocationEnabled => Providers.Any(Manager.IsProviderEnabled);


        public async Task<Position> GetLastKnownLocationAsync()
        {
            var hasPermission = await CheckPermissions();
            if (!hasPermission)
                return null;

            Location bestLocation = null;
            foreach (var provider in Providers)
            {
                var location = Manager.GetLastKnownLocation(provider);
                if (location != null && GeolocationUtils.IsBetterLocation(location, bestLocation))
                    bestLocation = location;
            }

            return bestLocation == null ? null : bestLocation.ToPosition();

        }

        async Task<bool> CheckPermissions()
        {
            var status = await CrossPermissions.Current.CheckPermissionStatusAsync(Permissions.Abstractions.Permission.Location).ConfigureAwait(false);
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


        /// <inheritdoc/>
        public async Task<Position> GetPositionAsync(TimeSpan? timeout, CancellationToken? cancelToken = null, bool includeHeading = false)
        {
            var timeoutMilliseconds = timeout.HasValue ? (int)timeout.Value.TotalMilliseconds : Timeout.Infinite;

            if (timeoutMilliseconds <= 0 && timeoutMilliseconds != Timeout.Infinite)
                throw new ArgumentOutOfRangeException(nameof(timeout), "timeout must be greater than or equal to 0");

            if (!cancelToken.HasValue)
                cancelToken = CancellationToken.None;

            var hasPermission = await CheckPermissions();
            if (!hasPermission)
                return null;

            var tcs = new TaskCompletionSource<Position>();

            if (!IsListening)
            {
                GeolocationSingleListener singleListener = null;
                singleListener = new GeolocationSingleListener(Manager, (float)DesiredAccuracy, timeoutMilliseconds, Providers.Where(Manager.IsProviderEnabled),
                    finishedCallback: () =>
                {
                    for (int i = 0; i < Providers.Length; ++i)
                        Manager.RemoveUpdates(singleListener);
                });

                if (cancelToken != CancellationToken.None)
                {
                    cancelToken.Value.Register(() =>
                    {
                        singleListener.Cancel();

                        for (int i = 0; i < Providers.Length; ++i)
                            Manager.RemoveUpdates(singleListener);
                    }, true);
                }

                try
                {
                    var looper = Looper.MyLooper() ?? Looper.MainLooper;

                    int enabled = 0;
                    for (int i = 0; i < Providers.Length; ++i)
                    {
                        if (Manager.IsProviderEnabled(Providers[i]))
                            enabled++;

                        Manager.RequestLocationUpdates(Providers[i], 0, 0, singleListener, looper);
                    }

                    if (enabled == 0)
                    {
                        for (int i = 0; i < Providers.Length; ++i)
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

            return await tcs.Task.ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieve addresses for position.
        /// </summary>
        /// <param name="position">Desired position (latitude and longitude)</param>
        /// <returns>Addresses of the desired position</returns>
        public async Task<IEnumerable<Address>> GetAddressesForPositionAsync(Position position)
        {
            if (position == null)
                return null;

            var geocoder = new Geocoder(Application.Context);
            var addressList = await geocoder.GetFromLocationAsync(position.Latitude, position.Longitude, 10);
            return addressList.ToAddresses();
        }

        /// <inheritdoc/>
        public async Task<bool> StartListeningAsync(TimeSpan minTime, double minDistance, bool includeHeading = false, ListenerSettings settings = null)
        {
            var hasPermission = await CheckPermissions();
            if (!hasPermission)
                return false;


            var minTimeMilliseconds = minTime.TotalMilliseconds;
            if (minTimeMilliseconds < 0)
                throw new ArgumentOutOfRangeException("minTime");
            if (minDistance < 0)
                throw new ArgumentOutOfRangeException("minDistance");
            if (IsListening)
                throw new InvalidOperationException("This Geolocator is already listening");

            listener = new GeolocationContinuousListener(Manager, minTime, Providers);
            listener.PositionChanged += OnListenerPositionChanged;
            listener.PositionError += OnListenerPositionError;

            Looper looper = Looper.MyLooper() ?? Looper.MainLooper;
            for (int i = 0; i < Providers.Length; ++i)
                Manager.RequestLocationUpdates(Providers[i], (long)minTimeMilliseconds, (float)minDistance, listener, looper);

            return true;
        }
        /// <inheritdoc/>
        public Task<bool> StopListeningAsync()
        {
            if (listener == null)
                return Task.FromResult(true);

            listener.PositionChanged -= OnListenerPositionChanged;
            listener.PositionError -= OnListenerPositionError;

            for (int i = 0; i < Providers.Length; ++i)
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
    }
}