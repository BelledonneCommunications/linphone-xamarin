using System;
using System.Runtime.InteropServices;
using System.Threading;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

using Org.Linphone.Mediastream.Video;
using Org.Linphone.Mediastream.Video.Display;

namespace LinphoneXamarin.Droid
{
    [Activity(Label = "LinphoneXamarin", Icon = "@drawable/icon", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsApplicationActivity,  AndroidVideoWindowImpl.IVideoWindowListener
    {
        private const string LIBNAME = "liblinphone-armeabi-v7a";
        private LinphoneXamarin.App app;
        private System.Threading.Thread coreIterate;
        private GL2JNIView view1;
        private SurfaceView view2;
        private AndroidVideoWindowImpl androidVideo;

        [DllImport(LIBNAME)]
        static extern void ms_set_jvm_from_env(IntPtr jnienv);

        [DllImport(LIBNAME)]
        static extern void setAndroidLogHandler();

        [DllImport(LIBNAME)]
        static extern void setMediastreamerAndroidContext(IntPtr jnienv, IntPtr context);

        [DllImport(LIBNAME)]
        static extern void linphone_core_iterate(IntPtr lc);

        [DllImport(LIBNAME)]
        static extern void linphone_core_set_native_video_window_id(IntPtr lc, IntPtr id);

        [DllImport(LIBNAME)]
        static extern void linphone_core_set_native_preview_window_id(IntPtr lc, IntPtr id);

        [DllImport(LIBNAME)]
        static extern void linphone_core_enable_video(IntPtr lc, int enable_c, int enable_d);

        [DllImport(LIBNAME)]
        static extern IntPtr linphone_core_create_call_params(IntPtr lc, IntPtr call);

        [DllImport(LIBNAME)]
        static extern void linphone_call_params_enable_video(IntPtr callp, int enabled);

        [DllImport(LIBNAME)]
        static extern IntPtr linphone_address_new(string to);

        [DllImport(LIBNAME)]
        static extern IntPtr linphone_core_invite_address_with_params(IntPtr lc, IntPtr to, IntPtr callp);

        void LinphoneCoreIterate()
        {
            while (true)
            {
                RunOnUiThread(() => linphone_core_iterate(app.corePtr));
                System.Threading.Thread.Sleep(50);
            }
        }

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
            setAndroidLogHandler();
            setMediastreamerAndroidContext(Android.Runtime.JNIEnv.Handle, this.Handle);

            app = new LinphoneXamarin.App();
            LoadApplication(app);

            coreIterate = new Thread(LinphoneCoreIterate);
            coreIterate.IsBackground = false;
            coreIterate.Start();

            view1 = new GL2JNIView(this);
            view1.LayoutParameters = new LinearLayout.LayoutParams(500, 500);
            view2 = new SurfaceView(this);
            view2.LayoutParameters = new LinearLayout.LayoutParams(500, 500);
            LinearLayout ll = new LinearLayout(this);
            ll.Orientation = Orientation.Vertical;
            ll.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
            ll.AddView(view1);
            ll.AddView(view2);
            SetContentView(ll);
            view1.SetZOrderOnTop(false);
            view2.SetZOrderOnTop(true);

            androidVideo = new AndroidVideoWindowImpl(view1, view2, this);
        }

        protected override void OnResume()
        {
            base.OnResume();

            linphone_core_enable_video(app.corePtr, 1, 1);
            IntPtr callp = linphone_core_create_call_params(app.corePtr, IntPtr.Zero);
            linphone_call_params_enable_video(callp, 1);
            IntPtr to = linphone_address_new("sip:sylvain@sip.linphone.org");
            linphone_core_invite_address_with_params(app.corePtr, to, callp);
            linphone_core_set_native_video_window_id(app.corePtr, androidVideo.Handle);
        }

        public void OnVideoPreviewSurfaceDestroyed(AndroidVideoWindowImpl p0)
        {

        }

        public void OnVideoPreviewSurfaceReady(AndroidVideoWindowImpl p0, SurfaceView p1)
        {
            linphone_core_set_native_preview_window_id(app.corePtr, p1.Handle);
        }

        public void OnVideoRenderingSurfaceDestroyed(AndroidVideoWindowImpl p0)
        {

        }

        public void OnVideoRenderingSurfaceReady(AndroidVideoWindowImpl p0, SurfaceView p1)
        {
            linphone_core_set_native_video_window_id(app.corePtr, p0.Handle);
        }
    }
}

