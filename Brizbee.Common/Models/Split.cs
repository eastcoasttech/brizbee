using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Brizbee.Common.Models
{
    public class Split
    {
        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime CreatedAt { get; set; }

        [Required]
        public int CreatedByUserId { get; set; }

        [ForeignKey("CreatedByUserId")]
        public virtual User CreatedByUser { get; set; }

        [Key]
        public int Id { get; set; }

        public string BeforeSplit { get; set; }

        public string AfterSplit { get; set; }

        [Required]
        public int OrganizationId { get; set; }

        [ForeignKey("OrganizationId")]
        public virtual Organization Organization { get; set; }
    }
}
