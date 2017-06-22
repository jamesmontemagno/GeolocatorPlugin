using Plugin.Geolocator.Abstractions;
using System;

namespace Plugin.Geolocator
{
    /// <summary>
    /// Cross platform Geolocator implemenations
    /// </summary>
    public class CrossGeolocator
    {
        static Lazy<IGeolocator> implementation = new Lazy<IGeolocator>(() => CreateGeolocator(), System.Threading.LazyThreadSafetyMode.PublicationOnly);
        /// <summary>
        /// Gets if the plugin is supported on the current platform.
        /// </summary>
        public static bool IsSupported => implementation.Value == null ? false : true;

        /// <summary>
        /// Current plugin implementation to use
        /// </summary>
        public static IGeolocator Current
        {
            get
            {
                var ret = implementation.Value;
                if (ret == null)
                {
                    throw NotImplementedInReferenceAssembly();
                }
                return ret;
            }
        }

        static IGeolocator CreateGeolocator()
        {
#if NETSTANDARD1_0
            return null;
#else
			return new GeolocatorImplementation();
#endif
        }

        internal static Exception NotImplementedInReferenceAssembly() =>
			new NotImplementedException("This functionality is not implemented in the portable version of this assembly.  You should reference the NuGet package from your main application project in order to reference the platform-specific implementation.");
        
    }
}
