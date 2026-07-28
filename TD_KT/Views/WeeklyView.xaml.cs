using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using TD_KT.Data;
using TD_KT.Services;
using TD_KT.ViewModels;

namespace TD_KT.Views
{
    public partial class WeeklyView : UserControl
    {
        private readonly WeeklyViewModel _vm;
        private ViolationEntryWindow violationWindow;
        private UserPermissionFlags _permissionFlags;

        public WeeklyView()
        {
            InitializeComponent();
            _vm = new WeeklyViewModel();
            DataContext = _vm;

            SetupDataGridBindings();
            ApplyPermissions();
            InitScoreFilters();
            RefreshScoreTab();
        }
        private void ApplyPermissions()
        {
            if (btnFinalizeWeek == null)
                return;

            _permissionFlags = LoadPermissionFlags();

            // Nút xác nhận điểm hiển thị theo phân quyền của tài khoản đăng nhập,
            // không phụ thuộc vai trò Admin/User.
            btnFinalizeWeek.Visibility = _permissionFlags.CanFinalizeScore
                ? Visibility.Visible
                : Visibility.Collapsed;

            // Khóa tuần chỉ dành cho Admin có quyền tương ứng.
            if (btnLockWeek != null)
            {
                btnLockWeek.Visibility = AppSession.IsAdmin && _permissionFlags.CanLockWeek
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
           
            UpdateViolationActionButtons();
        }

        private static UserPermissionFlags LoadPermissionFlags()
        {
            if (AppSession.CurrentUser == null)
                return new UserPermissionFlags();

            var permissionData = new UserPermissionData(new ConnectionStringProvider("Database"));
            return permissionData.GetUserPermissionFlags(AppSession.CurrentUser.Id);
        }

        private void UpdateViolationActionButtons()
        {
            if (btnAddViolation == null || btnEditViolation == null || btnDeleteViolation == null)
                return;

            var visibility = Visibility.Collapsed;
            var areaType = _vm.SelectedAreaTypeViolation;

            if (AppSession.IsAdmin)
            {
                visibility = areaType.HasValue && areaType.Value > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            else if (areaType.HasValue)
            {
                switch (areaType.Value)
                {
                    case 1:
                        if (_permissionFlags.EditTM) visibility = Visibility.Visible;
                        break;
                    case 2:
                        if (_permissionFlags.EditCT) visibility = Visibility.Visible;
                        break;
                    case 3:
                        if (_permissionFlags.EditHCKT) visibility = Visibility.Visible;
                        break;
                    case 4:
                        if (_permissionFlags.EditNV) visibility = Visibility.Visible;
                        break;
                }
            }

            btnAddViolation.Visibility = visibility;
            btnEditViolation.Visibility = visibility;
            btnDeleteViolation.Visibility = visibility;
        }

        private void SetupDataGridBindings()
        {
            if (dgUnitScores != null && dgUnitScores.Columns.Count >= 9)
            {
                ((DataGridTextColumn)dgUnitScores.Columns[0]).Binding = new Binding(nameof(WeeklyUnitScoreDisplayModel.OrgUnitName));
                ((DataGridTextColumn)dgUnitScores.Columns[1]).Binding = new Binding(nameof(WeeklyUnitScoreDisplayModel.TotalViolations));

                ((DataGridTextColumn)dgUnitScores.Columns[2]).Binding = new Binding(nameof(WeeklyUnitScoreDisplayModel.ScoreArea1)) { StringFormat = "F3" };
                ((DataGridTextColumn)dgUnitScores.Columns[3]).Binding = new Binding(nameof(WeeklyUnitScoreDisplayModel.ScoreArea2)) { StringFormat = "F3" };
                ((DataGridTextColumn)dgUnitScores.Columns[4]).Binding = new Binding(nameof(WeeklyUnitScoreDisplayModel.ScoreArea3)) { StringFormat = "F3" };
                ((DataGridTextColumn)dgUnitScores.Columns[5]).Binding = new Binding(nameof(WeeklyUnitScoreDisplayModel.ScoreArea4)) { StringFormat = "F3" };

                ((DataGridTextColumn)dgUnitScores.Columns[6]).Binding = new Binding(nameof(WeeklyUnitScoreDisplayModel.BonusPoints)) { StringFormat = "F3" };
                ((DataGridTextColumn)dgUnitScores.Columns[7]).Binding = new Binding(nameof(WeeklyUnitScoreDisplayModel.AverageScore)) { StringFormat = "F3" };
                ((DataGridTextColumn)dgUnitScores.Columns[8]).Binding = new Binding(nameof(WeeklyUnitScoreDisplayModel.Ranking));
            }

            if (dgUnitScores != null)
                dgUnitScores.ItemsSource = _vm.UnitScores;
        }

        private void InitScoreFilters()
        {
            cboYear3.ItemsSource = _vm.Years;
            cboMonth3.ItemsSource = _vm.Months;
            cboWeek3.ItemsSource = _vm.ScoreWeeks;

            var now = DateTime.Now;
            cboYear3.SelectedItem = _vm.Years.Contains(now.Year) ? (object)now.Year : _vm.Years.LastOrDefault();
            cboMonth3.SelectedItem = now.Month;

            RebuildScoreWeeksFromFilter();

            cboYear3.SelectionChanged += (s, e) => { RebuildScoreWeeksFromFilter(); RefreshScoreTab(); };
            cboMonth3.SelectionChanged += (s, e) => { RebuildScoreWeeksFromFilter(); RefreshScoreTab(); };
            cboWeek3.SelectionChanged += (s, e) => RefreshScoreTab();
        }

        private void RebuildScoreWeeksFromFilter()
        {
            if (!(cboYear3.SelectedItem is int y) || !(cboMonth3.SelectedItem is int m))
                return;

            _vm.BuildScoreWeeks(y, m);

            if (_vm.ScoreWeeks.Count == 0)
            {
                cboWeek3.SelectedIndex = -1;
                return;
            }

            // Mặc định chọn tuần hiện tại (không chọn "Tất cả")
            var today = DateTime.Today;
            var pick = _vm.ScoreWeeks.FirstOrDefault(w => w.WeekNoInMonth > 0 && today >= w.StartDate && today <= w.EndDate);
            cboWeek3.SelectedItem = pick
                ?? _vm.ScoreWeeks.FirstOrDefault(w => w.WeekNoInMonth > 0)
                ?? _vm.ScoreWeeks.FirstOrDefault();
        }

        private (int year, int month, int week)? GetScoreFilter()
        {
            if (!(cboYear3.SelectedItem is int year)) return null;
            if (!(cboMonth3.SelectedItem is int month)) return null;

            var weekOpt = cboWeek3.SelectedItem as WeekCalculator.WeekOption;
            var week = weekOpt != null ? weekOpt.WeekNoInMonth : 1;
            return (year, month, week);
        }

        private void RefreshScoreTab()
        {
            var f = GetScoreFilter();
            if (!f.HasValue) return;

            _vm.LoadUnitScores(f.Value.year, f.Value.month, f.Value.week);
            txtWeekStatus.Text = _vm.WeekStatusText;

            // bảng điểm tự tính -> luôn readonly
            if (dgUnitScores != null)
                dgUnitScores.IsReadOnly = true;
        }

        // TAB 1
        private void BtnArea_Click(object sender, RoutedEventArgs e)
        {
            // reset
            if (btnALL != null) btnALL.IsChecked = false;
            if (btnTM != null) btnTM.IsChecked = false;
            if (btnCT != null) btnCT.IsChecked = false;
            if (btnHCKT != null) btnHCKT.IsChecked = false;
            if (btnNV != null) btnNV.IsChecked = false;

            int? areaType = null;

            if (sender is ToggleButton btn)
            {
                btn.IsChecked = true;

                if (btn.Tag != null && int.TryParse(btn.Tag.ToString(), out var v))
                    areaType = v <= 0 ? (int?)null : v;
            }

            _vm.SelectedAreaTypeViolation = areaType;
            _vm.RefreshViolations();
            UpdateViolationActionButtons();
        }

        private void BtnRefreshViolation_Click(object sender, RoutedEventArgs e) => _vm.RefreshViolations();
        private void BtnAddViolation_Click(object sender, RoutedEventArgs e)
        {
            _vm.BeginAddViolation();
            if (_vm.IsViolationEditing)
                OpenViolationWindow("Thêm vi phạm / biểu dương");
        }

        private void BtnEditViolation_Click(object sender, RoutedEventArgs e)
        {
            _vm.BeginEditViolation();
            if (_vm.IsViolationEditing)
                OpenViolationWindow("Sửa vi phạm / biểu dương");
        }
        private void BtnDeleteViolation_Click(object sender, RoutedEventArgs e) => _vm.DeleteSelectedViolation();
        private void BtnSaveViolation_Click(object sender, RoutedEventArgs e) => _vm.SaveViolation();
        private void BtnCancelViolation_Click(object sender, RoutedEventArgs e) => _vm.CancelViolationEdit();

        // TAB 3
        private void BtnRefreshScore_Click(object sender, RoutedEventArgs e) => RefreshScoreTab();

        private void BtnSaveScore_Click(object sender, RoutedEventArgs e)
        {
            var f = GetScoreFilter();
            if (!f.HasValue) return;

            if (f.Value.week <= 0)
            {
                MessageBox.Show("Vui lòng chọn 1 tuần cụ thể trước khi lưu điểm.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                _vm.SaveUnitScores(f.Value.year, f.Value.month, f.Value.week);
                txtWeekStatus.Text = _vm.WeekStatusText;
                MessageBox.Show("Đã lưu điểm.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnLockWeek_Click(object sender, RoutedEventArgs e)
        {
            var f = GetScoreFilter();
            if (!f.HasValue) return;

            if (f.Value.week <= 0)
            {
                MessageBox.Show("Vui lòng chọn 1 tuần cụ thể trước khi khóa tuần.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_vm.IsWeekLocked)
            {
                MessageBox.Show("Tuần này đã khóa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                "Khóa tuần sẽ chốt hoàn toàn và không thể thêm/sửa/xóa dữ liệu. Bạn có chắc muốn khóa tuần này?",
                "Xác nhận",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                _vm.LockWeek(f.Value.year, f.Value.month, f.Value.week);
                RefreshScoreTab();
                MessageBox.Show("Đã khóa tuần.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenViolationWindow(string title)
        {
            if (violationWindow != null)
            {
                violationWindow.Activate();
                return;
            }

            violationWindow = new ViolationEntryWindow(_vm)
            {
                Owner = Window.GetWindow(this),
                Title = title
            };

            violationWindow.Closed += (_, __) => violationWindow = null;
            violationWindow.ShowDialog();
        }

        private void BtnFinalizeWeek_Click(object sender, RoutedEventArgs e)
        {
            var f = GetScoreFilter();
            if (!f.HasValue) return;

            if (f.Value.week <= 0)
            {
                MessageBox.Show("Vui lòng chọn 1 tuần cụ thể trước khi xác nhận điểm.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_vm.IsFinalized)
            {
                MessageBox.Show("Tuần này đã chốt.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                "Xác nhận chốt điểm tuần này? Sau khi chốt sẽ không sửa được.",
                "Xác nhận",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                _vm.FinalizeScores(f.Value.year, f.Value.month, f.Value.week);
                txtWeekStatus.Text = _vm.WeekStatusText;
                MessageBox.Show("Đã chốt điểm.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
