namespace TD_KT.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class UserPermission
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public bool ViewTM { get; set; }

        public bool EditTM { get; set; }

        public bool ViewCT { get; set; }

        public bool EditCT { get; set; }

        public bool ViewHCKT { get; set; }

        public bool EditHCKT { get; set; }

        public bool ViewNV { get; set; }

        public bool EditNV { get; set; }

        public bool CanFinalizeScore { get; set; }
        public bool CanLockWeek { get; set; }

        public bool CanAccessReport { get; set; }

        public bool CanAccessAdmin { get; set; }

        public virtual User User { get; set; }
    }
}
