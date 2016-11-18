using System;
using System.Runtime.InteropServices;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

namespace LinphoneXamarin.Droid
{
	[Activity (Label = "LinphoneXamarin", Icon = "@drawable/icon", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
	public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsApplicationActivity
    {
        [DllImport("liblinphone-armeabi-v7a")]
        static extern void ms_set_jvm_from_env(IntPtr jnienv);

        protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			global::Xamarin.Forms.Forms.Init (this, bundle);

            Java.Lang.JavaSystem.LoadLibrary("bctoolbox-armeabi-v7a");
            Java.Lang.JavaSystem.LoadLibrary("ortp-armeabi-v7a");
            Java.Lang.JavaSystem.LoadLibrary("mediastreamer_base-armeabi-v7a");
            Java.Lang.JavaSystem.LoadLibrary("mediastreamer_voip-armeabi-v7a");
            Java.Lang.JavaSystem.LoadLibrary("linphone-armeabi-v7a");

            ms_set_jvm_from_env(Android.Runtime.JNIEnv.Handle);

            LoadApplication (new LinphoneXamarin.App ());
        }
	}
}

