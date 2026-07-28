namespace TD_KT.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("RewardReportSnapshot")]
    public partial class RewardReportSnapshot
    {
        public int Id { get; set; }

        public int ReportYear { get; set; }

        [Required]
        [StringLength(20)]
        public string RecipientType { get; set; }

        public int? SoldierId { get; set; }

        public int? OrgUnitId { get; set; }

        [Required]
        [StringLength(200)]
        public string RecipientName { get; set; }

        public int? RewardContentId { get; set; }

        public int? RewardFormId { get; set; }

        public int IssuingLevelId { get; set; }

        public int DecisionId { get; set; }

        public int DecisionDetailId { get; set; }

        [Required]
        [StringLength(100)]
        public string DecisionNumber { get; set; }

        [Column(TypeName = "date")]
        public DateTime? SignedDate { get; set; }

        [StringLength(200)]
        public string Signer { get; set; }

        public DateTime CreatedAt { get; set; }

        public virtual DecisionDetail DecisionDetail { get; set; }

        public virtual Decision Decision { get; set; }

        public virtual IssuingLevel IssuingLevel { get; set; }

        public virtual OrgUnit OrgUnit { get; set; }

        public virtual RewardForm RewardForm { get; set; }

        public virtual Soldier Soldier { get; set; }
    }
}
