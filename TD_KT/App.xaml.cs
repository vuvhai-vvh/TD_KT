using DevExpress.Xpf.Core;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using TD_KT.Services;

namespace TD_KT
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static App()
        {
            CompatibilitySettings.UseLightweightThemes = true;
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var splash = new StartupSplashWindow("Đang khởi động phần mềm...");
            MainWindow = splash;
            splash.Show();

            await Task.Yield();

            var progress = new Progress<string>(splash.SetStatus);
            var updateResult = await new AutoUpdateService().CheckAndStartUpdateAsync(progress);

            if (updateResult.UpdateStarted)
            {
                splash.Close();
                Shutdown();
                return;
            }

            if (updateResult.BlockStartup)
            {
                MessageBox.Show(
                    updateResult.Message,
                    "Không thể cập nhật phần mềm",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                splash.Close();
                Shutdown();
                return;
            }

            splash.SetStatus("Đang mở màn hình đăng nhập...");
            await Task.Delay(200);

            var loginWindow = new MainWindow();
            MainWindow = loginWindow;
            loginWindow.Show();

            ShutdownMode = ShutdownMode.OnLastWindowClose;
            splash.Close();
        }
    }
}
