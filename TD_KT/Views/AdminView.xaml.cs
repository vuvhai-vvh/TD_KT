using System.Windows;
using System.Windows.Controls;
using TD_KT.Models;
using TD_KT.ViewModels;

namespace TD_KT.Views
{
    public partial class AdminView : UserControl
    {
        private AdminViewModel _viewModel;

        public AdminView()
        {
            InitializeComponent();

            _viewModel = new AdminViewModel();
            DataContext = _viewModel;

            ApplyAdminTabVisibility();

            // TAB 1 vẫn dùng code-behind cho PasswordBox/CRUD.
            // Các TAB khác (Phân quyền / Hồ sơ quân nhân / Danh mục) đã chuyển sang MVVM (binding/command) nên KHÔNG setup ItemsSource ở đây.

            // Combo trạng thái: để đơn giản giữ nguyên setup bằng code-behind
            cboAccStatus.Items.Add("Hoạt động");
            cboAccStatus.Items.Add("Khóa");
            cboAccStatus.SelectedIndex = 0;

            // Khi chọn 1 dòng trong danh sách tài khoản: đổ dữ liệu sang form nhập liệu để sửa nhanh
            dgAccounts.SelectionChanged += DgAccounts_SelectionChanged;
        }
        private void ApplyAdminTabVisibility()
        {
            var currentUser = Services.AppSession.CurrentUser;
            if (currentUser == null)
            {
                tabAccounts.Visibility = Visibility.Collapsed;
                tabPermissions.Visibility = Visibility.Collapsed;
                SelectFirstVisibleAdminTab();
                return;
            }

            var permissionData = new Data.UserPermissionData(new Services.ConnectionStringProvider("Database"));
            var flags = permissionData.GetUserPermissionFlags(currentUser.Id);

            tabAccounts.Visibility = flags.CanAccessAccountTab ? Visibility.Visible : Visibility.Collapsed;
            tabPermissions.Visibility = flags.CanAccessPermissionTab ? Visibility.Visible : Visibility.Collapsed;

            // TabControl có thể vẫn giữ nội dung của tab mặc định dù tab đó đã bị Collapsed.
            // Chỉ chọn tab đang được phép hiển thị, không thay đổi giao diện hay nghiệp vụ của các tab.
            SelectFirstVisibleAdminTab();
        }

        private void SelectFirstVisibleAdminTab()
        {
            var selectedTab = adminTabControl.SelectedItem as TabItem;
            if (selectedTab != null &&
                selectedTab.Visibility == Visibility.Visible &&
                selectedTab.IsEnabled)
            {
                return;
            }

            foreach (var item in adminTabControl.Items)
            {
                var tab = item as TabItem;
                if (tab != null && tab.Visibility == Visibility.Visible && tab.IsEnabled)
                {
                    adminTabControl.SelectedItem = tab;
                    tab.IsSelected = true;
                    return;
                }
            }

            adminTabControl.SelectedItem = null;
        }

        #region === TAB 1: TÀI KHOẢN ===

        private void BtnReloadAccounts_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.LoadAccounts();
            _viewModel.Permissions.LoadData();
        }

        private void BtnAddAccount_Click(object sender, RoutedEventArgs e)
        {
            var orgUnit = cboAccOrgUnit.SelectedItem as OrgUnit;
            var position = cboAccPosition.SelectedItem as Position;

            _viewModel.AddAccount(
                txtAccUserName.Text,
                txtAccPassword.Password,
                txtAccDisplayName.Text,
                orgUnit?.Id ?? 0,
                position?.Id ?? 0,
                cboAccRole.SelectedItem?.ToString(),
                cboAccStatus.SelectedItem?.ToString() == "Hoạt động"
            );
            ClearAccountForm();
            _viewModel.Permissions.LoadData();
        }

        private void BtnEditAccount_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedAccount == null)
            {
                _viewModel.EditAccount(
                    0,
                    "",
                    "",
                    "",
                    0,
                    0,
                    "",
                    false
                );
                return;
            }

            var orgUnit = cboAccOrgUnit.SelectedItem as OrgUnit;
            var position = cboAccPosition.SelectedItem as Position;

            _viewModel.EditAccount(
                _viewModel.SelectedAccount.Id,
                txtAccUserName.Text,
                txtAccPassword.Password,
                txtAccDisplayName.Text,
                orgUnit?.Id ?? 0,
                position?.Id ?? 0,
                cboAccRole.SelectedItem?.ToString(),
                cboAccStatus.SelectedItem?.ToString() == "Hoạt động"
            );
            ClearAccountForm();
            _viewModel.Permissions.LoadData();
        }


        private void BtnDeleteAccount_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedAccount == null)
            {
                _viewModel.DeleteAccount(0, "");
                return;
            }

            _viewModel.DeleteAccount(_viewModel.SelectedAccount.Id, _viewModel.SelectedAccount.Username);
            ClearAccountForm();
            _viewModel.Permissions.LoadData();
        }


        private void ClearAccountForm()
        {
            txtAccUserName.Text = "";
            txtAccDisplayName.Text = "";
            txtAccPassword.Password = "";
            cboAccRole.SelectedIndex = -1;
            cboAccOrgUnit.SelectedIndex = -1;
            cboAccPosition.SelectedIndex = -1;
            cboAccStatus.SelectedIndex = 0;
        }

        private void DgAccounts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var acc = _viewModel.SelectedAccount;
            if (acc == null)
                return;

            txtAccUserName.Text = acc.Username ?? string.Empty;
            txtAccDisplayName.Text = acc.DisplayName ?? string.Empty;
            txtAccPassword.Password = ""; // không tự đổ mật khẩu

            // Vai trò
            cboAccRole.SelectedItem = acc.Role;

            // Trạng thái
            cboAccStatus.SelectedItem = acc.IsActive ? "Hoạt động" : "Khóa";

            // OrgUnit / Position: tìm theo Name đang hiển thị trên grid
            if (!string.IsNullOrWhiteSpace(acc.OrgUnit))
            {
                foreach (var item in cboAccOrgUnit.Items)
                {
                    if (item is OrgUnit ou && ou.Name == acc.OrgUnit)
                    {
                        cboAccOrgUnit.SelectedItem = item;
                        break;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(acc.Position))
            {
                foreach (var item in cboAccPosition.Items)
                {
                    if (item is Position p && p.Name == acc.Position)
                    {
                        cboAccPosition.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        #endregion
    }
}