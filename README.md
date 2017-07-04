## Geolocator Plugin for Xamarin and Windows

Simple cross platform plugin to get GPS location including heading, speed, and more. Additionally, you can track geolocation changes :)

### Setup
* Available on NuGet: http://www.nuget.org/packages/Xam.Plugin.Geolocator [![NuGet](https://img.shields.io/nuget/v/Xam.Plugin.Geolocator.svg?label=NuGet)](https://www.nuget.org/packages/Xam.Plugin.Geolocator/)
* Install into your PCL project and Client projects.

Build Status: [![Build status](https://ci.appveyor.com/api/projects/status/nan2cxlgeo11sc5u?svg=true)](https://ci.appveyor.com/project/JamesMontemagno/geolocatorplugin)

**Platform Support**

Version 4.X

|Platform|Version|
| ------------------- |  :------------------: |
|Xamarin.iOS|iOS 7+|
|Xamarin.Android|API 14+|
|Windows 10 UWP|10+|
|macOS|All|
|tvOS|10+|


### API Usage
Call **CrossGeolocator.Current** from any project to gain access to APIs.

#### Get Position

```csharp

try
{
	var locator = CrossGeolocator.Current;
	locator.DesiredAccuracy = 50;

	var position = await locator.GetPositionAsync (TimeSpan.FromSeconds(10));
	if(position == null)
	return;

	Console.WriteLine ("Position Status: {0}", position.Timestamp);
	Console.WriteLine ("Position Latitude: {0}", position.Latitude);
	Console.WriteLine ("Position Longitude: {0}", position.Longitude);
}
catch(Exception ex)
{
  Debug.WriteLine("Unable to get location, may need to increase timeout: " + ex);
}
```

In addition to taking in a timespan ```GetPositionAsync``` also takes in a cancelation token.

#### Get Cached Location
On iOS, Android, tvOS, and macOS you can auery the last known position really fast by getting the cached position of the system.
**On UWP ths will always return null**, as this feature is not supported.

```csharp
var cached = await CrossGeolocator.Current.GetLastKnownLocationAsync();
if(cached == null)
  return;


Console.WriteLine ("Position Status: {0}", cached.Timestamp);
Console.WriteLine ("Position Latitude: {0}", cached.Latitude);
Console.WriteLine ("Position Longitude: {0}", cached.Longitude);
```

#### Listening for Location Changes

* iOS has special capabilities that allows certain types of apps to get location updates when in the background, but you must specify this.
* On Android you should use a background service and bind to the UI
* Windows you should also use background services


#### Checking permissions
Before tracking begine you must check to ensure that you have location permissions on all platforms. I recommend using the [Permissions Plugin](http://github.com/jamesmontemagno/PermissionPlugin) to ensure that you have permission.


#### Start Tracking
```csharp
/// <summary>
/// Start listening for changes
/// </summary>
/// <param name="minimumTime">Time</param>
/// <param name="minimumDistance">Distance</param>
/// <param name="includeHeading">Include heading or not</param>
/// <param name="listenerSettings">Optional settings (iOS only)</param>
Task<bool> StartListeningAsync(TimeSpan minimumTime, double minimumDistance, bool includeHeading = false, ListenerSettings listenerSettings = null)
```

**UWP Note:** How the Geolocator works you must either set the minTime or the minDistance. Setting both means that minDistance will take precedence between the two. You can read more on the [Windows blog](https://blogs.windows.com/buildingapps/2012/12/03/geoposition-advanced-tracking-scenarios-for-windows-phone-8/#81dhJ7lK83WcPgT2.97). 

#### Example
```csharp

async Task StartListening()
{
	if(CrossGeolocator.Current.IsListening)
		return;
	
	///This logic will run on the background automatically on iOS, however for Android and UWP you must put logic in background services. Else if your app is killed the location updates will be killed.
    await CrossGeolocator.Current.StartListeningAsync(TimeSpan.FromSeconds(5), 10, true, new Plugin.Geolocator.Abstractions.ListenerSettings
                {
                    ActivityType = Plugin.Geolocator.Abstractions.ActivityType.AutomotiveNavigation,
                    AllowBackgroundUpdates = true,
                    DeferLocationUpdates = true,
                    DeferralDistanceMeters = 1,
                    DeferralTime = TimeSpan.FromSeconds(1),
                    ListenForSignificantChanges = true,
                    PauseLocationUpdatesAutomatically = false
                });

    CrossGeolocator.Current.PositionChanged += Current_PositionChanged;
}

private void Current_PositionChanged(object sender, Plugin.Geolocator.Abstractions.PositionEventArgs e)
{
    Device.BeginInvokeOnMainThread(() =>
    {
        var test = e.Position;
        listenLabel.Text = "Full: Lat: " + test.Latitude.ToString() + " Long: " + test.Longitude.ToString();
        listenLabel.Text += "\n" + $"Time: {test.Timestamp.ToString()}";
        listenLabel.Text += "\n" + $"Heading: {test.Heading.ToString()}";
        listenLabel.Text += "\n" + $"Speed: {test.Speed.ToString()}";
        listenLabel.Text += "\n" + $"Accuracy: {test.Accuracy.ToString()}";
        listenLabel.Text += "\n" + $"Altitude: {test.Altitude.ToString()}";
        listenLabel.Text += "\n" + $"AltitudeAccuracy: {test.AltitudeAccuracy.ToString()}";
    });
}           
```


#### Reverse Geocoding
Based on a location that is passed in, thi swill grab a list of addresses.

**UWP** requires a Bing Map Key, which you can aquire by reading [this peice of documentation](https://docs.microsoft.com/en-us/windows/uwp/maps-and-location/authentication-key).

```csharp
try
{ 
  var addresses = await locator.GetAddressesForPositionAsync (position, string mapKey = null);
  var address = addresses.FirstOrDefault();
  
  if(address == null)
    Console.WriteLine ("No address found for position.");
  else
    Console.WriteLine ("Addresss: {0} {1} {2}", address.Thoroughfare, address.Locality, address.Country);
}
catch(Exception ex)
{
  Debug.WriteLine("Unable to get address: " + ex);
}
```


### **IMPORTANT**
#### Android:
The ACCESS_COARSE_LOCATION & ACCESS_FINE_LOCATION permissions are required, but the library will automatically add this for you. Additionally, if your users are running Marshmallow the Plugin will automatically prompt them for runtime permissions.

By adding these permissions [Google Play will automatically filter out devices](http://developer.android.com/guide/topics/manifest/uses-feature-element.html#permissions-features) without specific hardward. You can get around this by adding the following to your AssemblyInfo.cs file in your Android project:

```
[assembly: UsesFeature("android.hardware.location", Required = false)]
[assembly: UsesFeature("android.hardware.location.gps", Required = false)]
[assembly: UsesFeature("android.hardware.location.network", Required = false)]
```

### Android specific in your BaseActivity or MainActivity (for Xamarin.Forms) add this code:
```csharp
public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
{
    PermissionsImplementation.Current.OnRequestPermissionsResult(requestCode, permissions, grantResults);
}
```

**You MUST set your Target version to API 25+ and Compile against API 25+**

#### iOS:
In iOS 8 you now have to call either RequestWhenInUseAuthorization or RequestAlwaysAuthorization on the location manager (the plugin does this automatically for you, however, need to add either the concisely named NSLocationWhenInUseUsageDescription or NSLocationAlwaysUsageDescription to your Info.plist. 

You will need to add a new string entry called NSLocationWhenInUseUsageDescription or NSLocationAlwaysUsageDescription. 

Go to your info.plist and under source add one of these flags: http://screencast.com/t/YEeuAYMBBJ

For more information:  http://motzcod.es/post/97662738237/scanning-for-ibeacons-in-ios-8

**iOS 9 Simulator**
Getting location via the simulator doesn't seem to be supported, you will need to test on a device.

**iOS 9 Special Case: Background Updates (for background agents, not background tasks):**

New in iOS 9 allowsBackgroundLocationUpdates must be set if you are running a background agent to track location. I have exposed this on the Geolocator via a setting that is passed in when you start tracking:

The presence of the UIBackgroundModes key with the location value is required for background updates; you use this property to enable and disable the behavior based on your app’s behavior.

#### UWP:

You must set the ID_CAP_LOCATION permission.


#### License
Licensed under MIT, see license file.

This is a derivative to [Xamarin.Mobile's Geolocator](http://github.com/xamarin/xamarin.mobile) with a cross platform API and other enhancements.
﻿//
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
