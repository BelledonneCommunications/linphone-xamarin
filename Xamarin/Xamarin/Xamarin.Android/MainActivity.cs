using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using Android.Content.Res;
using System.IO;
using Linphone;
using Org.Linphone.Mediastream.Video;
using Xamarin.Forms.Platform.Android;
using Android;
using Android.Util;
using System.Collections.Generic;
using Xamarin.Forms;
using Android.Views;
using Android.Widget;
using System;

namespace Xamarin.Droid
{
    [Activity(Label = "Xamarin", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        int PERMISSIONS_REQUEST = 101;
        Org.Linphone.Mediastream.Video.Display.GL2JNIView displayCamera;
        SurfaceView captureCamera;

        protected override void OnCreate(Bundle bundle)
        {
            Java.Lang.JavaSystem.LoadLibrary("c++_shared");
            Java.Lang.JavaSystem.LoadLibrary("bctoolbox");
            Java.Lang.JavaSystem.LoadLibrary("ortp");
            Java.Lang.JavaSystem.LoadLibrary("mediastreamer_base");
            Java.Lang.JavaSystem.LoadLibrary("mediastreamer_voip");
            Java.Lang.JavaSystem.LoadLibrary("linphone");

            // This is mandatory for Android
            LinphoneAndroid.setAndroidContext(JNIEnv.Handle, this.Handle);

            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(bundle);

            AssetManager assets = Assets;
            string path = FilesDir.AbsolutePath;
            string rc_path = path + "/default_rc";
            if (!File.Exists(rc_path))
            {
                using (StreamReader sr = new StreamReader(assets.Open("linphonerc_default")))
                {
                    File.WriteAllText(rc_path, sr.ReadToEnd());
                }
            }
            string factory_path = path + "/factory_rc";
            if (!File.Exists(factory_path))
            {
                using (StreamReader sr = new StreamReader(assets.Open("linphonerc_factory")))
                {
                    File.WriteAllText(factory_path, sr.ReadToEnd());
                }
            }

            global::Xamarin.Forms.Forms.Init(this, bundle);
            App app = new App(); // Do not add an arg to App constructor
            app.ConfigFilePath = rc_path;
            app.FactoryFilePath = factory_path;
            app.Manager.AndroidContext = this;

            LinearLayout fl = new LinearLayout(this);
            ViewGroup.LayoutParams lparams = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            fl.LayoutParameters = lparams;

            displayCamera = new Org.Linphone.Mediastream.Video.Display.GL2JNIView(this);
            ViewGroup.LayoutParams dparams = new ViewGroup.LayoutParams(640, 480);
            displayCamera.LayoutParameters = dparams;
            displayCamera.Holder.SetFixedSize(640, 480);

            captureCamera = new SurfaceView(this);
            ViewGroup.LayoutParams cparams = new ViewGroup.LayoutParams(320, 240);
            captureCamera.LayoutParameters = cparams;
            captureCamera.Holder.SetFixedSize(240, 320);

            fl.AddView(displayCamera);
            fl.AddView(captureCamera);

            AndroidVideoWindowImpl androidView = new AndroidVideoWindowImpl(displayCamera, captureCamera, null);
            app.Core.NativeVideoWindowId = androidView.Handle;
            app.Core.NativePreviewWindowId = captureCamera.Handle;
            app.getLayoutView().Children.Add(fl);

            app.Core.VideoDisplayEnabled = true;
            app.Core.VideoCaptureEnabled = true;

            LoadApplication(app);
        }

        protected override void OnResume()
        {
            base.OnResume();
            if (Int32.Parse(global::Android.OS.Build.VERSION.Sdk) >= 23)
            {
                List<string> Permissions = new List<string>();
                if (this.CheckSelfPermission(Manifest.Permission.Camera) != Permission.Granted)
                {
                    Permissions.Add(Manifest.Permission.Camera);
                }
                if (this.CheckSelfPermission(Manifest.Permission.RecordAudio) != Permission.Granted)
                {
                    Permissions.Add(Manifest.Permission.RecordAudio);
                }
                if (Permissions.Count > 0)
                {
                    this.RequestPermissions(Permissions.ToArray(), PERMISSIONS_REQUEST);
                }
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            if (requestCode == PERMISSIONS_REQUEST)
            {
                int i = 0;
                foreach (string permission in permissions)
                {
                    Log.Info("LinphoneXamarin", "Permission " + permission + " : " + grantResults[i]);
                    i += 1;
                }
            }
        }
    }
}

