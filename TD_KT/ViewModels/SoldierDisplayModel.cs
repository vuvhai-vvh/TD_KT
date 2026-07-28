using System;

namespace TD_KT.ViewModels
{
    public class SoldierDisplayModel
    {
        public int Id { get; set; }
        public int Stt { get; set; }

        // ==== FK bám DB ====
        public int? OrgUnitId { get; set; }
        public int? OrgUnitParentId { get; set; }
        public int? PositionId { get; set; }
        public int? RankId { get; set; }

        // ==== Text hiển thị ====
        public string FullName { get; set; } = "";
        public string SubjectGroup { get; set; } = "";
        public string OrgUnit { get; set; } = "";
        public string Position { get; set; } = "";
        public string Rank { get; set; } = "";

        // ==== Ngày tháng bám DB ====
        public DateTime? EnlistmentDate { get; set; }

        // Hiển thị năm phục vụ (nếu code đang dùng)
        public int YearsOfService { get; set; }

        // Hiển thị dạng "x năm y tháng" theo yêu cầu (không map ngược vào DB)
        public string ServiceDurationText
        {
            get
            {
                if (!EnlistmentDate.HasValue) return "";
                var start = EnlistmentDate.Value.Date;
                var end = DateTime.Today;
                if (end < start) return "0 năm 0 tháng";

                var totalMonths = (end.Year - start.Year) * 12 + (end.Month - start.Month);
                if (end.Day < start.Day) totalMonths--; // chưa đủ 1 tháng
                if (totalMonths < 0) totalMonths = 0;

                var years = totalMonths / 12;
                var months = totalMonths % 12;
                return $"{years} năm {months} tháng";
            }
        }

        // Nếu bạn có cột năm sinh / quê quán ở grid
        public int? BirthYear { get; set; }
        public string Hometown { get; set; } = "";
        public string CitizenId { get; set; } = "";

        // tiện hiển thị dạng chuỗi nếu cần (không map ngược vào DB)
        public string EnlistmentDateText => EnlistmentDate?.ToString("dd/MM/yyyy") ?? "";

        // Giữ tương thích với Binding trong AdminView (cột "Ngày nhập ngũ/công tác")
        // Không map ngược vào DB.
        public string EnlistmentDateDisplay => EnlistmentDateText;

        public string RankName => Rank ?? "";
        public string PositionName => Position ?? "";
        public string OrgUnitName => OrgUnit ?? "";
    }
}
