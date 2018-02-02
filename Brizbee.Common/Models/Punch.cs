using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Text;

namespace Brizbee.Common.Models
{
    public partial class Punch
    {
        [Required]
        public DateTime CreatedAt { get; set; }

        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime InAt { get; set; }

        public DateTime OutAt { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}
