//
//  Organization.cs
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
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Brizbee.Core.Models
{
    public class Organization
    {
        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime CreatedAt { get; set; }

        [Key]
        public int Id { get; set; }

        [StringLength(7)]
        public string? MinutesFormat { get; set; }

        [Required]
        [StringLength(100)]
        public string? Name { get; set; }

        [Required]
        [StringLength(8)]
        public string? Code { get; set; }

        [Required]
        public int PlanId { get; set; } // 1, 2, 3, or 4

        [Required]
        public string? StripeCustomerId { get; set; }

        public string? StripeSourceCardBrand { get; set; }

        public string? StripeSourceCardExpirationMonth { get; set; }

        public string? StripeSourceCardExpirationYear { get; set; }

        public string? StripeSourceCardLast4 { get; set; }
        
        public string? StripeSourceId { get; set; }

        [Required]
        public string? StripeSubscriptionId { get; set; }

        [StringLength(200)]
        public string? Groups { get; set; }

        /// <summary>
        /// Indicates whether to show customer number on dashboard.
        /// </summary>
        [Required]
        public bool ShowCustomerNumber { get; set; }

        /// <summary>
        /// Indicates which column, Name or Number, is used to
        /// sort customers in drop downs.
        /// </summary>
        [Required]
        [StringLength(6)]
        public string? SortCustomersByColumn { get; set; }

        /// <summary>
        /// Indicates whether to show project number on dashboard.
        /// </summary>
        [Required]
        public bool ShowProjectNumber { get; set; }

        /// <summary>
        /// Indicates which column, Name or Number, is used to
        /// sort projects in drop downs.
        /// </summary>
        [Required]
        [StringLength(6)]
        public string? SortProjectsByColumn { get; set; }

        /// <summary>
        /// Indicates whether to show task number on dashboard.
        /// </summary>
        [Required]
        public bool ShowTaskNumber { get; set; }

        /// <summary>
        /// Indicates which column, Name or Number, is used to
        /// sort tasks in drop downs.
        /// </summary>
        [Required]
        [StringLength(6)]
        public string? SortTasksByColumn { get; set; }
    }
}
