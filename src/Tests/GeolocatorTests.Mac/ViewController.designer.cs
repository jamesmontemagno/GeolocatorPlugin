// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace GeolocatorTests.Mac
{
	[Register ("ViewController")]
	partial class ViewController
	{
		[Outlet]
		AppKit.NSButton ButtonGetAddress { get; set; }

		[Outlet]
		AppKit.NSButton ButtonGetLocation { get; set; }

		[Outlet]
		AppKit.NSButton ButtonIsAvailable { get; set; }

		[Outlet]
		AppKit.NSButton ButtonIsEnabled { get; set; }

		[Outlet]
		AppKit.NSButton ButtonListen { get; set; }

		[Outlet]
		AppKit.NSTextField LabelIsAvailable { get; set; }

		[Outlet]
		AppKit.NSTextField LabelIsEnabled { get; set; }

		[Outlet]
		AppKit.NSTextField LabelListenPosition { get; set; }

		[Outlet]
		AppKit.NSTextField LabelPosition { get; set; }

		[Action ("ButtonAvailableClicked:")]
		partial void ButtonAvailableClicked (Foundation.NSObject sender);

		[Action ("ButtonEnabledClicked:")]
		partial void ButtonEnabledClicked (Foundation.NSObject sender);

		[Action ("ButtonGetAddressClicked:")]
		partial void ButtonGetAddressClicked (Foundation.NSObject sender);

		[Action ("ButtonGetLocationClicked:")]
		partial void ButtonGetLocationClicked (Foundation.NSObject sender);

		[Action ("ButtonListenClicked:")]
		partial void ButtonListenClicked (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (ButtonIsAvailable != null) {
				ButtonIsAvailable.Dispose ();
				ButtonIsAvailable = null;
			}

			if (ButtonIsEnabled != null) {
				ButtonIsEnabled.Dispose ();
				ButtonIsEnabled = null;
			}

			if (ButtonGetLocation != null) {
				ButtonGetLocation.Dispose ();
				ButtonGetLocation = null;
			}

			if (ButtonGetAddress != null) {
				ButtonGetAddress.Dispose ();
				ButtonGetAddress = null;
			}

			if (ButtonListen != null) {
				ButtonListen.Dispose ();
				ButtonListen = null;
			}

			if (LabelIsAvailable != null) {
				LabelIsAvailable.Dispose ();
				LabelIsAvailable = null;
			}

			if (LabelIsEnabled != null) {
				LabelIsEnabled.Dispose ();
				LabelIsEnabled = null;
			}

			if (LabelPosition != null) {
				LabelPosition.Dispose ();
				LabelPosition = null;
			}

			if (LabelListenPosition != null) {
				LabelListenPosition.Dispose ();
				LabelListenPosition = null;
			}
		}
	}
}
