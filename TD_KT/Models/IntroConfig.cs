namespace TD_KT.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class IntroConfig
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string ConfigKey { get; set; }

        [Required]
        [StringLength(200)]
        public string ConfigName { get; set; }

        public string Content { get; set; }
    }
}
