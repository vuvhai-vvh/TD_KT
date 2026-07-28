using System.Windows;
using TD_KT.Views;

namespace TD_KT.Services
{
    public class HometownPickerService : IHometownPickerService
    {
        public string PickHometown(Window owner)
        {
            var dialog = new HometownDialog
            {
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            if (dialog.ShowDialog() == true)
                return dialog.SelectedHometown;

            return null;
        }
    }
}
