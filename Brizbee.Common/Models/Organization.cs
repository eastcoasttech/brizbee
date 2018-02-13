using System;
using System.ComponentModel.DataAnnotations;

namespace Brizbee.Common.Models
{
    public partial class Organization
    {
        [Required]
        public DateTime CreatedAt { get; set; }

        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Code { get; set; }
    }
}
