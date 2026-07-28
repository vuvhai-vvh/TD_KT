namespace TD_KT.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class DecisionAttachment
    {
        public int Id { get; set; }

        public int DecisionId { get; set; }

        public int? DecisionDetailId { get; set; }

        [Required]
        [StringLength(260)]
        public string FileName { get; set; }

        [StringLength(20)]
        public string FileType { get; set; }

        public long? FileSize { get; set; }

        [Required]
        [StringLength(500)]
        public string FilePath { get; set; }

        /// <summary>
        /// Nội dung file lưu trực tiếp trong SQL Server.
        /// FilePath vẫn được giữ để tương thích với các bản ghi cũ.
        /// </summary>
        public byte[] FileData { get; set; }

        public DateTime UploadDate { get; set; }

        [StringLength(100)]
        public string UploadedBy { get; set; }

        public virtual Decision Decision { get; set; }
    }
}
