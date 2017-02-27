using Microsoft.Phone.Maps.Services;
using Plugin.Geolocator.Abstractions;
using System.Collections.Generic;
using System.Device.Location;
using System.Linq;

namespace Plugin.Geolocator
{
    static class GeolocatorUtils
    {
        internal static GeoPositionAccuracy ToAccuracy(this double desiredAccuracy)
        {
            if (desiredAccuracy < 100)
                return GeoPositionAccuracy.High;

            return GeoPositionAccuracy.Default;
        }

        internal static Position ToPosition(this GeoPosition<GeoCoordinate> position)
        {
            if (position.Location.IsUnknown)
                return null;

            var p = new Position();
            p.Accuracy = position.Location.HorizontalAccuracy;
            p.Longitude = position.Location.Longitude;
            p.Latitude = position.Location.Latitude;

            if (!double.IsNaN(position.Location.VerticalAccuracy) && !double.IsNaN(position.Location.Altitude))
            {
                p.AltitudeAccuracy = position.Location.VerticalAccuracy;
                p.Altitude = position.Location.Altitude;
            }

            if (!double.IsNaN(position.Location.Course))
                p.Heading = position.Location.Course;

            if (!double.IsNaN(position.Location.Speed))
                p.Speed = position.Location.Speed;

            p.Timestamp = position.Timestamp.ToUniversalTime();

            return p;
        }

        internal static IEnumerable<Address> ToAddresses(this IEnumerable<MapLocation> addresses)
        {
            return addresses.Select(address => new Address
            {
                Longitude = address.GeoCoordinate.Longitude,
                Latitude = address.GeoCoordinate.Latitude,
                FeatureName = address.Information.Name,
                PostalCode = address.Information.Address.PostalCode,
                CountryCode = address.Information.Address.CountryCode,
                CountryName = address.Information.Address.Country,
                Thoroughfare = address.Information.Address.Street,
                SubThoroughfare = address.Information.Address.Township,
                Locality = address.Information.Address.City
            });
        }
    }
}
