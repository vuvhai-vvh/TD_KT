using System.Windows;
using TD_KT.ViewModels;

namespace TD_KT.Views
{
    public partial class OrgUnitDialog : Window
    {
        public OrgUnitDialog()
        {
            InitializeComponent();
            DataContext = new OrgUnitViewModel();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}