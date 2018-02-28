using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Brizbee.Common.Models
{
    public partial class Task
    {
        [Required]
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
    }
}
