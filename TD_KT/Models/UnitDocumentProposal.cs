namespace TD_KT.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public partial class UnitDocumentProposal
    {
        public int Id { get; set; }

        [Required]
        [StringLength(500)]
        public string Title { get; set; }

        public int? OrgUnitId { get; set; }

        [Required]
        [StringLength(255)]
        public string FileName { get; set; }

        [StringLength(20)]
        public string FileType { get; set; }

        [StringLength(50)]
        public string FileSize { get; set; }

        [Required]
        [StringLength(500)]
        public string FilePath { get; set; }

        /// <summary>
        /// Nội dung file lưu trực tiếp trong SQL Server.
        /// FilePath vẫn được giữ để đọc các hồ sơ cũ chưa chuyển vào CSDL.
        /// </summary>
        public byte[] FileData { get; set; }

        public DateTime UploadDate { get; set; }

        [StringLength(100)]
        public string UploadedBy { get; set; }

        public int? Year { get; set; }

        public int? Month { get; set; }

        [NotMapped]
        public string OrgUnitName { get; set; }

        [NotMapped]
        public bool HasFileData { get; set; }
    }
}
