using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;
using Linphone;
using Xamarin.Forms;

#if ANDROID
using Android.Util;
using Android.App;
#endif

namespace Xamarin
{
    public class LinphoneManager
    {
        public Core Core { get; set; }
        LoggingService LoggingService
        {
            get; set;
        }
        

#if ANDROID
        public Activity AndroidContext { get; set; }
#endif

        private System.Timers.Timer Timer;

        public LinphoneManager()
        {
            Debug.WriteLine("==== Phone information dump ====");
#if ANDROID
            Debug.WriteLine("DEVICE=" + global::Android.OS.Build.Device);
            Debug.WriteLine("MODEL=" + global::Android.OS.Build.Model);
            Debug.WriteLine("MANUFACTURER=" + global::Android.OS.Build.Manufacturer);
            Debug.WriteLine("SDK=" + global::Android.OS.Build.VERSION.Sdk);
#endif
            //LinphoneWrapper.setNativeLogHandler();
            LoggingService = LoggingService.Instance;
            LoggingService.LogLevel = LogLevel.Message;
            LoggingService.Listener.OnLogMessageWritten = OnLog;

            Debug.WriteLine("C# WRAPPER=" + LinphoneWrapper.VERSION);
        }

        public void Init(string configPath, string factoryPath)
        {

#if ANDROID
            // Giving app context in CreateCore is mandatory for Android to be able to load grammars (and other assets) from AAR
            Core = Factory.Instance.CreateCore(configPath, factoryPath, LinphoneAndroid.AndroidContext);
            // Required to be able to store logs as file
            Core.SetLogCollectionPath(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
#else
            Core = Factory.Instance.CreateCore(configPath, factoryPath, IntPtr.Zero);
            Core.Listener.OnGlobalStateChanged += OnGlobal;
#endif
        }

        public void Start()
        {
#if WINDOWS_UWP
            TimeSpan period = TimeSpan.FromSeconds(1);
            ThreadPoolTimer PeriodicTimer = ThreadPoolTimer.CreatePeriodicTimer(LinphoneCoreIterate , period);
#else
            Timer = new System.Timers.Timer();
            Timer.Interval = 20;
            Timer.Elapsed += OnTimedEvent;
            Timer.Enabled = true;

            //Core.NetworkReachable = true;
            Core.Start();

#endif
        }

        private void OnTimedEvent(object sender, System.Timers.ElapsedEventArgs e)
        {
#if ANDROID
            AndroidContext.RunOnUiThread(() => Core.Iterate());
#else
            Device.BeginInvokeOnMainThread(() => Core.Iterate());
#endif
        }

#if WINDOWS_UWP
        private void LinphoneCoreIterate(ThreadPoolTimer timer) {
            while (true)
            {
                CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High, () => LinphoneCore.Iterate());
            }
        }
#endif

        private void OnLog(LoggingService logService, string domain, LogLevel lev, string message)
        {
            string now = DateTime.Now.ToString("hh:mm:ss");
            string log = now + " [";
            switch (lev)
            {
                case LogLevel.Debug:
                    log += "DEBUG";
                    break;
                case LogLevel.Error:
                    log += "ERROR";
                    break;
                case LogLevel.Message:
                    log += "MESSAGE";
                    break;
                case LogLevel.Warning:
                    log += "WARNING";
                    break;
                case LogLevel.Fatal:
                    log += "FATAL";
                    break;
                default:
                    break;
            }
            log += "] (" + domain + ") " + message;
            Debug.WriteLine(log);
        }

        private void OnGlobal(Core lc, GlobalState gstate, string message)
        {
            Debug.WriteLine("Global state changed: " + gstate);
            if (gstate == GlobalState.On)
            {
                //lc.MsFactory.addDevicesInfo(global::Android.OS.Build.Manufacturer, global::Android.OS.Build.Model, global::Android.OS.Build.Device, 1 << 1, 0, 0);
            }
        }
    }
}
