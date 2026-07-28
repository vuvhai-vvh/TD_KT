using System;
using System.Windows.Forms;

namespace TD_KT.Updater
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                var options = UpdateOptions.Parse(args);
                Application.Run(new UpdaterForm(options));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không thể khởi động trình cập nhật.\n\nChi tiết: " + ex.Message,
                    "Lỗi cập nhật",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
