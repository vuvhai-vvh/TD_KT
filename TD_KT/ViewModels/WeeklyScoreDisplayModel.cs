using System;

namespace TD_KT.ViewModels
{
    public class WeeklyScoreDisplayModel
    {
        public int No { get; set; }
        public int Id { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int Week { get; set; }
        public int? SoldierId { get; set; }
        public string FullName { get; set; }
        public string Rank { get; set; }
        public string Position { get; set; }
        public string OrgUnit { get; set; }
        public string ViolationContent { get; set; }
        public int ViolationLevel { get; set; }
        public string ViolationContentDisplay { get; set; }
        public string CommendationContent { get; set; }
        public DateTime? RecordDateTime { get; set; }
        public string EnteredBy { get; set; }
        public int AreaType { get; set; }
    }
}
