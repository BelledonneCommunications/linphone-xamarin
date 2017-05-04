using System;
using System.Threading;

using Xamarin.Forms;

using Linphone;

namespace LinphoneXamarin
{
	public partial class App : Application
	{
        public Core LinphoneCore { get; set; }

		public App (string rc_path)
        {
            CoreListener listener = Factory.Instance.CreateCoreListener();
            listener.OnGlobalStateChanged = OnGlobal;
            LinphoneCore = Factory.Instance.CreateCore(listener, rc_path, null);

            MainPage = new MainPage();
		}

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

        private void OnGlobal(Core lc, GlobalState gstate, string message)
        {
            Console.WriteLine("Global state changed: " + gstate);
        }

        protected override void OnStart ()
		{
            // Handle when your app starts
            Thread iterate = new Thread(LinphoneCoreIterate);
            iterate.IsBackground = false;
            iterate.Start();
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
