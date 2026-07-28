using System.Windows;
using TD_KT.ViewModels;

namespace TD_KT.Views
{
    public partial class RewardFormDialog : Window
    {
        public RewardFormDialog()
        {
            InitializeComponent();
            DataContext = new RewardFormViewModel();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}