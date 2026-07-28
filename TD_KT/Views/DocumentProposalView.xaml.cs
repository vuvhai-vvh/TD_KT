using System.Windows.Controls;
using TD_KT.ViewModels;

namespace TD_KT.Views
{
    public partial class DocumentProposalView : UserControl
    {
        public DocumentProposalView()
        {
            InitializeComponent();
            DataContext = new DocumentProposalViewModel();
            UnitProposalTab.DataContext = new UnitDocumentProposalViewModel();
        }
    }
}
