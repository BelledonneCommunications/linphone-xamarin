using Linphone;
using System;
using System.Threading;
using System.Diagnostics;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

#if ANDROID
using Android.Util;
#endif

[assembly: XamlCompilation (XamlCompilationOptions.Compile)]
namespace Xamarin
{
	public partial class App : Application
    {
        public string ConfigFilePath { get; set; }

        public Core Core { get; set; }

        public App ()
        {
            InitializeComponent();

            LinphoneWrapper.setNativeLogHandler();
            Factory.Instance.EnableLogCollection(LogCollectionState.Enabled);
            LoggingService.Instance.Listener.OnLogMessageWritten = OnLog;

            CoreListener listener = Factory.Instance.CreateCoreListener();
            listener.OnGlobalStateChanged = OnGlobal;
#if ANDROID
            // Giving app context in CreateCore is mandatory for Android to be able to load grammars (and other assets) from AAR
            Core = Factory.Instance.CreateCore(listener, ConfigFilePath, null, IntPtr.Zero, LinphoneAndroid.AndroidContext);
            // Required to be able to store logs as file
            Core.SetLogCollectionPath(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData));
#else
            Core = Factory.Instance.CreateCore(listener, ConfigFilePath, null);
#endif
            Core.NetworkReachable = true;

            MainPage = new MainPage();
        }

        public StackLayout getLayoutView()
        {
            return MainPage.FindByName<StackLayout>("stack_layout");
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

        protected override void OnStart ()
		{
            // Handle when your app starts
#if WINDOWS_UWP
            TimeSpan period = TimeSpan.FromSeconds(1);
            ThreadPoolTimer PeriodicTimer = ThreadPoolTimer.CreatePeriodicTimer(LinphoneCoreIterate , period);
#else
            Thread iterate = new Thread(LinphoneCoreIterate);
            iterate.IsBackground = false;
            iterate.Start();
#endif
        }

        protected override void OnSleep ()
		{
			// Handle when your app sleeps
		}

		protected override void OnResume ()
		{
			// Handle when your app resumes
		}
	}
}
