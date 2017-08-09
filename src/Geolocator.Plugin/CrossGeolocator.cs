using Plugin.Geolocator.Abstractions;
using System;

namespace Plugin.Geolocator
{
    /// <summary>
    /// Cross platform Geolocator implemenations
    /// </summary>
    public class CrossGeolocator
    {
        static Lazy<IGeolocator> _implementation;
        /// <summary>
        /// Gets if the plugin is supported on the current platform.
        /// </summary>
        public static bool IsSupported => Current != null;

        /// <summary>
        /// Current plugin implementation to use
        /// </summary>
        public static IGeolocator Current
        {
            get
            {
#if !NETSTANDARD1_0
                if (_implementation == null)
                {
					Console.WriteLine($"CrossGeolocator Init: Using default Geolocator");
					Init(() => new GeolocatorImplementation());
    			}
#endif

				var ret = _implementation?.Value;
                if (ret == null)
                {
                    throw NotImplementedInReferenceAssembly();
                }
                return ret;
            }
        }

#if !NETSTANDARD1_0
        public static void Init(Func<IGeolocator> action)
        {
            if (_implementation != null) throw new Exception("CrossGeolocator already initialized");
    		_implementation = new Lazy<IGeolocator>(action, System.Threading.LazyThreadSafetyMode.PublicationOnly);
        }
#endif

		internal static Exception NotImplementedInReferenceAssembly() =>
			new NotImplementedException("This functionality is not implemented in the portable version of this assembly.  You should reference the NuGet package from your main application project in order to reference the platform-specific implementation.");
        
    }
}
