using Plugin.Geolocator.Abstractions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Tizen.Maps;

namespace Plugin.Geolocator
{
    internal static class GeolocatorUtils
    {
		private static double longitude = 0;
		private static double latitude = 0;
		internal static void Positions(this Position position)
		{
			latitude = position.Latitude;
			longitude = position.Longitude;
		}

		internal static IEnumerable<Address> ToAddresses(this IEnumerable<PlaceAddress> addresses)
		{
			return addresses.Select(address => new Address
			{
				Latitude = latitude,
				Longitude = longitude,
				FeatureName = address.County,
				PostalCode = address.PostalCode,
				CountryCode = address.CountryCode,
				CountryName = address.Country,
				Thoroughfare = address.Street,
				SubThoroughfare = address.District,
				Locality = address.City,
				//SubLocality = address.City,
				AdminArea = address.State,
				SubAdminArea = address.Building
			});
        }
    }
}
