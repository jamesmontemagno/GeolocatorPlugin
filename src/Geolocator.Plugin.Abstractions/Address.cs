
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
                throw new ArgumentNullException("address");

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
        /// Gets or sets the latitude.
        /// </summary>
        public string CountryCode { get; set; }

        /// <summary>
        /// Gets or sets the longitude.
        /// </summary>
        public string CountryName { get; set; }

        public string FeatureName { get; set; }
        public string PostalCode { get; set; }
        public string SubLocality { get; set; }
        public string Thoroughfare { get; set; }
        public string SubThoroughfare { get; set; }
        public string Locality { get; set; }
    }
}
