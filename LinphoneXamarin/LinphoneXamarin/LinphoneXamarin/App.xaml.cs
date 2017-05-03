using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xamarin.Forms;

using Linphone;

namespace LinphoneXamarin
{
	public partial class App : Application
	{
        public Core Core;

		public App ()
		{
			MainPage = new LinphoneXamarin.MainPage();
		}

        private void OnGlobal(Core lc, GlobalState gstate, string message)
        {
            Console.WriteLine("Global state changed: " + gstate);
        }

        protected override void OnStart ()
		{
            // Handle when your app starts
            CoreListener listener = Factory.Instance.CreateCoreListener();
            listener.OnGlobalStateChanged = OnGlobal;
            Core = Factory.Instance.CreateCore(listener, null, null);
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
