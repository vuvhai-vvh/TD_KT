using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using TD_KT.Data;
using TD_KT.Views;
using TD_KT.Models;
using TD_KT.Services;

namespace TD_KT
{
    public partial class HomeWindow : Window
    {
        private HomeView _homeView;
        private WeeklyView _weeklyView;
        private DocumentProposalView _documentProposalView;
        private DecisionView _decisionView;
        private ReportStatisticView _reportStatisticView;
        private AdminView _adminView;

        private readonly User _currentUser;

        private const double CollapsedWidth = 80;
        private const double ExpandedWidth = 280;

        public HomeWindow(User currentUser)
        {
            InitializeComponent();

            _currentUser = currentUser;

            ApplyPermissions();
            UpdateLogoutDisplay();

            // Mặc định mở Trang chủ
            Navigate(GetHomeView(), "Trang chủ");
        }

        private HomeView GetHomeView() => _homeView ??= new HomeView();
        private WeeklyView GetWeeklyView() => _weeklyView ??= new WeeklyView();
        private DocumentProposalView GetDocumentProposalView() => _documentProposalView ??= new DocumentProposalView();
        private DecisionView GetDecisionView() => _decisionView ??= new DecisionView();
        private ReportStatisticView GetReportStatisticView() => _reportStatisticView ??= new ReportStatisticView();
        private AdminView GetAdminView() => _adminView ??= new AdminView();

        private void Navigate(UserControl view, string pageTitle)
        {
            MainContent.Content = view;
            txtPageTitle.Text = pageTitle;
        }

        private void ApplyPermissions()
        {
            // Quyết định khen thưởng và Báo cáo - Thống kê là chức năng dùng chung.
            // Mọi tài khoản đều được nhìn thấy và thao tác hai phân hệ này.
            btnDecision.Visibility = Visibility.Visible;
            btnReportStatistic.Visibility = Visibility.Visible;

            // Quyền hiển thị từng tab bên trong Quản trị hệ thống vẫn được xử lý tại AdminView.
            btnAdmin.Visibility = Visibility.Visible;
        }

        private void UpdateLogoutDisplay()
        {
            var username = (_currentUser?.Username ?? string.Empty).Trim();
            var logoutLabel = string.IsNullOrWhiteSpace(username)
                ? "Đăng xuất"
                : $"Đăng xuất ({username})";

            btnLogout.ToolTip = logoutLabel;
            btnLogout.ApplyTemplate();

            var logoutText = FindLogoutText();
            if (logoutText != null)
                logoutText.Text = logoutLabel;
        }

        // Kéo cửa sổ
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        // Sidebar Expand/Collapse
        private void Sidebar_MouseEnter(object sender, MouseEventArgs e)
        {
            ExpandSidebar();
        }

        private void Sidebar_MouseLeave(object sender, MouseEventArgs e)
        {
            CollapseSidebar();
        }

        private void ExpandSidebar()
        {
            // Animation mở rộng sidebar
            var widthAnimation = new DoubleAnimation
            {
                To = ExpandedWidth,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            SidebarBorder.BeginAnimation(WidthProperty, widthAnimation);

            // Hiện text
            txtTitle.Visibility = Visibility.Visible;
            txtSubtitle.Visibility = Visibility.Visible;
            txtMenu0.Visibility = Visibility.Visible;
            txtMenu1.Visibility = Visibility.Visible;
            txtMenu2.Visibility = Visibility.Visible;
            txtMenu3.Visibility = Visibility.Visible;
            txtMenu4.Visibility = Visibility.Visible;
            txtMenu5.Visibility = Visibility.Visible;

            // Hiện text logout (tìm trong template)
            var logoutText = FindLogoutText();
            if (logoutText != null)
                logoutText.Visibility = Visibility.Visible;
        }

        private void CollapseSidebar()
        {
            // Animation thu nhỏ sidebar
            var widthAnimation = new DoubleAnimation
            {
                To = CollapsedWidth,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            SidebarBorder.BeginAnimation(WidthProperty, widthAnimation);

            // Ẩn text
            txtTitle.Visibility = Visibility.Collapsed;
            txtSubtitle.Visibility = Visibility.Collapsed;
            txtMenu0.Visibility = Visibility.Collapsed;
            txtMenu1.Visibility = Visibility.Collapsed;
            txtMenu2.Visibility = Visibility.Collapsed;
            txtMenu3.Visibility = Visibility.Collapsed;
            txtMenu4.Visibility = Visibility.Collapsed;
            txtMenu5.Visibility = Visibility.Collapsed;

            // Ẩn text logout
            var logoutText = FindLogoutText();
            if (logoutText != null)
                logoutText.Visibility = Visibility.Collapsed;
        }

        private TextBlock FindLogoutText()
        {
            // Tìm TextBlock "Đăng xuất" trong Button template
            var border = VisualTreeHelper.GetChild(btnLogout, 0) as Border;
            if (border != null)
            {
                var stackPanel = border.Child as StackPanel;
                if (stackPanel != null && stackPanel.Children.Count > 1)
                {
                    return stackPanel.Children[1] as TextBlock;
                }
            }
            return null;
        }

        // Menu Click Events
        private void btnHome_Click(object sender, RoutedEventArgs e)
            => Navigate(GetHomeView(), "Trang chủ");

        private void btnWeekly_Click(object sender, RoutedEventArgs e)
            => Navigate(GetWeeklyView(), "Chấm điểm thi đua");

        private void btnDocumentProposal_Click(object sender, RoutedEventArgs e)
            => Navigate(GetDocumentProposalView(), "Văn bản đề nghị khen thưởng");

        private void btnDecision_Click(object sender, RoutedEventArgs e)
            => Navigate(GetDecisionView(), "Quyết định khen thưởng");

        private void btnReportStatistic_Click(object sender, RoutedEventArgs e)
            => Navigate(GetReportStatisticView(), "Báo cáo - Thống kê");

        private void btnAdmin_Click(object sender, RoutedEventArgs e)
        {
            Navigate(GetAdminView(), "Quản trị hệ thống");
        }

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Bạn có chắc chắn muốn đăng xuất?",
                "Xác nhận",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                AppSession.Clear();
                var login = new MainWindow();
                Application.Current.MainWindow = login;
                login.Show();
                Close();
            }
        }

        // Window Control Events
        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Bạn có chắc chắn muốn thoát ứng dụng?",
                "Xác nhận",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }
    }
}