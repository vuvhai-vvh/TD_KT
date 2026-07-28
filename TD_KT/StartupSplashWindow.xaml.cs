using System;
using System.Windows;
using TD_KT.Services;

namespace TD_KT
{
    public partial class StartupSplashWindow : Window
    {
        public StartupSplashWindow(string initialStatus = "Đang khởi động...")
        {
            InitializeComponent();
            txtApplicationName.Text = AppInfo.DisplayName;
            txtVersion.Text = "Phiên bản " + AppInfo.VersionText;
            SetStatus(initialStatus);
        }

        public void SetStatus(string status)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => SetStatus(status));
                return;
            }

            txtStatus.Text = string.IsNullOrWhiteSpace(status) ? "Đang xử lý..." : status.Trim();
        }
    }
}
