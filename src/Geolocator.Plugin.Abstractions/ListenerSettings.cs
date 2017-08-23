using System;

namespace Plugin.Geolocator.Abstractions
{
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


		/// <summary>
		/// Gets or sets the fastest rate for active location updates. This interval is exact, and your
		/// application will never receive updates faster than this value.
		/// </summary>
		public TimeSpan? FastestInterval { get; set; }

		/// <summary>
		/// Gets or sets the maximum amount of time to wait between updates
		/// </summary>
		public TimeSpan? MaxWaitTime { get; set; }
		/// <summary>
		/// Set the priority of the request, a strong hit to the listener for which location sources to use. For example, high accuracy is 
		/// more likely to use GPS and balanced is more likely to use Wi-Fi and Cell positioning.
		/// </summary>
		public ListenerPriority Priority { get; set; } = ListenerPriority.HighAccuracy;


	}

	/// <summary>
	/// Priority options
	/// </summary>
	public enum ListenerPriority
	{
		/// <summary>
		/// REquest best accuracy possible with zero additional power consumption
		/// </summary>
		NoPower,
		/// <summary>
		/// Request city level accuracy. City level accuracy is considered to be about 10km accuracy. 
		/// </summary>
		LowPower,
		/// <summary>
		/// Request block level accuracy. Block level accuracy is considered to be about 100 meter accuracy. 
		/// </summary>
		BalancedPowerAccuracy,
		/// <summary>
		/// The ost accurate locations available
		/// </summary>
		HighAccuracy
	}
}