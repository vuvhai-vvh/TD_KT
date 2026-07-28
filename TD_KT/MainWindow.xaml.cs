using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using TD_KT.Models;
using TD_KT.Services;

namespace TD_KT
{
    public partial class MainWindow : Window
    {
        private bool _isOpeningHome;

        private static readonly string RememberFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TD-KT",
            "remember.txt");

        public MainWindow()
        {
            InitializeComponent();

            // Cho phép kéo cửa sổ
            this.MouseLeftButtonDown += (s, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                    this.DragMove();
            };

            // Focus vào ô username + nạp "Ghi nhớ đăng nhập"
            Loaded += (s, e) =>
            {
                LoadRememberedCredentials();
                if (string.IsNullOrWhiteSpace(txtUsername.Text))
                    txtUsername.Focus();
                else
                    txtPassword.Focus();
            };

            // Enter để đăng nhập
            txtPassword.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                    Login();
            };
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            Login();
        }

        private void Login()
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Password;

            if (string.IsNullOrEmpty(username))
            {
                MessageBox.Show("Vui lòng nhập tên đăng nhập!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtUsername.Focus();
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Vui lòng nhập mật khẩu!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPassword.Focus();
                return;
            }

            // Xác thực đăng nhập từ database (Users.Username/Password/IsActive)
            User loggedInUser = null;
            try
            {
                using (var db = new Database())
                {
                    // EF không support StringComparison trong SQL => dùng ToLower
                    var u = username.ToLowerInvariant();
                    loggedInUser = db.Users
                        .FirstOrDefault(x => x.Username != null && x.Username.ToLower() == u);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không kết nối được cơ sở dữ liệu!\n\nChi tiết: {ex.Message}",
                    "Lỗi kết nối", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            bool isPasswordOk = false;

            if (loggedInUser != null && loggedInUser.IsActive)
            {
                if (!string.IsNullOrWhiteSpace(loggedInUser.PasswordHash) &&
                    !string.IsNullOrWhiteSpace(loggedInUser.PasswordSalt))
                {
                    isPasswordOk = PasswordHasherService.VerifyPassword(
                        password,
                        loggedInUser.PasswordHash,
                        loggedInUser.PasswordSalt,
                        loggedInUser.PasswordIterations,
                        loggedInUser.PasswordAlgo
                    );
                }
                else if (!string.IsNullOrWhiteSpace(loggedInUser.Password))
                {
                    // Legacy plaintext password (migration from old DB)
                    isPasswordOk = loggedInUser.Password == password;

                    if (isPasswordOk)
                    {
                        try
                        {
                            var hash = PasswordHasherService.HashPassword(password);

                            using (var db2 = new Database())
                            {
                                var u = db2.Users.FirstOrDefault(x => x.Id == loggedInUser.Id);
                                if (u != null)
                                {
                                    u.PasswordHash = hash.HashBase64;
                                    u.PasswordSalt = hash.SaltBase64;
                                    u.PasswordAlgo = hash.Algorithm;
                                    u.PasswordIterations = hash.Iterations;
                                    u.Password = null;
                                    db2.SaveChanges();
                                }
                            }

                            loggedInUser.PasswordHash = hash.HashBase64;
                            loggedInUser.PasswordSalt = hash.SaltBase64;
                            loggedInUser.PasswordAlgo = hash.Algorithm;
                            loggedInUser.PasswordIterations = hash.Iterations;
                            loggedInUser.Password = null;
                        }
                        catch
                        {
                            // ignore migrate failure - still allow login
                        }
                    }
                }
            }

            if (loggedInUser != null && loggedInUser.IsActive && isPasswordOk)
            {
                if (chkRemember.IsChecked == true)
                {
                    SaveRememberedCredentials(username, password);
                }
                else
                {
                    ClearRememberedCredentials();
                }

                OpenHomeWithLoading(loggedInUser);
            }
            else
            {
                MessageBox.Show("Tên đăng nhập hoặc mật khẩu không đúng!", "Lỗi đăng nhập", MessageBoxButton.OK, MessageBoxImage.Error);
                txtPassword.Clear();
                txtPassword.Focus();
            }
        }


        private async void OpenHomeWithLoading(User loggedInUser)
        {
            if (_isOpeningHome)
                return;

            _isOpeningHome = true;
            IsEnabled = false;

            var splash = new StartupSplashWindow("Đang đăng nhập...")
            {
                Owner = this
            };

            try
            {
                splash.Show();
                await Dispatcher.Yield(DispatcherPriority.Background);

                AppSession.CurrentUser = loggedInUser;

                splash.SetStatus("Đang tải thông tin người dùng và giao diện...");
                await Dispatcher.Yield(DispatcherPriority.Background);

                var home = new HomeWindow(loggedInUser);

                splash.SetStatus("Đăng nhập thành công. Đang mở phần mềm...");
                Application.Current.MainWindow = home;
                home.Show();

                splash.Close();
                Close();
            }
            catch (Exception ex)
            {
                splash.Close();
                AppSession.Clear();
                IsEnabled = true;
                _isOpeningHome = false;

                MessageBox.Show(
                    "Không thể mở giao diện chính!\n\nChi tiết: " + ex.Message,
                    "Lỗi khởi động",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void LoadRememberedCredentials()
        {
            try
            {
                if (File.Exists(RememberFilePath))
                {
                    // Backward compatible:
                    // - Old format: only username (single line)
                    // - New format: username + password (2 lines)
                    var lines = File.ReadAllLines(RememberFilePath);

                    var rememberedUsername = lines.Length > 0 ? (lines[0] ?? string.Empty).Trim() : string.Empty;
                    var rememberedPassword = lines.Length > 1 ? (lines[1] ?? string.Empty) : string.Empty;

                    if (!string.IsNullOrWhiteSpace(rememberedUsername))
                        txtUsername.Text = rememberedUsername;

                    if (!string.IsNullOrEmpty(rememberedPassword))
                        txtPassword.Password = rememberedPassword;

                    if (!string.IsNullOrWhiteSpace(rememberedUsername))
                        chkRemember.IsChecked = true;
                }
            }
            catch
            {
                // Không chặn đăng nhập nếu lỗi IO
            }
        }

        private void SaveRememberedCredentials(string username, string password)
        {
            try
            {
                var dir = Path.GetDirectoryName(RememberFilePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                // New format: username on line 1, password on line 2
                File.WriteAllLines(RememberFilePath, new[]
                {
                    username ?? string.Empty,
                    password ?? string.Empty
                });
            }
            catch
            {
                // bỏ qua
            }
        }

        private void ClearRememberedCredentials()
        {
            try
            {
                if (File.Exists(RememberFilePath))
                    File.Delete(RememberFilePath);
            }
            catch
            {
                // bỏ qua
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}