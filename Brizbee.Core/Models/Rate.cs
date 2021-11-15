//
//  Rate.cs
//  BRIZBEE Common Library
//
//  Copyright (C) 2019-2021 East Coast Technology Services, LLC
//
//  This file is part of the BRIZBEE Common Library.
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Affero General Public License as
//  published by the Free Software Foundation, either version 3 of the
//  License, or (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Affero General Public License for more details.
//
//  You should have received a copy of the GNU Affero General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
//

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Brizbee.Core.Models
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

        [Required]
        [StringLength(40)]
        public string? Name { get; set; }

        [Required]
        public int OrganizationId { get; set; }

        [ForeignKey("OrganizationId")]
        public virtual Organization? Organization { get; set; }

        public int? ParentRateId { get; set; }

        [ForeignKey("ParentRateId")]
        public virtual Rate? ParentRate { get; set; } // Base Rates have Alternate Rates (Regular can have Over-time and Double-time)

        [StringLength(31)]
        public string? QBDPayrollItem { get; set; }

        [StringLength(31)]
        public string? QBDServiceItem { get; set; }

        [StringLength(31)]
        public string? QBOPayrollItem { get; set; }

        [StringLength(31)]
        public string? QBOServiceItem { get; set; }

        [StringLength(20)]
        public string? Type { get; set; } // Payroll or Service
    }
}
