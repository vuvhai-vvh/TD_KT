using System.Windows;

namespace TD_KT.Services
{
    public class DialogService : IDialogService
    {
        public bool? ShowDialog<TDialog>() where TDialog : class, new()
        {
            var dlg = new TDialog();
            return ShowDialog(dlg);
        }

        public bool? ShowDialog(object dialog)
        {
            if (dialog is Window wnd)
            {
                return wnd.ShowDialog();
            }

            return null;
        }
    }
}
