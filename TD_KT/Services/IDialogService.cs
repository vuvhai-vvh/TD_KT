using System;

namespace TD_KT.Services
{
    public interface IDialogService
    {
        bool? ShowDialog<TDialog>() where TDialog : class, new();
        bool? ShowDialog(object dialog);
    }
}
