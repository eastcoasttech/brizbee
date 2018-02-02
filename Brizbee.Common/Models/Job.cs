using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Brizbee.Common.Models
{
    public partial class Job
    {
        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public virtual Customer Customer { get; set; }

        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }
    }
}
