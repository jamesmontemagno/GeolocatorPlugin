using System;
using System.Linq;
using Foundation;
using Plugin.Geolocator;
using UIKit;

namespace GeolocatorTests.tvOS
{
    public partial class ViewController : UIViewController
    {
        public ViewController(IntPtr handle) : base(handle)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            ButtonAddress.AllEvents += async (sender, e) =>
            {
                try
                {
                    var position = await CrossGeolocator.Current.GetPositionAsync(TimeSpan.FromSeconds(15));
                    var address = await CrossGeolocator.Current.GetAddressesForPositionAsync(position);

                    var first = address.FirstOrDefault();
                    LabelAddress.Text = $"Address: {first.Thoroughfare} {first.Locality}";
                }
                catch (Exception ex)
                {
                    LabelAddress.Text = "Error";
                }
            };


            ButtonEnabled.AllEvents += async (sender, e) =>
            {
                LabelEnabled.Text = CrossGeolocator.Current.IsGeolocationEnabled ? "Enabled" : "Not Enabled";

            };

            ButtonAvailable.AllEvents += async (sender, e) =>
            {
                LabelAvailable.Text = CrossGeolocator.Current.IsGeolocationAvailable ? "Available" : "Not Available";

            };

            ButtonGetLocation.AllEvents += async (sender, e) =>
            {
                var test = await CrossGeolocator.Current.GetPositionAsync(TimeSpan.FromMinutes(2));
                LabelLocation.Text = "Full: Lat: " + (test.Latitude).ToString() + " Long: " + (test.Longitude).ToString();
                LabelLocation.Text += "\n" + $"Time: {test.Timestamp.ToString()}";
                LabelLocation.Text += "\n" + $"Heading: {test.Heading.ToString()}";
                LabelLocation.Text += "\n" + $"Speed: {test.Speed.ToString()}";
                LabelLocation.Text += "\n" + $"Accuracy: {test.Accuracy.ToString()}";
                LabelLocation.Text += "\n" + $"Altitude: {test.Altitude.ToString()}";
                LabelLocation.Text += "\n" + $"AltitudeAccuracy: {test.AltitudeAccuracy.ToString()}";

            };
            // Perform any additional setup after loading the view, typically from a nib.
        }

        public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();
            // Release any cached data, images, etc that aren't in use.
        }


    }
}


