using System;
using Plugin.Geolocator.Abstractions;


namespace Plugin.Geolocator
{
    /// <summary>
    /// Cross platform Geolocator implemenations
    /// </summary>
    public class CrossGeolocator
    {
        static IGeolocator current;


        /// <summary>
        /// Current settings to use
        /// </summary>
        public static IGeolocator Current
        {
            get
            {
#if BAIT
                if (current == null)
                    throw new NotImplementedException("[Plugin.Geolocator] This functionality is not implemented in the portable version of this assembly.  You should reference the NuGet package from your main application project in order to reference the platform-specific implementation.");
#else
                current = current ?? new GeolocatorImplementation();
#endif
                return current;
            }
            set => current = value;
        }
    }
}
