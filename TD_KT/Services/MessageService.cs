using System.Windows;

namespace TD_KT.Services
{
    public class MessageService : IMessageService
    {
        public void Info(string message, string title = "Thông báo")
            => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);

        public void Warning(string message, string title = "Thông báo")
            => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);

        public void Error(string message, string title = "Lỗi")
            => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);

        public bool Confirm(string message, string title = "Xác nhận")
            => MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
    }
}
