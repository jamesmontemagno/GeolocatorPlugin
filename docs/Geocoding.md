## Geocoding

The geolocator plugin also features common functions when dealing with locations such as Geocoding.

#### Reverse Geoloding
Based on a location that is passed in attempt to get a list of addresses.

```csharp
/// <summary>
/// Retrieve addresses for position.
/// </summary>
/// <param name="position">Desired position (latitude and longitude)</param>
/// <param name="mapKey">Map Key required only on UWP</param>
/// <returns>Addresses of the desired position</returns>
Task<IEnumerable<Address>> GetAddressesForPositionAsync(Position position, string mapKey = null);
```

Example:
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
### UWP Additional Setup
UWP requires a Bing Map Key, which you can aquire by reading this [piece of documentation](https://docs.microsoft.com/en-us/windows/uwp/maps-and-location/authentication-key) and then pass it in via the `mapKey` property.


<= Back to [Table of Contents](README.md)