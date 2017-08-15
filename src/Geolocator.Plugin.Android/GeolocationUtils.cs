using Android.Locations;
using Plugin.Geolocator.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using Address = Plugin.Geolocator.Abstractions.Address;

namespace Plugin.Geolocator
{
    public static class GeolocationUtils
    {

        static int TwoMinutes = 120000;

        internal static bool IsBetterLocation(Location location, Location bestLocation)
        {

            if (bestLocation == null)
                return true;

            var timeDelta = location.Time - bestLocation.Time;
            var isSignificantlyNewer = timeDelta > TwoMinutes;
            var isSignificantlyOlder = timeDelta < -TwoMinutes;
            var isNewer = timeDelta > 0;

            if (isSignificantlyNewer)
                return true;

            if (isSignificantlyOlder)
                return false;

            var accuracyDelta = (int)(location.Accuracy - bestLocation.Accuracy);
            var isLessAccurate = accuracyDelta > 0;
            var isMoreAccurate = accuracyDelta < 0;
            var isSignificantlyLessAccurage = accuracyDelta > 200;

            var isFromSameProvider = IsSameProvider(location.Provider, bestLocation.Provider);

            if (isMoreAccurate)
                return true;

            if (isNewer && !isLessAccurate)
                return true;

            if (isNewer && !isSignificantlyLessAccurage && isFromSameProvider)
                return true;

            return false;


        }

        internal static bool IsSameProvider(string provider1, string provider2)
        {
            if (provider1 == null)
                return provider2 == null;

            return provider1.Equals(provider2);
        }

        internal static Position ToPosition(this Location location)
        {
            var p = new Position();
            if (location.HasAccuracy)
                p.Accuracy = location.Accuracy;
            if (location.HasAltitude)
                p.Altitude = location.Altitude;
            if (location.HasBearing)
                p.Heading = location.Bearing;
            if (location.HasSpeed)
                p.Speed = location.Speed;

            p.Longitude = location.Longitude;
            p.Latitude = location.Latitude;
            p.Timestamp = location.GetTimestamp();
            return p;
        }

        internal static IEnumerable<Address> ToAddresses(this IEnumerable<Android.Locations.Address> addresses)
        {
            return addresses.Select(address => new Address
            {
                Longitude = address.Longitude,
                Latitude = address.Latitude,
                FeatureName = address.FeatureName,
                PostalCode = address.PostalCode,
                SubLocality = address.SubLocality,
                CountryCode = address.CountryCode,
                CountryName = address.CountryName,
                Thoroughfare = address.Thoroughfare,
                SubThoroughfare = address.SubThoroughfare,
                Locality = address.Locality,
                AdminArea = address.AdminArea,
                SubAdminArea = address.SubAdminArea
            });
        }

        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        internal static DateTimeOffset GetTimestamp(this Location location)
        {
            try
            {
                return new DateTimeOffset(Epoch.AddMilliseconds(location.Time));
            }
            catch (Exception e)
            {
                return new DateTimeOffset(Epoch);
            }
        }
    }
}