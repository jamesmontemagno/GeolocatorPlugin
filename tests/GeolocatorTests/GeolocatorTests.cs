using System;

using Xamarin.Forms;
using Plugin.Geolocator;

namespace GeolocatorTests
{
    public class App : Application
    {
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

            var button = new Button
            {
                Text = "Get Location"
            };

            var addressBtn = new Button
            {
                Text = "Get Address"
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
                    var test = await CrossGeolocator.Current.ReverseGeocodeCurrentLocation();
                    label.Text += "\n" + "Full: Lat: " + test.Latitude.ToString() + " Long: " + test.Longitude.ToString();
                    addressLabel.Text = $"Address: {test.Thoroughfare} {test.Locality}";
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

            // The root page of your application
            MainPage = new ContentPage
            {
                Content = new StackLayout
                {
                    VerticalOptions = LayoutOptions.Center,
                    Children = { label, button, addressBtn, addressLabel }
                }
            };
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

