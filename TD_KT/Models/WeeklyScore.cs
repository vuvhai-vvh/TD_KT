namespace TD_KT.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class WeeklyScore
    {
        public int Id { get; set; }

        public int Year { get; set; }

        public int Month { get; set; }

        public int Week { get; set; }

        public int AreaType { get; set; }

        public int? SoldierId { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [StringLength(50)]
        public string Rank { get; set; }

        [StringLength(100)]
        public string Position { get; set; }

        [StringLength(200)]
        public string OrgUnit { get; set; }

        [StringLength(1000)]
        public string ViolationContent { get; set; }
        public int ViolationLevel { get; set; }

        [StringLength(1000)]
        public string CommendationContent { get; set; }

        public DateTime? RecordDateTime { get; set; }

        [StringLength(100)]
        public string EnteredBy { get; set; }

        public virtual Soldier Soldier { get; set; }
    }
}
