namespace TD_KT.Services
{
    public interface IMessageService
    {
        void Info(string message, string title = "Thông báo");
        void Warning(string message, string title = "Thông báo");
        void Error(string message, string title = "Lỗi");
        bool Confirm(string message, string title = "Xác nhận");
    }
}
