using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TD_KT.Updater
{
    internal sealed class UpdaterForm : Form
    {
        private readonly UpdateOptions _options;
        private readonly Label _statusLabel;
        private readonly ProgressBar _progressBar;
        private readonly Label _versionLabel;

        public UpdaterForm(UpdateOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            Text = "Cập nhật phần mềm";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            ControlBox = false;
            ShowInTaskbar = true;
            ClientSize = new Size(720, 430);
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 265,
                BackColor = Color.FromArgb(22, 114, 212)
            };

            var titleLabel = new Label
            {
                AutoSize = false,
                Text = _options.DisplayName,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 20F, FontStyle.Bold, GraphicsUnit.Point),
                TextAlign = ContentAlignment.MiddleCenter,
                Bounds = new Rectangle(40, 92, 640, 48)
            };

            _versionLabel = new Label
            {
                AutoSize = false,
                Text = "Đang cập nhật lên phiên bản " + _options.Version,
                ForeColor = Color.FromArgb(231, 242, 255),
                Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point),
                TextAlign = ContentAlignment.MiddleCenter,
                Bounds = new Rectangle(40, 148, 640, 32)
            };

            var badge = new Label
            {
                AutoSize = false,
                Text = "TD-KT",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point),
                TextAlign = ContentAlignment.MiddleCenter,
                Bounds = new Rectangle(295, 35, 130, 44)
            };

            header.Controls.Add(titleLabel);
            header.Controls.Add(_versionLabel);
            header.Controls.Add(badge);

            _statusLabel = new Label
            {
                AutoSize = false,
                Text = "Đang chuẩn bị cập nhật...",
                ForeColor = Color.FromArgb(75, 85, 99),
                Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point),
                Bounds = new Rectangle(52, 292, 616, 28)
            };

            _progressBar = new ProgressBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Style = ProgressBarStyle.Continuous,
                Bounds = new Rectangle(52, 326, 616, 18)
            };

            var footerLabel = new Label
            {
                AutoSize = false,
                Text = "Trung tâm TSKT 47 - Bộ Tham mưu - Quân chủng Hải quân",
                ForeColor = Color.FromArgb(123, 132, 146),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point),
                Bounds = new Rectangle(52, 385, 500, 24)
            };

            var logoLabel = new Label
            {
                AutoSize = false,
                Text = "TD-KT",
                ForeColor = Color.FromArgb(22, 114, 212),
                Font = new Font("Segoe UI", 13F, FontStyle.Bold, GraphicsUnit.Point),
                TextAlign = ContentAlignment.MiddleRight,
                Bounds = new Rectangle(560, 380, 108, 30)
            };

            Controls.Add(header);
            Controls.Add(_statusLabel);
            Controls.Add(_progressBar);
            Controls.Add(footerLabel);
            Controls.Add(logoLabel);

            Shown += UpdaterForm_Shown;
        }

        private async void UpdaterForm_Shown(object sender, EventArgs e)
        {
            try
            {
                var engine = new UpdaterEngine(_options, ReportProgress);
                await Task.Run(() => engine.Run());
                await Task.Delay(900);
                Close();
            }
            catch (Exception ex)
            {
                ReportProgress(100, "Cập nhật không thành công.");
                ControlBox = true;

                MessageBox.Show(
                    this,
                    "Không thể hoàn tất cập nhật. Phiên bản trước đã được khôi phục khi có thể.\n\nChi tiết: " + ex.Message,
                    "Lỗi cập nhật",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                Close();
            }
        }

        private void ReportProgress(int percent, string status)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<int, string>(ReportProgress), percent, status);
                return;
            }

            _progressBar.Value = Math.Max(_progressBar.Minimum, Math.Min(_progressBar.Maximum, percent));
            _statusLabel.Text = string.IsNullOrWhiteSpace(status) ? "Đang cập nhật..." : status;
        }
    }
}
