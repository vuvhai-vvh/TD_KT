namespace TD_KT.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class User
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public User()
        {
            UserPermissions = new HashSet<UserPermission>();
        }

        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; }

        [StringLength(255)]
        public string Password { get; set; }

        [StringLength(255)]
        public string PasswordHash { get; set; }

        [StringLength(255)]
        public string PasswordSalt { get; set; }

        [Required]
        [StringLength(50)]
        public string PasswordAlgo { get; set; }

        public int PasswordIterations { get; set; }

        [Required]
        [StringLength(100)]
        public string DisplayName { get; set; }

        public int OrgUnitId { get; set; }

        public int PositionId { get; set; }

        [Required]
        [StringLength(50)]
        public string Role { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public virtual OrgUnit OrgUnit { get; set; }

        public virtual Position Position { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<UserPermission> UserPermissions { get; set; }
    }
}
