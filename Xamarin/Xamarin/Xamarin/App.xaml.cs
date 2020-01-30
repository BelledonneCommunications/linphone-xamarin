/*
 * Copyright (c) 2010-2019 Belledonne Communications SARL.
 *
 * This file is part of linphone-android
 * (see https://www.linphone.org).
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see <http://www.gnu.org/licenses/>.
 */

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
