namespace TD_KT.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("UnitScoreSummary")]
    public partial class UnitScoreSummary
    {
        public int Id { get; set; }

        public int Year { get; set; }

        public int Month { get; set; }

        public int Week { get; set; }

        public int OrgUnitId { get; set; }

        public decimal? ScoreArea1 { get; set; }

        public decimal? ScoreArea2 { get; set; }

        public decimal? ScoreArea3 { get; set; }

        public decimal? ScoreArea4 { get; set; }

        public decimal? AverageScore { get; set; }

        public int? Ranking { get; set; }

        public virtual OrgUnit OrgUnit { get; set; }
    }
}
