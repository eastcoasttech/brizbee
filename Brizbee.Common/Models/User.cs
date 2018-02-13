﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace Brizbee.Common.Models
{
    public partial class User
    {
        [Required]
        public DateTime CreatedAt { get; set; }

        [EmailAddress]
        [Required]
        public string EmailAddress { get; set; }

        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public int OrganizationId { get; set; }

        [ForeignKey("OrganizationId")]
        public virtual Organization Organization { get; set; }

        public string Password { get; set; }
        [IgnoreDataMember]
        public string PasswordSalt { get; set; }
        [IgnoreDataMember]
        public string PasswordHash { get; set; }

        public string Pin { get; set; }

        [Required]
        public string Role { get; set; }
    }
}
