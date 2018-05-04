Geolocator Readme

For latest changes: https://github.com/jamesmontemagno/GeolocatorPlugin/blob/master/CHANGELOG.md

## EXTREMELY IMPORTANT SETUP
Please follow the guide to properly setup the Geolocator inside of your application: 

https://jamesmontemagno.github.io/GeolocatorPlugin/GettingStarted.html

Additionally, see the permission setup below for Android to ensure everything is configured correct.

## Additional Android Permission Setup

This plugin uses the [Current Activity Plugin](https://github.com/jamesmontemagno/CurrentActivityPlugin/blob/master/README.md) to get access to the current Android Activity. Be sure to complete the full setup if a MainApplication.cs file was not automatically added to your application. Please fully read through the [Current Activity Plugin Documentation](https://github.com/jamesmontemagno/CurrentActivityPlugin/blob/master/README.md). At an absolute minimum you must set the following in your Activity's OnCreate method:

```csharp
CrossCurrentActivity.Current.Activity.Init(this, bundle);
```

It is highly recommended that you use a custom Application that are outlined in the Current Activity Plugin Documentation](https://github.com/jamesmontemagno/CurrentActivityPlugin/blob/master/README.md)

## Android specific in your BaseActivity or MainActivity (for Xamarin.Forms) add this code:

Add usings:

using Plugin.Permissions;

Then add to Activity:

public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Android.Content.PM.Permission[] grantResults)
{
    Plugin.Permissions.PermissionsImplementation.Current.OnRequestPermissionsResult(requestCode, permissions, grantResults);
	base.OnRequestPermissionsResult(requestCode, permissions, grantResults)
}

