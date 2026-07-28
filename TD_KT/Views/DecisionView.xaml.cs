using System.Windows.Controls;
using TD_KT.ViewModels;

namespace TD_KT.Views
{
    public partial class DecisionView : UserControl
    {
        public DecisionView()
        {
            InitializeComponent();
            DataContext = new DecisionViewModel();
        }
    }
}