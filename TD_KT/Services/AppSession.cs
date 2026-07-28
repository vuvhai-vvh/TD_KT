using TD_KT.Models;

namespace TD_KT.Services
{
    /// <summary>
    /// Lưu thông tin phiên đăng nhập hiện tại của ứng dụng.
    /// </summary>
    public static class AppSession
    {
        public static User CurrentUser { get; set; }

        public static bool IsAdmin
        {
            get
            {
                // Role trong DB: User.Role (nvarchar(50)).
                return CurrentUser != null &&
                       !string.IsNullOrWhiteSpace(CurrentUser.Role) &&
                       CurrentUser.Role.Trim().ToLower() == "admin";
            }
        }

        public static void Clear()
        {
            CurrentUser = null;
        }
    }
}
