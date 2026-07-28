namespace TD_KT.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("MedalRecords")]
    public partial class MedalRecord
    {
        public int Id { get; set; }

        [StringLength(100)]
        public string FullName { get; set; }

        [StringLength(50)]
        public string Rank { get; set; }

        [StringLength(100)]
        public string Position { get; set; }

        [StringLength(200)]
        public string OrgUnit { get; set; }

        [StringLength(200)]
        public string TitleName { get; set; }

        [StringLength(100)]
        public string DecisionNumber { get; set; }

        [StringLength(500)]
        public string Note { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}