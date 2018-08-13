using Linphone;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
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

        private Dictionary<string, TransportType> Transports;

        private void OnRegistration(Core lc, ProxyConfig config, RegistrationState state, string message)
        {
            Debug.WriteLine("Registration state changed: " + state);

            registration_status.Text = "Registration state changed: " + state;

            if (state == RegistrationState.Ok)
            {
                register.IsEnabled = false;
                stack_registrar.IsVisible = false;
            }
        }

        private void OnCall(Core lc, Call lcall, CallState state, string message)
        {
            Debug.WriteLine("Call state changed: " + state);

            call_status.Text = "Call state changed: " + state;

            if (lc.CallsNb > 0)
            {
                video.IsEnabled = state == CallState.StreamsRunning;

                if (state == CallState.IncomingReceived)
                {
                    call.Text = "Answer Call (" + lcall.RemoteAddressAsString + ")";
                    video_call.Text = "Answer Call with Video";
                }
                else
                {
                    call.Text = "Terminate Call";
                    video_call.Text = "Terminate Call";
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
                video.IsEnabled = false;
                call.Text = "Start Call";
                call_stats.Text = "";
                video_call.Text = "Start Video Call";
            }
            camera.IsEnabled = video.IsEnabled;
        }

        private void OnStats(Core lc, Call call, CallStats stats)
        {
            Debug.WriteLine("Call stats: " + stats.DownloadBandwidth + " kbits/s / " + stats.UploadBandwidth + " kbits/s");

            call_stats.Text = "Call stats: " + stats.DownloadBandwidth + " kbits/s / " + stats.UploadBandwidth + " kbits/s";
        }

        private void OnLogCollectionUpload(Core lc, CoreLogCollectionUploadState state, string info)
        {
            Debug.WriteLine("Logs upload state changed: " + state + ", url is " + info);

            logsUrl.Text = info;
            var tapGestureRecognizer = new TapGestureRecognizer();
            tapGestureRecognizer.Tapped += (s, e) => {
                Device.OpenUri(new Uri(((Label)s).Text));
            };
            logsUrl.GestureRecognizers.Add(tapGestureRecognizer);
        }

        public MainPage()
		{
			InitializeComponent();

            welcome.Text = "Linphone Xamarin version: " + Core.Version;

            Listener = Factory.Instance.CreateCoreListener();
            Listener.OnRegistrationStateChanged = OnRegistration;
            Listener.OnCallStateChanged = OnCall;
            Listener.OnCallStatsUpdated = OnStats;
            Listener.OnLogCollectionUploadStateChanged = OnLogCollectionUpload;
            Core.AddListener(Listener);
            
            Transports = new Dictionary<string, TransportType>
            {
                { "UDP", TransportType.Udp }, { "TCP", TransportType.Tcp }, { "TLS", TransportType.Tls },
            };
            foreach (string protocol in Transports.Keys)
            {
                transport.Items.Add(protocol);
            }
            transport.SelectedIndex = 2;
        }

        private void OnRegisterClicked(object sender, EventArgs e)
        {
            var authInfo = Factory.Instance.CreateAuthInfo(username.Text, null, password.Text, null, null, domain.Text);
            Core.AddAuthInfo(authInfo);

            var proxyConfig = Core.CreateProxyConfig();
            var identity = Factory.Instance.CreateAddress("sip:sample@domain.tld");
            identity.Username = username.Text;
            identity.Domain = domain.Text;
            identity.Transport = Transports.Values.ElementAt(transport.SelectedIndex);
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

        private void OnVideoCallClicked(object sender, EventArgs e)
        {
            if (Core.CallsNb == 0)
            {
                var addr = Core.InterpretUrl(address.Text);
                CallParams CallParams = Core.CreateCallParams(null);
                CallParams.VideoEnabled = true;
                Core.InviteAddressWithParams(addr, CallParams);
            }
            else
            {
                Call call = Core.CurrentCall;
                if (call.State == CallState.IncomingReceived)
                {
                    CallParams CallParams = Core.CreateCallParams(call);
                    CallParams.VideoEnabled = true;
                    Core.AcceptCallWithParams(call, CallParams);
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

        private void OnCameraClicked(object sender, EventArgs e)
        {
            if (Core.CallsNb > 0)
            {
                Call call = Core.CurrentCall;
                if (call.State == CallState.StreamsRunning)
                {
                    try
                    {
                        string currentDevice = Core.VideoDevice;
                        IEnumerable<string> devices = Core.VideoDevicesList;
                        int index = 0;
                        foreach (string d in devices)
                        {
                            if (d == currentDevice)
                            {
                                break;
                            }
                            index++;
                        }

                        String newDevice;
                        if (index == 1)
                        {
                            newDevice = devices.ElementAt(0);
                        }
                        else if (devices.Count() > 1)
                        {
                            newDevice = devices.ElementAt(1);
                        }
                        else
                        { 
                            newDevice = devices.ElementAt(index);
                        }
                        Core.VideoDevice = newDevice;

                        Core.UpdateCall(call, call.Params);
                    }
                    catch (ArithmeticException)
                    {
                        Debug.WriteLine("Cannot swtich camera : no camera");
                    }
                }
            }
        }

        private void onUploadLogsCliked(object sender, EventArgs e)
        {
            Core.UploadLogCollection();
        }
    }
}
