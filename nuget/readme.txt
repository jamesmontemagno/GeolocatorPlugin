Geolocator Readme

For latest changes: https://github.com/jamesmontemagno/GeolocatorPlugin/blob/master/CHANGELOG.md

## News
- Plugins have moved to .NET Standard and have some important changes! Please read my blog:
http://motzcod.es/post/162402194007/plugins-for-xamarin-go-dotnet-standard


## Android 
You must set your app to compile against API 25 or higher and be able to install the latest android support libraries.

## Android specific in your BaseActivity or MainActivity (for Xamarin.Forms) add this code:

Add usings:

using Plugin.Permissions;
using Plugin.Permissions.Abstractions;

Then add to Activity:

public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
{
    PermissionsImplementation.Current.OnRequestPermissionsResult(requestCode, permissions, grantResults);
}

Android:
The ACCESS_COARSE_LOCATION & ACCESS_FINE_LOCATION permissions are required, but the library will automatically add this for you. 
Additionally, if your users are running Marshmallow the Plugin will automatically prompt them for runtime permissions.

By adding these permissions Google Play will automatically filter out devices without specific hardward. You can get around this by adding the following to your AssemblyInfo.cs file in your Android project:

[assembly: UsesFeature("android.hardware.location", Required = false)]
[assembly: UsesFeature("android.hardware.location.gps", Required = false)]
[assembly: UsesFeature("android.hardware.location.network", Required = false)]

iOS:
In iOS 8 you now have to call either RequestWhenInUseAuthorization or RequestAlwaysAuthorization on the location manager. Additionally you need to add either the concisely named NSLocationWhenInUseUsageDescription or NSLocationAlwaysUsageDescription to your Info.plist. 
See:  http://motzcod.es/post/97662738237/scanning-for-ibeacons-in-ios-8

You will need to add a new string entry called NSLocationWhenInUseUsageDescription or NSLocationAlwaysUsageDescription.

iOS Background updates:
New in iOS 9 allowsBackgroundLocationUpdates must be set if using in a background agent. This is exposed via the ListenerSettings
that are passed to StartListeningAysnc. The presence of the UIBackgroundModes key with the location value is required for background 
updates; you use this property to enable and disable the behavior based on your app’s behavior.

UWP:
You must set the Location permission.
