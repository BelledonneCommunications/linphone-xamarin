using Linphone;
using System;
using System.Threading;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

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
            
            CoreListener listener = Factory.Instance.CreateCoreListener();
            listener.OnGlobalStateChanged = OnGlobal;
            Core = Factory.Instance.CreateCore(listener, ConfigFilePath, null);
            Core.NetworkReachable = true;

            MainPage = new MainPage();
		}

        private void OnGlobal(Core lc, GlobalState gstate, string message)
        {
            Console.WriteLine("Global state changed: " + gstate);
        }

        private void LinphoneCoreIterate()
        {
            while (true)
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    Core.Iterate();
                });
                Thread.Sleep(50);
            }
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
