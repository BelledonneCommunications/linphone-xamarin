using System;
using System.Threading;

using Xamarin.Forms;

using Linphone;
using Windows.System.Threading;
using Windows.UI.Core;

namespace LinphoneXamarin
{
	public partial class App : Application
	{
        public Core LinphoneCore { get; set; }

		public App (string rc_path = null)
        {
            LinphoneWrapper.setNativeLogHandler();

            CoreListener listener = Factory.Instance.CreateCoreListener();
            listener.OnGlobalStateChanged = OnGlobal;
            LinphoneCore = Factory.Instance.CreateCore(listener, rc_path, null);

            MainPage = new MainPage();
		}

#if !WINDOWS_UWP
        private void LinphoneCoreIterate()
        {
            while (true)
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    LinphoneCore.Iterate();
                });
               Thread.Sleep(50);
            }
        }
#endif

        private void OnGlobal(Core lc, GlobalState gstate, string message)
        {
            // Console.WriteLine("Global state changed: " + gstate);
        }

        protected override void OnStart ()
		{
            // Handle when your app starts
#if WINDOWS_UWP
            TimeSpan period = TimeSpan.FromMilliseconds(50);
            ThreadPoolTimer PeriodicTimer = ThreadPoolTimer.CreatePeriodicTimer((source) =>
            {
                Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High,
                () =>
                {
                    LinphoneCore.Iterate();
                });
            }, period);
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
