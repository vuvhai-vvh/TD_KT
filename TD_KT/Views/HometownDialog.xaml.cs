using System.Windows;
using TD_KT.ViewModels;

namespace TD_KT.Views
{
    public partial class HometownDialog : Window
    {
        private readonly HometownViewModel _vm;

        // ✅ để code nơi khác gọi hometownDialog.SelectedHometown
        public string SelectedHometown { get; private set; }

        public HometownDialog()
        {
            InitializeComponent();

            _vm = new HometownViewModel();
            DataContext = _vm;
        }

        // ====== ADD/DELETE TỈNH/TP ======
        private void BtnAddProvince_Click(object sender, RoutedEventArgs e)
        {
            _vm.AddProvince(txtProvinceName.Text);
            txtProvinceName.Clear();
        }

        private void BtnDeleteProvince_Click(object sender, RoutedEventArgs e)
        {
            _vm.DeleteProvince();
        }

        // ====== ADD/DELETE XÃ/PHƯỜNG/ĐẶC KHU ======
        private void BtnAddCommune_Click(object sender, RoutedEventArgs e)
        {
            _vm.AddCommune(txtCommuneName.Text);
            txtCommuneName.Clear();
        }

        private void BtnDeleteCommune_Click(object sender, RoutedEventArgs e)
        {
            _vm.DeleteCommune();
        }

        // ====== OK / CANCEL ======
        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            _vm.ConfirmSelection();
            SelectedHometown = _vm.SelectedHometown;
            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void BtnClose_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void BtnRefresh_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // refresh lại danh sách cấp Tỉnh/TP
            _vm.LoadProvinces();
        }
    }
}
