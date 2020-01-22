using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Brizbee.Common.Models
{
    /// <summary>
    /// Example:
    /// 
    /// Payroll Rates:
    /// 
    /// - Hourly Regular - Base Rate
    ///   - Hourly OT - Alternate Rate (after 40 hours)
    ///   - Hourly DT - Alternate Rate (holidays)
    ///   
    /// - Salary Regular - Base Rate
    /// 
    /// Service Rates:
    /// 
    /// - Consulting Regular - Base Rate
    ///   - Consulting OT - Alternate Rate (before 7am, after 5pm, and weekends)
    ///   - Consulting DT - Alternate Rate (holidays)
    ///   
    /// - Engineering Regular - Base Rate
    ///   - Engineering OT - Alternate Rate (before 7am, after 5pm, and weekends)
    ///   - Engineering DT - Alternate Rate (holidays)
    /// </summary>
    public class Rate
    {
        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime CreatedAt { get; set; }

        [Key]
        public int Id { get; set; }

        [Required]
        public bool IsDeleted { get; set; }

        [MaxLength(40)]
        public string Name { get; set; }

        [Required]
        public int OrganizationId { get; set; }

        [ForeignKey("OrganizationId")]
        public virtual Organization Organization { get; set; }

        public int? ParentRateId { get; set; }

        [ForeignKey("ParentRateId")]
        public virtual Rate ParentRate { get; set; } // Base Rates have Alternate Rates (Regular can have Over-time and Double-time)

        public string QBDPayrollItem { get; set; }

        public string QBDServiceItem { get; set; }

        public string QBOPayrollItem { get; set; }

        public string QBOServiceItem { get; set; }

        [MaxLength(20)]
        public string Type { get; set; } // Payroll or Service
    }
}
