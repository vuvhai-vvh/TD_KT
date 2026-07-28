using System.Windows;
using TD_KT.ViewModels;

namespace TD_KT.Views
{
    public partial class PositionDialog : Window
    {
        public PositionDialog()
        {
            InitializeComponent();
            DataContext = new PositionViewModel();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}