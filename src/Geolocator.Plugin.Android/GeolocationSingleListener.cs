using System;
using System.Threading.Tasks;
using Android.Locations;
using Android.OS;
using System.Threading;
using System.Collections.Generic;
using Plugin.Geolocator.Abstractions;
using Android.Runtime;

namespace Plugin.Geolocator
{
    [Preserve(AllMembers = true)]
    internal class GeolocationSingleListener
       : Java.Lang.Object, ILocationListener
    {

        readonly object locationSync = new object();
        Location bestLocation;


        readonly Action finishedCallback;
        readonly float desiredAccuracy;
        readonly Timer timer;
        readonly TaskCompletionSource<Position> completionSource = new TaskCompletionSource<Position>();
        HashSet<string> activeProviders = new HashSet<string>();

        public GeolocationSingleListener(LocationManager manager, float desiredAccuracy, int timeout, IEnumerable<string> activeProviders, Action finishedCallback)
        {
            this.desiredAccuracy = desiredAccuracy;
            this.finishedCallback = finishedCallback;

            this.activeProviders = new HashSet<string>(activeProviders);

            foreach(var provider in activeProviders)
            {
                var location = manager.GetLastKnownLocation(provider);
                if (location != null && GeolocationUtils.IsBetterLocation(location, bestLocation))
                    bestLocation = location;
            }
            

            if (timeout != Timeout.Infinite)
                timer = new Timer(TimesUp, null, timeout, 0);
        }

        public Task<Position> Task => completionSource.Task; 
        

        public void OnLocationChanged(Location location)
        {
            if (location.Accuracy <= desiredAccuracy)
            {
                Finish(location);
                return;
            }

            lock (locationSync)
            {
                if (GeolocationUtils.IsBetterLocation(location, bestLocation))
                    bestLocation = location;
            }
        }

        

        public void OnProviderDisabled(string provider)
        {
            lock (activeProviders)
            {
                if (activeProviders.Remove(provider) && activeProviders.Count == 0)
                    completionSource.TrySetException(new GeolocationException(GeolocationError.PositionUnavailable));
            }
        }

        public void OnProviderEnabled(string provider)
        {
            lock (activeProviders)
              activeProviders.Add(provider);
        }

        public void OnStatusChanged(string provider, Availability status, Bundle extras)
        {
            switch (status)
            {
                case Availability.Available:
                    OnProviderEnabled(provider);
                    break;

                case Availability.OutOfService:
                    OnProviderDisabled(provider);
                    break;
            }
        }

        public void Cancel() =>  completionSource.TrySetCanceled();

        private void TimesUp(object state)
        {
            lock (locationSync)
            {
                if (bestLocation == null)
                {
                    if (completionSource.TrySetCanceled())
                        finishedCallback?.Invoke();
                }
                else
                {
                    Finish(bestLocation);
                }
            }
        }

        private void Finish(Location location)
        {
            finishedCallback?.Invoke();
            completionSource.TrySetResult(location.ToPosition());
        }
    }
}