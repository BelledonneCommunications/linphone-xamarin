using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

using Linphone;
using System.Threading;

namespace LinphoneXamarin
{
	[Activity (Label = "LinphoneXamarin", Icon = "@drawable/icon", Theme="@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
	public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
	{
        private LinphoneXamarin.App app;

        private void LinphoneCoreIterate()
        {
            while (true)
            {
                RunOnUiThread(() => app.Core.Iterate());
                System.Threading.Thread.Sleep(50);
            }
        }

        protected override void OnCreate (Bundle bundle)
		{
            Java.Lang.JavaSystem.LoadLibrary("bctoolbox");
            Java.Lang.JavaSystem.LoadLibrary("ortp");
            Java.Lang.JavaSystem.LoadLibrary("mediastreamer_base");
            Java.Lang.JavaSystem.LoadLibrary("mediastreamer_voip");
            Java.Lang.JavaSystem.LoadLibrary("linphone");

            // This is mandatory for Android
            LinphoneAndroid.setAndroidContext(JNIEnv.Handle, this.Handle);
            LinphoneAndroid.setNativeLogHandler();

            TabLayoutResource = Resource.Layout.Tabbar;
			ToolbarResource = Resource.Layout.Toolbar; 

			base.OnCreate (bundle);

			global::Xamarin.Forms.Forms.Init (this, bundle);
            app = new LinphoneXamarin.App();
            LoadApplication (app);

            Thread coreIterate = new Thread(LinphoneCoreIterate);
            coreIterate.IsBackground = false;
            coreIterate.Start();
        }
	}
}

