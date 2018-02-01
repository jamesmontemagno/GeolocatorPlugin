
## FAQ and Common Issues

Here is a quick guide to questions asked.

### I am receiving a "Task was cancelled issue" exception
This is completely possible if the device doesn't return the location in the specified time. It is good to check the cached location first before polling. If you are on an emulator it is very common to get this. You must bring up the emulators settings and push a location to a device. The same can be true for the iOS Simulator.

### Does the Android power setting effect geolocation?
Yes, according to the [documentation](https://developer.android.com/about/versions/oreo/background-location-limits.html) If your app is running in the background, the location system service computes a new location for your app only a few times each hour. This is the case even when your app is requesting more frequent location updates.

### IsGeolocationAvailable and IsGeolocationEnabled returns false on Android when no permission is granted
This is the current expected behavior, it is the only way to actually query the providers for gps. I recommend that the app developer checks the permissions first using the [Permissions Plugin](https://github.com/jamesmontemagno/PermissionsPlugin).


<= Back to [Table of Contents](README.md)