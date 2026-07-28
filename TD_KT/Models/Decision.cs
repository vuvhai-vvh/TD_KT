namespace TD_KT.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class Decision
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Decision()
        {
            DecisionAttachments = new HashSet<DecisionAttachment>();
            DecisionDetails = new HashSet<DecisionDetail>();
            RewardHistories = new HashSet<RewardHistory>();
            RewardReportSnapshots = new HashSet<RewardReportSnapshot>();
        }

        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string DecisionNumber { get; set; }

        [Required]
        [StringLength(1000)]
        public string DecisionContent { get; set; }

        public int IssuingLevelId { get; set; }

        public int? TotalRows { get; set; }

        [StringLength(1000)]
        public string Note { get; set; }

        [StringLength(200)]
        public string Signer { get; set; }

        [Column(TypeName = "date")]
        public DateTime? SignedDate { get; set; }

        public DateTime CreatedAt { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<DecisionAttachment> DecisionAttachments { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<DecisionDetail> DecisionDetails { get; set; }

        public virtual IssuingLevel IssuingLevel { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<RewardHistory> RewardHistories { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<RewardReportSnapshot> RewardReportSnapshots { get; set; }
    }
}
