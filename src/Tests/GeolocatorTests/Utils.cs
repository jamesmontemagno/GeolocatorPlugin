using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace GeolocatorTests
{
	public static class Utils
	{
		public static async Task<bool> CheckPermissions<T>() where T : BasePermission, new()
		{
			var permissionStatus = await CrossPermissions.Current.CheckPermissionStatusAsync<T>();
			bool request = false;
			if (permissionStatus == PermissionStatus.Denied)
			{
				if (Device.RuntimePlatform == Device.iOS)
				{

					var title = $"{nameof(T)} Permission";
					var question = $"To use this plugin the {nameof(T)} permission is required. Please go into Settings and turn on {nameof(T)} for the app.";
					var positive = "Settings";
					var negative = "Maybe Later";
					var task = Application.Current?.MainPage?.DisplayAlert(title, question, positive, negative);
					if (task == null)
						return false;

					var result = await task;
					if (result)
					{
						CrossPermissions.Current.OpenAppSettings();
					}

					return false;
				}

				request = true;

			}

			if (request || permissionStatus != PermissionStatus.Granted)
			{
				var newStatus = await CrossPermissions.Current.RequestPermissionAsync<T>();
				if (newStatus != PermissionStatus.Granted)
				{
					var title = $"{nameof(T)} Permission";
					var question = $"To use the plugin the {nameof(T)} permission is required.";
					var positive = "Settings";
					var negative = "Maybe Later";
					var task = Application.Current?.MainPage?.DisplayAlert(title, question, positive, negative);
					if (task == null)
						return false;

					var result = await task;
					if (result)
					{
						CrossPermissions.Current.OpenAppSettings();
					}
					return false;
				}
			}

			return true;
		}
	}
}

