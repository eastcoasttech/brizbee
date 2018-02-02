using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

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
    }
}
