namespace TD_KT.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class Soldier
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Soldier()
        {
            DecisionDetails = new HashSet<DecisionDetail>();
            RewardHistories = new HashSet<RewardHistory>();
            RewardReportSnapshots = new HashSet<RewardReportSnapshot>();
            SeniorityAwards = new HashSet<SeniorityAward>();
            WeeklyScores = new HashSet<WeeklyScore>();
        }

        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [StringLength(50)]
        public string SubjectGroup { get; set; }

        public int? RankId { get; set; }

        public int? PositionId { get; set; }

        public int? OrgUnitId { get; set; }

        public int? BirthYear { get; set; }

        [Column(TypeName = "date")]
        public DateTime? EnlistmentDate { get; set; }

        [StringLength(500)]
        public string Hometown { get; set; }

        [StringLength(20)]
        public string CitizenId { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<DecisionDetail> DecisionDetails { get; set; }

        public virtual OrgUnit OrgUnit { get; set; }

        public virtual Position Position { get; set; }

        public virtual Rank Rank { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<RewardHistory> RewardHistories { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<RewardReportSnapshot> RewardReportSnapshots { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<SeniorityAward> SeniorityAwards { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<WeeklyScore> WeeklyScores { get; set; }
    }
}
