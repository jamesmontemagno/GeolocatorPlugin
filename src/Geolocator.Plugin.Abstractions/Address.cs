using System;

namespace Plugin.Geolocator.Abstractions
{
    public class Address
    {
        public Address()
        {
        }

        public Address(Address address)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));

            CountryCode = address.CountryCode;
            CountryName = address.CountryName;
            Latitude = address.Latitude;
            Longitude = address.Longitude;
            FeatureName = address.FeatureName;
            PostalCode = address.PostalCode;
            SubLocality = address.SubLocality;
            Thoroughfare = address.Thoroughfare;
            SubThoroughfare = address.SubThoroughfare;
        }

        /// <summary>
        /// Gets or sets the latitude.
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// Gets or sets the longitude.
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// Gets or sets the country ISO code.
        /// </summary>
        public string CountryCode { get; set; }

        /// <summary>
        /// Gets or sets the country name.
        /// </summary>
        public string CountryName { get; set; }

        /// <summary>
        /// Gets or sets a featured name.
        /// </summary>
        public string FeatureName { get; set; }

        /// <summary>
        /// Gets or sets a postal code.
        /// </summary>
        public string PostalCode { get; set; }

        /// <summary>
        /// Gets or sets a sub locality.
        /// </summary>
        public string SubLocality { get; set; }

        /// <summary>
        /// Gets or sets a street name.
        /// </summary>
        public string Thoroughfare { get; set; }

        /// <summary>
        /// Gets or sets optional info: sub street or region.
        /// </summary>
        public string SubThoroughfare { get; set; }

        /// <summary>
        /// Gets or sets a city/town.
        /// </summary>
        public string Locality { get; set; }
    }
}
