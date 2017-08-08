## Checking Current Location
There are a few properties that can be used to easily check connection information using the plugin.

### Location Properties
There are several properties on `CrossGeolocator.Current` that can help when getting geolocation and ensuring the hardware has support for geolocation.

```csharp
/// <summary>
/// Desired accuracy in meters
/// </summary>
double DesiredAccuracy { get; set; }
```
This property tells the location managers that it is okay to be X meters off. The default is 100 meters.

```csharp
/// <summary>
/// Gets if device supports heading
/// </summary>
bool SupportsHeading { get; }
```
Determines if the device and OS supports returning the heading of the location.

```csharp
/// <summary>
/// Gets if geolocation is available on device
/// </summary>
bool IsGeolocationAvailable { get; }
```
Determines if geolocation is actually available and capable of getting geolocation.

```csharp
/// <summary>
/// Gets if geolocation is enabled on device
/// </summary>
bool IsGeolocationEnabled { get; }
```
If the geolocation mechanisms of the device are actually enabled.

### Cached/Last Known Location
Before quering for a full location which will boot up sensors for geolocation you can query for the last known or cached location of the manager. It will return `null` if no cached location is available.

```csharp
/// <summary>
/// Gets the last known and most accurate location.
/// This is usually cached and best to display first before querying for full position.
/// </summary>
/// <returns>Best and most recent location or null if none found</returns>
Task<Position> GetLastKnownLocationAsync();
```

### Query Current Location
Requests a query of the current location. This will start the location sensors on the device to attempt to get the current location. It is very possible that the request take much longer or not available, so exception handling should be considered.

```csharp
/// <summary>
/// Gets position async with specified parameters
/// </summary>
/// <param name="timeout">Timeout to wait, Default Infinite</param>
/// <param name="token">Cancellation token</param>
/// <param name="includeHeading">If you would like to include heading</param>
/// <returns>Position</returns>
Task<Position> GetPositionAsync(TimeSpan? timeout = null, CancellationToken? token = null, bool includeHeading = false);
```

Full Example:
```csharp
public async Task<Position> GetCurrentLocation()
{
  Position position = null;
  try
  {
    var locator = CrossGeolocator.Current;
    locator.DesiredAccuracy = 100;

    position = await locator.GetLastKnownLocationAsync();

    if (position != null)
    {
      //got a cahched position, so let's use it.
      return;
    }

    if(!locator.IsGeolocationAvailable || !locator.IsGeolocationEnabled)
    {
      //not available or enabled
      return;
    }

    position = await locator.GetPositionAsync(TimeSpan.FromSeconds(20), null, true);

  }
  catch (Exception ex)
  {
    //Display error as we have timed out or can't get location.
  }

  if(position == null)
    return;

  var output = string.Format("Time: {0} \nLat: {1} \nLong: {2} \nAltitude: {3} \nAltitude Accuracy: {4} \nAccuracy: {5} \nHeading: {6} \nSpeed: {7}",
      position.Timestamp, position.Latitude, position.Longitude,
      position.Altitude, position.AltitudeAccuracy, position.Accuracy, position.Heading, position.Speed);

  Debug.WriteLine(output);
}
```



<= Back to [Table of Contents](README.md)

