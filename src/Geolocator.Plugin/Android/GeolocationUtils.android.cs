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

		static int twoMinutes = 120000;

		internal static bool IsBetterLocation(Location location, Location bestLocation)
		{

			if (bestLocation == null)
				return true;

			var timeDelta = location.Time - bestLocation.Time;
			var isSignificantlyNewer = timeDelta > twoMinutes;
			var isSignificantlyOlder = timeDelta < -twoMinutes;
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

			p.HasAccuracy = location.HasAccuracy;
			if (location.HasAccuracy)
				p.Accuracy = location.Accuracy;

			p.HasAltitude = location.HasAltitude;
			if (location.HasAltitude)
				p.Altitude = location.Altitude;

			p.HasHeading = location.HasBearing;
			if (location.HasBearing)
				p.Heading = location.Bearing;

			p.HasSpeed = location.HasSpeed;
			if (location.HasSpeed)
				p.Speed = location.Speed;

			p.HasLatitudeLongitude = true;
			p.Longitude = location.Longitude;
			p.Latitude = location.Latitude;
			p.Timestamp = location.GetTimestamp();

			if ((int)Android.OS.Build.VERSION.SdkInt >= 18)
				p.IsFromMockProvider = location.IsFromMockProvider;
			else
				p.IsFromMockProvider = false;

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

		private static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		internal static DateTimeOffset GetTimestamp(this Location location)
		{
			try
			{
				return new DateTimeOffset(epoch.AddMilliseconds(location.Time));
			}
			catch (Exception)
			{
				return new DateTimeOffset(epoch);
			}
		}
	}
}