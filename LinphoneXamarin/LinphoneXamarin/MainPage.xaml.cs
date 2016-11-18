using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Xamarin.Forms;

namespace LinphoneXamarin
{
	public partial class MainPage : ContentPage
    {
        const string LIBNAME = "liblinphone-armeabi-v7a";

        struct LinphoneCoreVTable
        {
            public IntPtr global_state_changed;
            public IntPtr registration_state_changed;
            public IntPtr call_state_changed;
            public IntPtr notify_presence_received;
            public IntPtr notify_presence_received_for_uri_or_tel;
            public IntPtr new_subscription_requested;
            public IntPtr auth_info_requested;
            public IntPtr authentication_requested;
            public IntPtr call_log_updated;
            public IntPtr message_received;
            public IntPtr is_composing_received;
            public IntPtr dtmf_received;
            public IntPtr refer_received;
            public IntPtr call_encryption_changed;
            public IntPtr transfer_state_changed;
            public IntPtr buddy_info_updated;
            public IntPtr call_stats_updated;
            public IntPtr info_received;
            public IntPtr subscription_state_changed;
            public IntPtr notify_received;
            public IntPtr publish_state_changed;
            public IntPtr configuring_status;
            public IntPtr display_status;
            public IntPtr display_message;
            public IntPtr display_warning;
            public IntPtr display_url;
            public IntPtr show;
            public IntPtr text_received;
            public IntPtr file_transfer_recv;
            public IntPtr file_transfer_send;
            public IntPtr file_transfer_progress_indication;
            public IntPtr network_reachable;
            public IntPtr log_collection_upload_state_changed;
            public IntPtr log_collection_upload_progress_indication;
            public IntPtr friend_list_created;
            public IntPtr friend_list_removed;
        };

        public enum LinphoneGlobalState
        {
            LinphoneGlobalOff,
            LinphoneGlobalStartup,
            LinphoneGlobalOn,
            LinphoneGlobalShutdown,
            LinphoneGlobalConfiguring
        };

        [DllImport(LIBNAME)]
        static extern IntPtr linphone_core_v_table_new();

        [DllImport(LIBNAME)]
        static extern IntPtr linphone_core_new(IntPtr vtable, string config_path, string factory_config_path, IntPtr userdata);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void LinphoneCoreGlobalStateChangedCb(IntPtr lc, LinphoneGlobalState state, string message);

        void OnGlobalStateChanged(IntPtr lc, LinphoneGlobalState state, string message)
        {
            globalStateLabel.Text += "[LINPHONE GLOBAL STATE CHANGED] " + message + "\r\n";
        }

        public MainPage()
		{
			InitializeComponent();
        }

        void OnButtonClicked(object sender, EventArgs args)
        {
            globalStateLabel.Text = "";
            startLinphoneButton.IsEnabled = false;

            IntPtr vtablePtr = linphone_core_v_table_new();
            LinphoneCoreVTable vtable = Marshal.PtrToStructure<LinphoneCoreVTable>(vtablePtr);
            LinphoneCoreGlobalStateChangedCb global_cb = new LinphoneCoreGlobalStateChangedCb(OnGlobalStateChanged);
            vtable.global_state_changed = Marshal.GetFunctionPointerForDelegate(global_cb);
            Marshal.StructureToPtr(vtable, vtablePtr, false);

            IntPtr corePtr = linphone_core_new(vtablePtr, null, null, IntPtr.Zero);
        }
    }
}
