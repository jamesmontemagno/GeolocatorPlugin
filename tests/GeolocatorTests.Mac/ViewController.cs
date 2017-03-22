using System;
using System.Linq;
using AppKit;
using Foundation;
using Plugin.Geolocator;

namespace GeolocatorTests.Mac
{
    public partial class ViewController : NSViewController
    {
        public ViewController(IntPtr handle) : base(handle)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();



            // Do any additional setup after loading the view.
        }

        partial void ButtonAvailableClicked(NSObject sender)
        {
            LabelIsAvailable.StringValue = CrossGeolocator.Current.IsGeolocationAvailable ? "Available" : "Not Available";
        }

        partial void ButtonEnabledClicked(NSObject sender)
        {
            LabelIsEnabled.StringValue = CrossGeolocator.Current.IsGeolocationEnabled ? "Enabled" : "Not Enabled";

        }

        async partial void ButtonGetAddressClicked(NSObject sender)
        {
            try
            {
                var position = await CrossGeolocator.Current.GetPositionAsync(TimeSpan.FromSeconds(15));
                var address = await CrossGeolocator.Current.GetAddressesForPositionAsync(position);

                var first = address.FirstOrDefault();
                LabelPosition.StringValue = $"Address: {first.Thoroughfare} {first.Locality}";
            }
            catch (Exception ex)
            {
                LabelPosition.StringValue = "Error";
            }
        }

        async partial void ButtonGetLocationClicked(NSObject sender)
        {
            var test = await CrossGeolocator.Current.GetPositionAsync(TimeSpan.FromMinutes(2));
            LabelPosition.StringValue =  "Full: Lat: " + (test.Latitude).ToString() + " Long: " + (test.Longitude).ToString();
            LabelPosition.StringValue += "\n" + $"Time: {test.Timestamp.ToString()}";
            LabelPosition.StringValue += "\n" + $"Heading: {test.Heading.ToString()}";
            LabelPosition.StringValue += "\n" + $"Speed: {test.Speed.ToString()}";
            LabelPosition.StringValue += "\n" + $"Accuracy: {test.Accuracy.ToString()}";
            LabelPosition.StringValue += "\n" + $"Altitude: {test.Altitude.ToString()}";
            LabelPosition.StringValue += "\n" + $"AltitudeAccuracy: {test.AltitudeAccuracy.ToString()}";
        }

        partial void ButtonListenClicked(NSObject sender)
        {

        }

        public override NSObject RepresentedObject
        {
            get
            {
                return base.RepresentedObject;
            }
            set
            {
                base.RepresentedObject = value;
                // Update the view, if already loaded.
            }
        }
    }
}
