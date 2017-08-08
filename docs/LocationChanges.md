## Location Changes
In addition to a one shot location query you can easily register for location changes. By default these are for foreground changes when your application is open. For background or when app is closed be srue to read through the [Background Updates](BackgroundUpdates.md) documentation.


### Start Listening
Before subscribing to events you must start listening, which will start the managers to query for location changes. 

To check to see if you are listning there is a nifty `IsListening` property on the `CrossGeolocator.Current` that you can use.

```csharp
/// <summary>
/// Gets if you are listening for location changes
/// </summary>
bool IsListening { get; }
```

Once you are ready to start listening for changes you can call the `StartListeningAsync`. After this, you can add event handlers to get the changes.
```csharp
  /// <summary>
  /// Start listening for changes
  /// </summary>
  /// <param name="minimumTime">Minimum time between updates</param>
  /// <param name="minimumDistance">Distance distance in meters between updates</param>
  /// <param name="includeHeading">Include heading or not</param>
  /// <param name="listenerSettings">Optional settings (iOS only)</param>
  Task<bool> StartListeningAsync(TimeSpan minimumTime, double minimumDistance, bool includeHeading = false, ListenerSettings listenerSettings = null);
```

`ListenerSettings` are details more in the [Background Updates](BackgroundUpdates.md) documentation.

UWP Note: How the Geolocator works you must either set the `minTime` or the `minDistance`. Setting both means that `minDistance` will take precedence between the two. You can read more on the [Windows blog](https://blogs.windows.com/buildingapps/2012/12/03/geoposition-advanced-tracking-scenarios-for-windows-phone-8/#81dhJ7lK83WcPgT2.97).

### Position Changed Event

```csharp
/// <summary>
/// Position changed event handler
/// </summary>
event EventHandler<PositionEventArgs> PositionChanged;
```
These event args have one property of `Position` that is the new position that has been detected.


### Position Error Event
If an error occures you will be notified by this event. It is best practice to stop listening and start listening again after handling the error.

```csharp
/// <summary>
/// Position error event handler
/// </summary>
event EventHandler<PositionErrorEventArgs> PositionError;
```

### Stop Listening
When you are all done you can stop listening for changes.

```csharp
/// <summary>
/// Stop listening
/// </summary>
/// <returns>If successfully stopped</returns>
Task<bool> StopListeningAsync();
```

Example:
```csharp
async Task StartListening()
{
	if(CrossGeolocator.Current.IsListening)
		return;
	
  await CrossGeolocator.Current.StartListeningAsync(TimeSpan.FromSeconds(5), 10, true);

  CrossGeolocator.Current.PositionChanged += PositionChanged;
  CrossGeolocator.Current.PositionError += PositionError;
}

private void PositionChanged(object sender, PositionEventArgs e)
{
  
  //If updating the UI, ensure you invoke on main thread
  var position = e.Position;
  var output = "Full: Lat: " + position.Latitude + " Long: " + position.Longitude;
  output += "\n" + $"Time: {position.Timestamp}";
  output += "\n" + $"Heading: {position.Heading}";
  output += "\n" + $"Speed: {position.Speed}";
  output += "\n" + $"Accuracy: {position.Accuracy}";
  output += "\n" + $"Altitude: {position.Altitude}";
  output += "\n" + $"Altitude Accuracy: {position.AltitudeAccuracy}";
  Debug.WriteLine(output);
} 

private void PositionError(object sender, PositionErrorEventArgs e)
{
  Debug.WriteLine(e.Error);
  //Handle event here for errors
} 

async Task StopListening()
{
	if(!CrossGeolocator.Current.IsListening)
		return;
	
  await CrossGeolocator.Current.StopListening);

  CrossGeolocator.Current.PositionChanged -= PositionChanged;
  CrossGeolocator.Current.PositionError -= PositionError;
}
```



<= Back to [Table of Contents](README.md)