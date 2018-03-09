using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

using Linphone;
using System.Diagnostics;

namespace LinphoneXamarin
{
	public partial class MainPage : ContentPage
	{
        private Core LinphoneCore
        {
            get
            {
                return ((App)App.Current).LinphoneCore;
            }
        }
        private CoreListener Listener;

        private void OnRegistration(Core lc, ProxyConfig config, RegistrationState state, string message)
        {
#if WINDOWS_UWP
            Debug.WriteLine("Registration state changed: " + state);
#else
            Console.WriteLine("Registration state changed: " + state);
#endif
            registration_status.Text = "Registration state changed: " + state;

            if (state == RegistrationState.Ok)
            {
                register.IsEnabled = false;
                this.FindByName<StackLayout>("stack_registrar").IsVisible = false;
            }
        }

        private void OnCall(Core lc, Call lcall, CallState state, string message)
        {
#if WINDOWS_UWP
            Debug.WriteLine("Call state changed: " + state);
#else
            Console.WriteLine("Call state changed: " + state);
#endif
            call_status.Text = "Call state changed: " + state;

            if (lc.CallsNb > 0)
            {
                if (state == CallState.IncomingReceived)
                {
                    call.Text = "Answer Call (" + lcall.RemoteAddressAsString + ")";
                }
                else
                {
                    call.Text = "Terminate Call";
                }
                if (lcall.CurrentParams.VideoEnabled) {
                    video.Text = "Stop Video";
                } else {
                    video.Text = "Start Video";
                }
            }
            else
            {
                call.Text = "Start Call";
                call_stats.Text = "";
            }
        }

        private void OnStats(Core lc, Call call, CallStats stats)
        {
#if WINDOWS_UWP
            Debug.WriteLine("Call stats: " + stats.DownloadBandwidth + " kbits/s / " + stats.UploadBandwidth + " kbits/s");
#else
            Console.WriteLine("Call stats: " + stats.DownloadBandwidth + " kbits/s / " + stats.UploadBandwidth + " kbits/s");
#endif
            call_stats.Text = "Call stats: " + stats.DownloadBandwidth + " kbits/s / " + stats.UploadBandwidth + " kbits/s";
        }

        public MainPage()
		{
			InitializeComponent();

            Listener = Factory.Instance.CreateCoreListener();
            Listener.OnRegistrationStateChanged = OnRegistration;
            Listener.OnCallStateChanged = OnCall;
            Listener.OnCallStatsUpdated = OnStats;
            LinphoneCore.AddListener(Listener);
        }

        private void OnRegisterClicked(object sender, EventArgs e)
        {
            var authInfo = Factory.Instance.CreateAuthInfo(username.Text, null, password.Text, null, null, domain.Text);
            LinphoneCore.AddAuthInfo(authInfo);

            var proxyConfig = LinphoneCore.CreateProxyConfig();
            var identity = Factory.Instance.CreateAddress("sip:sample@domain.tld");
            identity.Username = username.Text;
            identity.Domain = domain.Text;
            proxyConfig.Edit();
            proxyConfig.IdentityAddress = identity;
            proxyConfig.ServerAddr = domain.Text;
            proxyConfig.Route = domain.Text;
            proxyConfig.RegisterEnabled = true;
            proxyConfig.Done();
            LinphoneCore.AddProxyConfig(proxyConfig);
            LinphoneCore.DefaultProxyConfig = proxyConfig;

            LinphoneCore.RefreshRegisters();
        }

        private void OnCallClicked(object sender, EventArgs e)
        {
            if (LinphoneCore.CallsNb == 0)
            {
                var addr = LinphoneCore.InterpretUrl(address.Text);
                LinphoneCore.InviteAddress(addr);
            }
            else
            {
                Call call = LinphoneCore.CurrentCall;
                if (call.State == CallState.IncomingReceived)
                {
                    LinphoneCore.AcceptCall(call);
                }
                else
                {
                    LinphoneCore.TerminateAllCalls();
                }
            }
        }

        private void OnVideoClicked(object sender, EventArgs e) {
            if (LinphoneCore.CallsNb > 0) {
                Call call = LinphoneCore.CurrentCall;
                if (call.State == CallState.StreamsRunning) {
                    LinphoneCore.VideoAdaptiveJittcompEnabled = true;
                    CallParams param = LinphoneCore.CreateCallParams(call);
                    param.VideoEnabled = !call.CurrentParams.VideoEnabled;
                    param.VideoDirection = MediaDirection.SendRecv;
                    LinphoneCore.UpdateCall(call, param);
                }
            }
        }
    }
}
