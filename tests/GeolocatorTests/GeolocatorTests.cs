using System;
using System.Linq;
using Xamarin.Forms;
using Plugin.Geolocator;

namespace GeolocatorTests
{
    public class App : Application
    {
        Label listenLabel;
        public App()
        {

            var label = new Label
            {
                Text = "Click Get Location"
            };

            var addressLabel = new Label
            {
                Text = "Click Get Address"
            };

            listenLabel = new Label
            {
                Text = "Click to listen"
            };

            var button = new Button
            {
                Text = "Get Location"
            };

            var addressBtn = new Button
            {
                Text = "Get Address"
            };

            var listenToggle = new Button
            {
                Text = "Listen"
            };


            var buttonIsAvailable = new Button
            {
                Text = "IsAvailable"
            };

            var buttonIsEnabled = new Button
            {
                Text = "IsEnabled"
            };

            buttonIsAvailable.Clicked += (sender, args) =>
            {
                buttonIsAvailable.Text = CrossGeolocator.Current.IsGeolocationAvailable ? "Available" : "Not Available";
            };

            buttonIsEnabled.Clicked += (sender, args) =>
            {
                buttonIsEnabled.Text = CrossGeolocator.Current.IsGeolocationEnabled ? "Enabled" : "Not Enabled";
            };

            button.Clicked += async (sender, e) =>
            {
                try
                {
                    button.IsEnabled = false;
                    label.Text = "Getting...";

                    var cached = await CrossGeolocator.Current.GetLastKnownLocationAsync();
                    if (cached == null)
                        label.Text += "No cached";
                    else
                        label.Text += "\n" + "Cached: Lat: " + cached.Latitude.ToString() + " Long: " + cached.Longitude.ToString();

                    var test = await CrossGeolocator.Current.GetPositionAsync(TimeSpan.FromMinutes(2));
                    label.Text += "\n" + "Full: Lat: " + test.Latitude.ToString() + " Long: " + test.Longitude.ToString();
                    label.Text += "\n" + $"Time: {test.Timestamp.ToString()}";
                    label.Text += "\n" + $"Heading: {test.Heading.ToString()}";
                    label.Text += "\n" + $"Speed: {test.Speed.ToString()}";
                    label.Text += "\n" + $"Accuracy: {test.Accuracy.ToString()}";
                    label.Text += "\n" + $"Altitude: {test.Altitude.ToString()}";
                    label.Text += "\n" + $"AltitudeAccuracy: {test.AltitudeAccuracy.ToString()}";
                }
                catch (Exception ex)
                {
                    label.Text = ex.Message;
                }
                finally
                {
                    button.IsEnabled = true;
                }
            };

            addressBtn.Clicked += async (sender, e) =>
            {
                try
                {
                    addressBtn.IsEnabled = false;
                    label.Text = "Getting address...";
                    var position = await CrossGeolocator.Current.GetPositionAsync(TimeSpan.FromMinutes(2));
                    var addresses = await CrossGeolocator.Current.GetAddressesForPositionAsync(position, "RJHqIE53Onrqons5CNOx~FrDr3XhjDTyEXEjng-CRoA~Aj69MhNManYUKxo6QcwZ0wmXBtyva0zwuHB04rFYAPf7qqGJ5cHb03RCDw1jIW8l");

					var address = addresses.FirstOrDefault();

                    if (address == null)
                    {
                        addressLabel.Text = "No address found for position.";
                    }
                    else
                    {
                        addressLabel.Text = $"Address: {address.Thoroughfare} {address.Locality}";
                    }
                }
                catch (Exception ex)
                {
                    label.Text = ex.Message;
                }
                finally
                {
                    addressBtn.IsEnabled = true;
                }
            };


            listenToggle.Clicked += async (sender, args) =>
            {
                if(CrossGeolocator.Current.IsListening)
                {
                    listenToggle.Text = "Stopped Listening";
                    await CrossGeolocator.Current.StopListeningAsync();

                    CrossGeolocator.Current.PositionChanged -= Current_PositionChanged;
                    return;
                }

                listenToggle.Text = "Listening";
                await CrossGeolocator.Current.StartListeningAsync(TimeSpan.FromSeconds(5), 1, true, new Plugin.Geolocator.Abstractions.ListenerSettings
                {
                    ActivityType = Plugin.Geolocator.Abstractions.ActivityType.AutomotiveNavigation,
                    AllowBackgroundUpdates = false,
                    DeferLocationUpdates = false,
                    DeferralDistanceMeters = 1,
                    DeferralTime = TimeSpan.FromSeconds(1),
                    ListenForSignificantChanges = false,
                    PauseLocationUpdatesAutomatically = false
                });

                CrossGeolocator.Current.PositionChanged += Current_PositionChanged;
            };

            // The root page of your application
            MainPage = new ContentPage
            {
                Content = new StackLayout
                {
                    VerticalOptions = LayoutOptions.Center,
                    Children = { buttonIsAvailable, buttonIsEnabled, label, button, addressBtn, addressLabel, listenToggle, listenLabel }
                }
            };
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

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}

