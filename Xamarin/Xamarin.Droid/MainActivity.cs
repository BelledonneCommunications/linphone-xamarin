using Android.App;
using Android.Runtime;
using Android.OS;
using Android.Content.Res;

using System;
using System.IO;
using System.Threading;

using Linphone;

namespace LinphoneXamarin.Droid
{
	[Activity (Label = "LinphoneXamarin.Droid", MainLauncher = true, Icon = "@drawable/icon")]
	public class MainActivity : Activity
	{
        private Core core;

        private void LinphoneCoreIterate()
        {
            while (true)
            {
                RunOnUiThread(() => core.Iterate());
                System.Threading.Thread.Sleep(50);
            }
        }

        private void OnGlobal(Core lc, GlobalState gstate, string message)
        {
            Console.WriteLine("Global state changed: " + gstate);
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Copy the default_rc from the Assets to the device
            // The same can be done for the factory_rc
            AssetManager assets = this.Assets;
            string path = this.FilesDir.AbsolutePath;
            using (var br = new BinaryReader(Application.Context.Assets.Open("linphonerc_default")))
            {
                using (var bw = new BinaryWriter(new FileStream(path + "/default_rc", FileMode.Create)))
                {
                    byte[] buffer = new byte[2048];
                    int length = 0;
                    while ((length = br.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        bw.Write(buffer, 0, length);
                    }
                }
            }

            // Load the libraries
            Java.Lang.JavaSystem.LoadLibrary("bctoolbox");
            Java.Lang.JavaSystem.LoadLibrary("ortp");
            Java.Lang.JavaSystem.LoadLibrary("mediastreamer_base");
            Java.Lang.JavaSystem.LoadLibrary("mediastreamer_voip");
            Java.Lang.JavaSystem.LoadLibrary("linphone");

            SetContentView(Resource.Layout.Main);

            // This is mandatory for Android
            LinphoneAndroid.setAndroidContext(JNIEnv.Handle, this.Handle);
            LinphoneAndroid.setNativeLogHandler();

            CoreListener listener = Factory.Instance.CreateCoreListener();
            listener.OnGlobalStateChanged = OnGlobal;
            core =Factory.Instance.CreateCore(listener, path + "/default_rc", null);

            Thread coreIterate = new Thread(LinphoneCoreIterate);
            coreIterate.IsBackground = false;
            coreIterate.Start();
        }
    }
}


