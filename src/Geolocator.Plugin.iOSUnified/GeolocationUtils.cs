using CoreLocation;
using Address = Plugin.Geolocator.Abstractions.Address;

namespace Plugin.Geolocator
{
    public static class GeolocationUtils
    {
        internal static Address ToAddress(this CLPlacemark address)
        {
            var a = new Address
            {
                Longitude = address.Location.Coordinate.Longitude,
                Latitude = address.Location.Coordinate.Latitude,
                FeatureName = address.Name,
                PostalCode = address.PostalCode,
                SubLocality = address.SubLocality,
                CountryCode = address.IsoCountryCode,
                CountryName = address.Country
            };

            return a;
        }
    }
}