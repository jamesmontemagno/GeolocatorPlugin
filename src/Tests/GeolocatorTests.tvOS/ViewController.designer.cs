// WARNING
//
// This file has been generated automatically by Xamarin Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using Foundation;
using System;
using System.CodeDom.Compiler;

namespace GeolocatorTests.tvOS
{
    [Register ("ViewController")]
    partial class ViewController
    {
        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton ButtonAddress { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton ButtonAvailable { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton ButtonEnabled { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton ButtonGetLocation { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel LabelAddress { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel LabelAvailable { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel LabelEnabled { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel LabelLocation { get; set; }

        void ReleaseDesignerOutlets ()
        {
            if (ButtonAddress != null) {
                ButtonAddress.Dispose ();
                ButtonAddress = null;
            }

            if (ButtonAvailable != null) {
                ButtonAvailable.Dispose ();
                ButtonAvailable = null;
            }

            if (ButtonEnabled != null) {
                ButtonEnabled.Dispose ();
                ButtonEnabled = null;
            }

            if (ButtonGetLocation != null) {
                ButtonGetLocation.Dispose ();
                ButtonGetLocation = null;
            }

            if (LabelAddress != null) {
                LabelAddress.Dispose ();
                LabelAddress = null;
            }

            if (LabelAvailable != null) {
                LabelAvailable.Dispose ();
                LabelAvailable = null;
            }

            if (LabelEnabled != null) {
                LabelEnabled.Dispose ();
                LabelEnabled = null;
            }

            if (LabelLocation != null) {
                LabelLocation.Dispose ();
                LabelLocation = null;
            }
        }
    }
}