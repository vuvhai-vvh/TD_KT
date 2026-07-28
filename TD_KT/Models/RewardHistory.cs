namespace TD_KT.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("RewardHistory")]
    public partial class RewardHistory
    {
        public int Id { get; set; }

        public int? SoldierId { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [StringLength(50)]
        public string Rank { get; set; }

        [StringLength(200)]
        public string PositionUnit { get; set; }

        public int RewardYear { get; set; }

        [StringLength(20)]
        public string RecipientType { get; set; }

        public int? OrgUnitId { get; set; }

        [StringLength(200)]
        public string RecipientName { get; set; }

        public int? RewardContentId { get; set; }

        public int? RewardFormId { get; set; }

        public int IssuingLevelId { get; set; }

        [StringLength(100)]
        public string DecisionNumber { get; set; }

        public int? DecisionId { get; set; }

        public int? DecisionDetailId { get; set; }

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
