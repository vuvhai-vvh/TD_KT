namespace TD_KT.Models
{
    using System;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("TitleProposals")]
    public partial class TitleProposal
    {
        public int Id { get; set; }

        public int ProposalYear { get; set; }

        public int SoldierId { get; set; }

        public int YearsOfService { get; set; }

        public string Status { get; set; }

        public string ProposedTitle { get; set; }

        public string Note { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public virtual Soldier Soldier { get; set; }
    }
}