using System;
using System.Globalization;

namespace TD_KT.ViewModels
{
    /// <summary>
    /// Display model cho dòng khen thưởng trong Quyết định.
    /// Lưu ý: View/Code-behind hiện đang dùng tên property tiếng Việt (HoTen, CapBac, ...)
    /// trong khi một số chỗ khác (dialog) lại dùng tên tiếng Anh (FullName, Rank, ...).
    /// => File này cung cấp CẢ HAI bộ property và map về cùng dữ liệu để tránh lỗi.
    /// </summary>
    public class DecisionDetailDisplay
    {
        // ====== Khóa / liên kết ======
        public int Id { get; set; }
        public int DecisionId { get; set; }

        // ====== JOIN đồng bộ (nullable để không phá dữ liệu cũ) ======
        // Đồng bộ quân nhân
        public int? SoldierId { get; set; }

        // Đồng bộ danh mục "Nội dung khen thưởng"
        public int? RewardContentId { get; set; }

        // Đồng bộ danh mục "Hình thức khen thưởng"
        public int? RewardFormId { get; set; }

        // ====== Dữ liệu chuẩn theo DB (DecisionDetails) ======
        public int OrderNo { get; set; }
        public string FullName { get; set; }
        public string Rank { get; set; }
        public string PositionUnit { get; set; }
        public int? BirthYear { get; set; }

        // DB đang lưu NVARCHAR(50) (thường dd/MM/yyyy)
        private string _enlistmentDateText;
        public string EnlistmentDateText
        {
            get => _enlistmentDateText;
            set => _enlistmentDateText = value;
        }

        /// <summary>
        /// Dùng thuận tiện cho DatePicker.
        /// Get: parse từ EnlistmentDateText.
        /// Set: lưu lại EnlistmentDateText theo dd/MM/yyyy.
        /// </summary>
        public DateTime? EnlistmentDate
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_enlistmentDateText)) return null;

                // Ưu tiên dd/MM/yyyy
                if (DateTime.TryParseExact(_enlistmentDateText.Trim(), "dd/MM/yyyy",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var d1))
                    return d1;

                // Fallback TryParse
                if (DateTime.TryParse(_enlistmentDateText, out var d2))
                    return d2;

                return null;
            }
            set
            {
                _enlistmentDateText = value.HasValue ? value.Value.ToString("dd/MM/yyyy") : null;
            }
        }

        public string Hometown { get; set; }
        /// <summary>
        /// ND khen thưởng (ví dụ: Chiến sĩ thi đua, ...)
        /// </summary>
        public string RewardContent { get; set; }

        /// <summary>
        /// Hình thức khen thưởng (ví dụ: Giấy khen, Bằng khen, ...)
        /// </summary>
        public string RewardForm { get; set; }
        public string Circumstance { get; set; }
        public string Note { get; set; }

        public DecisionDetailDisplay()
        {
            FullName = "";
            Rank = "";
            PositionUnit = "";
            Hometown = "";
            RewardContent = "";
            RewardForm = "";
            Circumstance = "";
            Note = "";
            _enlistmentDateText = "";
        }

        // ====== Alias tiếng Việt (đúng theo View/Code-behind hiện tại) ======
        // STT
        public int STT
        {
            get => OrderNo;
            set => OrderNo = value;
        }

        public string HoTen
        {
            get => FullName;
            set => FullName = value;
        }

        public string CapBac
        {
            get => Rank;
            set => Rank = value;
        }

        public string ChucVuDonVi
        {
            get => PositionUnit;
            set => PositionUnit = value;
        }

        public int? NamSinh
        {
            get => BirthYear;
            set => BirthYear = value;
        }

        /// <summary>
        /// Chuỗi ngày nhập ngũ (dd/MM/yyyy) dùng cho hiển thị/ghi DB.
        /// </summary>
        public string NhapNgu
        {
            get => EnlistmentDateText;
            set => EnlistmentDateText = value;
        }

        public string QueQuan
        {
            get => Hometown;
            set => Hometown = value;
        }

        // ND khen thưởng
        public string NoiDungKT
        {
            get => RewardContent;
            set => RewardContent = value;
        }

        public string HinhThucKT
        {
            get => RewardForm;
            set => RewardForm = value;
        }

        public string HoanCanh
        {
            get => Circumstance;
            set => Circumstance = value;
        }

        public string GhiChu
        {
            get => Note;
            set => Note = value;
        }
    }
}
