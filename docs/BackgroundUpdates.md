## Background Updates
Background updates are handled a bit different on each platform. The [My Driving application](https://github.com/azure-samples/mydriving) is a great sample showing how to handle this on all mobile platforms.

### iOS
We are lucky here as iOS has background tracking built into the API. iOS is a little bit different as when the user force closes your app everything is gone as there are no background services. This is where the `ListenerSettings` come into play when executing the `StartListeningAsync` method.

For background notifications you must set background updates in your info.plist under `UIBackgroundModes`.

In addition to allowing background updates there are a plethora of other options:

```csharp
/// <summary>
/// Settings for location listening (only applies to iOS). All defaults are set as indicated in the docs for CLLocationManager.
/// </summary>
public class ListenerSettings
{
  /// <summary>
  /// Gets or sets whether background location updates should be allowed (>= iOS 9). Default:  false
  /// </summary>
  public bool AllowBackgroundUpdates { get; set; } = false;

  /// <summary>
  /// Gets or sets whether location updates should be paused automatically when the location is unlikely to change (>= iOS 6). Default:  true
  /// </summary>
  public bool PauseLocationUpdatesAutomatically { get; set; } = true;

  /// <summary>
  /// Gets or sets the activity type that should be used to determine when to automatically pause location updates (>= iOS 6). Default:  ActivityType.Other
  /// </summary>
  public ActivityType ActivityType { get; set; } = ActivityType.Other;

  /// <summary>
  /// Gets or sets whether the location manager should only listen for significant changes in location, rather than continuous listening (>= iOS 4). Default:  false
  /// </summary>
  public bool ListenForSignificantChanges { get; set; } = false;

  /// <summary>
  /// Gets or sets whether the location manager should defer location updates until an energy efficient time arrives, or distance and time criteria are met (>= iOS 6). Default:  false
  /// </summary>
  public bool DeferLocationUpdates { get; set; } = false;

  /// <summary>
  /// If deferring location updates, the minimum distance to travel before updates are delivered (>= iOS 6). Set to null for indefinite wait. Default:  500
  /// </summary>
  public double? DeferralDistanceMeters { get; set; } = 500;

  /// <summary>
  /// If deferring location updates, the minimum time that should elapse before updates are delivered (>= iOS 6). Set to null for indefinite wait. Default:  5 minutes
  /// </summary>
  /// <value>The time between updates (default:  5 minutes).</value>
  public TimeSpan? DeferralTime { get; set; } = TimeSpan.FromMinutes(5);
}
```

Example:
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
```

### Android
For this you will want to integrate a foreground service that subscribes to location changes and the user interface binds to. Please read through the [Xamarin.Android Services documentation](https://developer.xamarin.com/guides/android/application_fundamentals/services/)

### UWP
There are several different approaches you can take here, but it is recommended to implement [Extended Execution](https://docs.microsoft.com/en-us/windows/uwp/launch-resume/run-minimized-with-extended-execution) for background tracking.


<= Back to [Table of Contents](README.md)