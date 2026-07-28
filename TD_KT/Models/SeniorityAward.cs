namespace TD_KT.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class SeniorityAward
    {
        public int Id { get; set; }

        public int Year { get; set; }

        public int SoldierId { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [StringLength(50)]
        public string Rank { get; set; }

        [StringLength(100)]
        public string Position { get; set; }

        [StringLength(200)]
        public string OrgUnit { get; set; }

        [Column(TypeName = "date")]
        public DateTime? EnlistmentDate { get; set; }

        public int YearsOfService { get; set; }

        [StringLength(300)]
        public string SuggestedReward { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; }

        public virtual Soldier Soldier { get; set; }
    }
}
