using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;
using Linphone;
using Xamarin.Forms;

#if ANDROID
using Android.Util;
#endif

namespace Xamarin
{
    public class LinphoneManager
    {
        public Core Core { get; set; }

        public LinphoneManager()
        {
            LinphoneWrapper.setNativeLogHandler();
            Factory.Instance.EnableLogCollection(LogCollectionState.Enabled);
            LoggingService.Instance.LogLevel = LogLevel.Debug;
            LoggingService.Instance.Listener.OnLogMessageWritten = OnLog;

            Debug.WriteLine("LinphoneWrapper.cs version is " + LinphoneWrapper.VERSION);
        }

        public void Init(string configPath)
        {
            CoreListener listener = Factory.Instance.CreateCoreListener();
            listener.OnGlobalStateChanged = OnGlobal;
#if ANDROID
            // Giving app context in CreateCore is mandatory for Android to be able to load grammars (and other assets) from AAR
            Core = Factory.Instance.CreateCore(listener, configPath, null, IntPtr.Zero, LinphoneAndroid.AndroidContext);
            // Required to be able to store logs as file
            Core.SetLogCollectionPath(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
#else
            Core = Factory.Instance.CreateCore(listener, ConfigFilePath, null);
#endif
            Core.NetworkReachable = true;
        }

        public void Start()
        {
#if WINDOWS_UWP
            TimeSpan period = TimeSpan.FromSeconds(1);
            ThreadPoolTimer PeriodicTimer = ThreadPoolTimer.CreatePeriodicTimer(LinphoneCoreIterate , period);
#else
            Thread iterate = new Thread(LinphoneCoreIterate);
            iterate.IsBackground = false;
            iterate.Start();
#endif
        }

#if WINDOWS_UWP
        private void LinphoneCoreIterate(ThreadPoolTimer timer) {
#else
        private void LinphoneCoreIterate()
        {
#endif
            while (true)
            {
#if WINDOWS_UWP
                CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High,
                () => {
                    LinphoneCore.Iterate();
                });
#else
                Device.BeginInvokeOnMainThread(() =>
                {
                    Core.Iterate();
                });
                Thread.Sleep(50);
#endif
            }
        }

        private void OnLog(LoggingService logService, string domain, LogLevel lev, string message)
        {
            string now = DateTime.Now.ToString("hh:mm:ss");
            string log = now + " [";
            switch (lev)
            {
                case LogLevel.Debug:
                    log += "DEBUG";
#if ANDROID
                    Log.Debug(domain, message);
#endif
                    break;
                case LogLevel.Error:
                    log += "ERROR";
#if ANDROID
                    Log.Error(domain, message);
#endif
                    break;
                case LogLevel.Message:
                    log += "MESSAGE";
#if ANDROID
                    Log.Info(domain, message);
#endif
                    break;
                case LogLevel.Warning:
                    log += "WARNING";
#if ANDROID
                    Log.Warn(domain, message);
#endif
                    break;
                case LogLevel.Fatal:
                    log += "FATAL";
#if ANDROID
                    Log.Error(domain, message);
#endif
                    break;
                default:
                    break;
            }
            log += "] (" + domain + ") " + message;
#if WINDOWS_UWP
            Debug.WriteLine(log);
#endif
        }

        private void OnGlobal(Core lc, GlobalState gstate, string message)
        {
            Debug.WriteLine("Global state changed: " + gstate);
        }
    }
}
