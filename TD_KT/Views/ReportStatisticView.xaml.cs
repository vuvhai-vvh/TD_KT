using System.Windows.Controls;
using TD_KT.ViewModels;

namespace TD_KT.Views
{
    public partial class ReportStatisticView : UserControl
    {
        public ReportStatisticView()
        {
            InitializeComponent();
            DataContext = new ReportStatisticViewModel();
        }
    }
}