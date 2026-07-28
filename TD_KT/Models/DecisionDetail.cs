namespace TD_KT.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class DecisionDetail
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public DecisionDetail()
        {
            RewardHistories = new HashSet<RewardHistory>();
            RewardReportSnapshots = new HashSet<RewardReportSnapshot>();
        }

        public int Id { get; set; }

        public int DecisionId { get; set; }

        public int OrderNo { get; set; }

        public int? SoldierId { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [StringLength(50)]
        public string Rank { get; set; }

        [StringLength(300)]
        public string PositionUnit { get; set; }

        public int? BirthYear { get; set; }

        [StringLength(50)]
        public string EnlistmentDate { get; set; }

        [StringLength(500)]
        public string Hometown { get; set; }

        public int? RewardContentId { get; set; }

        public int? RewardFormId { get; set; }

        [StringLength(500)]
        public string Circumstance { get; set; }

        [StringLength(500)]
        public string Note { get; set; }

        public virtual Decision Decision { get; set; }

        public virtual RewardForm RewardForm { get; set; }

        public virtual Soldier Soldier { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<RewardHistory> RewardHistories { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<RewardReportSnapshot> RewardReportSnapshots { get; set; }
    }
}
