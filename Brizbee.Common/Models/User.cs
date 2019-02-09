using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace Brizbee.Common.Models
{
    public partial class User
    {
        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime CreatedAt { get; set; }

        [EmailAddress]
        [MaxLength(254)]
        public string EmailAddress { get; set; }

        [Key]
        public int Id { get; set; }

        [Required]
        public bool IsDeleted { get; set; }

        [Required]
        [MaxLength(128)]
        public string Name { get; set; }

        [Required]
        public int OrganizationId { get; set; }

        [ForeignKey("OrganizationId")]
        public virtual Organization Organization { get; set; }

        // Password is ignored in BrizbeeWebContext configuration
        public string Password { get; set; }

        [IgnoreDataMember]
        public string PasswordSalt { get; set; }

        [IgnoreDataMember]
        public string PasswordHash { get; set; }

        [Required]
        public string Pin { get; set; }

        public string QuickBooksEmployee { get; set; }

        [Required]
        [MaxLength(128)]
        public string Role { get; set; }

        [Required]
        public string TimeZone { get; set; }
    }
}
