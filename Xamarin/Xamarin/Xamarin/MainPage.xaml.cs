using Linphone;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Xamarin
{
	public partial class MainPage : ContentPage
	{
        private Core Core
        {
            get
            {
                return ((App)App.Current).Core;
            }
        }
        private CoreListener Listener;

        private void OnRegistration(Core lc, ProxyConfig config, RegistrationState state, string message)
        {
            Console.WriteLine("Registration state changed: " + state);

            registration_status.Text = "Registration state changed: " + state;

            if (state == RegistrationState.Ok)
            {
                register.IsEnabled = false;
                this.FindByName<StackLayout>("stack_registrar").IsVisible = false;
            }
        }

        private void OnCall(Core lc, Call lcall, CallState state, string message)
        {
            Console.WriteLine("Call state changed: " + state);

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
                if (lcall.CurrentParams.VideoEnabled)
                {
                    video.Text = "Stop Video";
                }
                else
                {
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
            Console.WriteLine("Call stats: " + stats.DownloadBandwidth + " kbits/s / " + stats.UploadBandwidth + " kbits/s");

            call_stats.Text = "Call stats: " + stats.DownloadBandwidth + " kbits/s / " + stats.UploadBandwidth + " kbits/s";
        }

        public MainPage()
		{
			InitializeComponent();

            welcome.Text = "Linphone Xamarin version: " + Core.Version;

            Listener = Factory.Instance.CreateCoreListener();
            Listener.OnRegistrationStateChanged = OnRegistration;
            Listener.OnCallStateChanged = OnCall;
            Listener.OnCallStatsUpdated = OnStats;
            Core.AddListener(Listener);
        }

        private void OnRegisterClicked(object sender, EventArgs e)
        {
            var authInfo = Factory.Instance.CreateAuthInfo(username.Text, null, password.Text, null, null, domain.Text);
            Core.AddAuthInfo(authInfo);

            var proxyConfig = Core.CreateProxyConfig();
            var identity = Factory.Instance.CreateAddress("sip:sample@domain.tld");
            identity.Username = username.Text;
            identity.Domain = domain.Text;
            proxyConfig.Edit();
            proxyConfig.IdentityAddress = identity;
            proxyConfig.ServerAddr = domain.Text;
            proxyConfig.Route = domain.Text;
            proxyConfig.RegisterEnabled = true;
            proxyConfig.Done();
            Core.AddProxyConfig(proxyConfig);
            Core.DefaultProxyConfig = proxyConfig;

            Core.RefreshRegisters();
        }

        private void OnCallClicked(object sender, EventArgs e)
        {
            if (Core.CallsNb == 0)
            {
                var addr = Core.InterpretUrl(address.Text);
                Core.InviteAddress(addr);
            }
            else
            {
                Call call = Core.CurrentCall;
                if (call.State == CallState.IncomingReceived)
                {
                    Core.AcceptCall(call);
                }
                else
                {
                    Core.TerminateAllCalls();
                }
            }
        }

        private void OnVideoClicked(object sender, EventArgs e)
        {
            if (Core.CallsNb > 0)
            {
                Call call = Core.CurrentCall;
                if (call.State == CallState.StreamsRunning)
                {
                    Core.VideoAdaptiveJittcompEnabled = true;
                    CallParams param = Core.CreateCallParams(call);
                    param.VideoEnabled = !call.CurrentParams.VideoEnabled;
                    param.VideoDirection = MediaDirection.SendRecv;
                    Core.UpdateCall(call, param);
                }
            }
        }
    }
}
