
using System.Windows.Controls;
using System.Windows;
using TD_KT.ViewModels;

namespace TD_KT.Views
{
    public partial class HomeView : UserControl
    {
        private readonly IntroConfigViewModel _vm;

        public HomeView()
        {
            InitializeComponent();
            _vm = new IntroConfigViewModel();
            DataContext = _vm;          
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            _vm.LoadData();
        }
    }
}
