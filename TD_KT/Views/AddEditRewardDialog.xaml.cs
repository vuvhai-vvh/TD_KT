using System;
using System.Windows;
using System.Windows.Input;
using TD_KT.ViewModels;

namespace TD_KT.Views
{
    public partial class AddEditRewardDialog : Window
    {
        private readonly AddEditRewardDialogViewModel _vm;

        public AddEditRewardDialog() : this(null)
        {
        }

        // DecisionView đang gọi new AddEditRewardDialog(dgDecisionDetails.SelectedItem)
        public AddEditRewardDialog(object selectedItem)
        {
            InitializeComponent();

            var edit = selectedItem as DecisionDetailDisplay;
            _vm = new AddEditRewardDialogViewModel(edit);
            _vm.SetOwner(this);
            _vm.RequestClose += OnRequestClose;

            DataContext = _vm;

            // UI-only: kéo cửa sổ
            MouseLeftButtonDown += (s, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                    DragMove();
            };
        }

        private void OnRequestClose(bool? result)
        {
            // Setting DialogResult will close the dialog automatically.
            DialogResult = result == true;
        }

        // ===== Properties để DecisionView lấy dữ liệu (giữ tương thích luồng hiện tại) =====
        public string HoTen => _vm.OutputHoTen;
        public string CapBac => _vm.OutputCapBac;
        public string NamSinh => _vm.OutputNamSinh;
        public string ChucVuDonVi => _vm.OutputChucVuDonVi;
        public string NhapNgu => _vm.OutputNhapNgu;
        public string QueQuan => _vm.OutputQueQuan;
        public string HinhThucKT => _vm.OutputHinhThucKT;
        public string HoanCanh => _vm.OutputHoanCanh;
        public string GhiChu => _vm.OutputGhiChu;

        // Id phục vụ JOIN đồng bộ
        public int? SoldierId => _vm.OutputSoldierId;
        public int? RewardFormId => _vm.OutputRewardFormId;
    }
}
