using Android.App;

[assembly: UsesPermission(Android.Manifest.Permission.AccessCoarseLocation)]

//Required when targeting Android API 21+
[assembly: UsesFeature("android.hardware.location.gps")]
[assembly: UsesFeature("android.hardware.location.network")]
