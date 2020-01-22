using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Brizbee.Common.Models
{
    public partial class Task
    {
        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime CreatedAt { get; set; }

        [Key]
        public int Id { get; set; }

        [Required]
        public int JobId { get; set; }

        [ForeignKey("JobId")]
        public virtual Job Job { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Number { get; set; }
        
        public string QuickBooksPayrollItem { get; set; }

        public string QuickBooksServiceItem { get; set; }

        public int? BaseServiceRateId { get; set; } // optional, but necessary for populating punches later

        [ForeignKey("BaseServiceRateId")]
        public virtual Rate BaseServiceRate { get; set; }

        public int? BasePayrollRateId { get; set; } // optional, but necessary for populating punches later

        [ForeignKey("BasePayrollRateId")]
        public virtual Rate BasePayrollRate { get; set; }
    }
}
