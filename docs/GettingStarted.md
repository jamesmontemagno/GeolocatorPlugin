# Getting Started

## Setup
* NuGet: [Xam.Plugin.Geolocator](http://www.nuget.org/packages/Xam.Plugin.Geolocator) [![NuGet](https://img.shields.io/nuget/v/Xam.Plugin.Geolocator.svg?label=NuGet)](https://www.nuget.org/packages/Xam.Plugin.Geolocator/)
* `PM> Install-Package Xam.Plugin.Geolocator`
* Install into ALL of your projects, include client projects.
* namespace: `using Plugin.Geolocator;`


## Using Geolocator APIs
It is drop dead simple to gain access to the Geolocator APIs in any project. All you need to do is get a reference to the current instance of IGeolocator via `CrossGeolocator.Current`:

```csharp
public Task<bool> IsLocationAvailable()
{
    return CrossGeolocator.Current.GetIsGeolocationAvailableAsync();
}
```

There may be instances where you install a plugin into a platform that it isn't supported yet. This means you will have access to the interface, but no implementation exists. You can make a simple check before calling any API to see if it is supported on the platform where the code is running. This is nifty when unit testing:

```csharp
public Task<bool> IsLocationAvailable()
{
    if(!CrossGeolocator.IsSupported)
        return false;

    return CrossGeolocator.Current.GetIsGeolocationAvailableAsync();
}
```


## Android + Google Play Location Services 
Introduced in version 5.0 the Android implementation uses the Fused client which is part of Google Play services. The plugin will attempt to automatically detect if Google Play services are available on the device and use this implementation. You can default to the non-Google Play services version by setting this flag in your Android project startup code:

```
CrossGeolocator.UseFusedLocationProvider = false;
```


## Permissions & Additional Setup Considerations
Before making any calls to the geolocator that requires the permissions, you should consider checking that the user has granted proper permission. The geolocator plugin will attempt to ask for permission, but it is not guaranteed.

### Android:

This plugin uses the [Current Activity Plugin](https://github.com/jamesmontemagno/CurrentActivityPlugin/blob/master/README.md) to get access to the current Android Activity. Be sure to complete the full setup if a MainApplication.cs file was not automatically added to your application. Please fully read through the [Current Activity Plugin Documentation](https://github.com/jamesmontemagno/CurrentActivityPlugin/blob/master/README.md). At an absolute minimum you must set the following in your Activity's OnCreate method:

```csharp
Plugin.CurrentActivity.CrossCurrentActivity.Current.Activity = this;
```

It is highly recommended that you use a custom Application that are outlined in the Current Activity Plugin Documentation](https://github.com/jamesmontemagno/CurrentActivityPlugin/blob/master/README.md)

#### Permissions:
The `ACCESS_COARSE_LOCATION` and `ACCESS_FINE_LOCATION` permissions are required and are automatically added to your Android Manifest when you compile. No need to add them manually!

By adding these permissions [Google Play will automatically filter out devices](http://developer.android.com/guide/topics/manifest/uses-feature-element.html#permissions-features) without specific hardware. You can get around this by adding the following to your AssemblyInfo.cs file in your Android project:

```csharp
[assembly: UsesFeature("android.hardware.location", Required = false)]
[assembly: UsesFeature("android.hardware.location.gps", Required = false)]
[assembly: UsesFeature("android.hardware.location.network", Required = false)]
```

This plugin leverages the [Permission Plugin](http://github.com/jamesmontemagno/permissionsplugin), which means you must add the following code to your BaseActivity or MainActivity in Xamarin.Forms:

Add usings:
```csharp
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
```

Add in Activity:
```csharp
public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
{
    PermissionsImplementation.Current.OnRequestPermissionsResult(requestCode, permissions, grantResults);
}
```

### iOS/tvOS/macOS
Your app is required to have keys in your Info.plist for `NSLocationWhenInUseUsageDescription` or `NSLocationAlwaysUsageDescription` in order to access the device's location. You can read more here: https://blog.xamarin.com/new-ios-10-privacy-permission-settings/

Such as:
```xml
<key>NSLocationWhenInUseUsageDescription</key>
<string>This app needs access location when open.</string>
```
or
```xml
<key>NSLocationAlwaysUsageDescription</key>
<string>This app needs access always to location.</string>
```

**iOS 11 Introduces important changes when using Always Usage**

You are required to include the NSLocationWhenInUseUsageDescription and NSLocationAlwaysAndWhenInUseUsageDescription keys in your app's Info.plist file. (If your app supports iOS 10 and earlier, the NSLocationAlwaysUsageDescription key is also required.) If those keys are not present, authorization requests fail immediately.

```xml
<key>NSLocationAlwaysAndWhenInUseUsageDescription</key>
<string>This app needs access location when open and in the background.</string>
```

If you are targeting devices running older than iOS 8 you must add **NSLocationUsageDescription**.

If you want the dialogs to be translated you must support the specific languages in your app. Read the [iOS Localization Guide](https://developer.xamarin.com/guides/ios/advanced_topics/localization_and_internationalization/)

If you need location updates in the background be sure to read the [Background Updates](BackgroundUpdates.md) section for additional setup.



Please see the [Apple Documentation](https://devstreaming-cdn.apple.com/videos/wwdc/2017/713tkef4yl0sv3k/713/713_whats_new_in_location_technologies.pdf)

### UWP
You must set the `ID_CAP_LOCATION` permission.





<= Back to [Table of Contents](README.md)
