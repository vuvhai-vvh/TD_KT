using System;

namespace TD_KT.ViewModels
{
    public class DecisionAttachmentDisplay
    {
        public int Id { get; set; }
        public int DecisionId { get; set; }
        public int? DecisionDetailId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public long? FileSize { get; set; }

        /// <summary>
        /// Đường dẫn cũ được giữ để mở và chuyển các file đã tải lên trước phiên bản 1.0.0.3.
        /// File mới không còn được sao chép vào thư mục ứng dụng.
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>Nội dung file dùng khi thêm mới vào SQL Server.</summary>
        public byte[] FileData { get; set; }

        /// <summary>Cho biết bản ghi đang có nội dung file trong cột FileData.</summary>
        public bool HasFileData { get; set; }

        public DateTime UploadDate { get; set; }
        public string UploadedBy { get; set; } = string.Empty;

        public string UploadDateText => UploadDate == DateTime.MinValue
            ? string.Empty
            : UploadDate.ToString("dd/MM/yyyy HH:mm");
    }
}
