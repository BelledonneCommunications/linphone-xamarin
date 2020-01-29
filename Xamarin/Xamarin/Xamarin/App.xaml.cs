using Linphone;
using System;
using System.Threading;
using System.Diagnostics;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation (XamlCompilationOptions.Compile)]
namespace TutoXamarin
{
	public partial class App : Application
    {
        public static string ConfigFilePath { get; set; }
        public static string FactoryFilePath { get; set; }

        public LinphoneManager Manager { get; set; }

        public Core Core
        {
            get
            {
                return Manager.Core;
            }
        }

        public App (IntPtr context)
        {
            InitializeComponent();

            Manager = new LinphoneManager();
            Manager.Init(ConfigFilePath, FactoryFilePath, context);

            MainPage = new MainPage();
        }

        public StackLayout getLayoutView()
        {
            return MainPage.FindByName<StackLayout>("stack_layout");
        }
        
        protected override void OnStart ()
		{
            // Handle when your app starts
            Manager.Start();
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
