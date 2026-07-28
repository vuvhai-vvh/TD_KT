namespace TD_KT.ViewModels
{
    public class UserDisplayModel
    {
        // ==== Các property mà code đang gọi ====
        public int Id { get; set; }          // code đang gọi Id
        public int Stt { get; set; }         // code đang gọi Stt
        public string OrgUnit { get; set; }  // code đang gọi OrgUnit
        public string Position { get; set; } // code đang gọi Position
        public string Status => IsActive ? "Hoạt động" : "Ngừng";


        // ==== Các field tài khoản ====
        public string Username { get; set; }
        public string Password { get; set; }
        public string DisplayName { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
    }
}
