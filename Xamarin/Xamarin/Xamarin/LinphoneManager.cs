using System;
using System.Threading;
using System.Diagnostics;
using Linphone;
using Xamarin.Forms;

namespace TutoXamarin
{
    public class LinphoneManager
    {
        public Core Core { get; set; }
        LoggingService LoggingService
        {
            get; set;
        }

        private System.Timers.Timer Timer;

        public LinphoneManager()
        {
            LoggingService = LoggingService.Instance;
            LoggingService.LogLevel = LogLevel.Message;
            LoggingService.Listener.OnLogMessageWritten = OnLog;

            Debug.WriteLine("==== Phone information dump ====");
            Debug.WriteLine("C# WRAPPER=" + LinphoneWrapper.VERSION);
        }

        public void Init(string configPath, string factoryPath, IntPtr context)
        {
            // Giving app context in CreateCore is mandatory for Android to be able to load grammars (and other assets) from AAR
            Core = Factory.Instance.CreateCore(configPath, factoryPath, context);
            // Required to be able to store logs as file
            Core.SetLogCollectionPath(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
        }

        public void Start()
        {
            Timer = new System.Timers.Timer
            {
                Interval = 20
            };
            Timer.Elapsed += OnTimedEvent;
            Timer.Enabled = true;
            Core.Start();
        }

        private void OnTimedEvent(object sender, System.Timers.ElapsedEventArgs e)
        {
            Device.BeginInvokeOnMainThread(() => Core.Iterate());
        }

        private void OnLog(LoggingService logService, string domain, LogLevel lev, string message)
        {
            string now = DateTime.Now.ToString("hh:mm:ss");
            string log = now + " [";
            switch (lev)
            {
                case LogLevel.Debug:
                    log += "DEBUG";
                    break;
                case LogLevel.Error:
                    log += "ERROR";
                    break;
                case LogLevel.Message:
                    log += "MESSAGE";
                    break;
                case LogLevel.Warning:
                    log += "WARNING";
                    break;
                case LogLevel.Fatal:
                    log += "FATAL";
                    break;
                default:
                    break;
            }
            log += "] (" + domain + ") " + message;
            Debug.WriteLine(log);
        }

        private void OnGlobal(Core lc, GlobalState gstate, string message)
        {
            Debug.WriteLine("Global state changed: " + gstate);
        }
    }
}
