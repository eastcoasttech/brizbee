﻿//
//  Job.cs
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

using Brizbee.Core.Validations;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Brizbee.Core.Models
{
    public class Job
    {
        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime CreatedAt { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public virtual Customer? Customer { get; set; }

        [StringLength(3000)]
        public string? Description { get; set; }

        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string? Name { get; set; }

        [Required]
        [StringLength(10)]
        public string? Number { get; set; }

        [FullNameLengthValidation]
        public string? QuickBooksCustomerJob { get; set; }

        [StringLength(159)]
        public string? QuickBooksClass { get; set; }

        [StringLength(20)]
        public string? Status { get; set; }

        [StringLength(50)]
        public string? CustomerWorkOrder { get; set; }

        [StringLength(50)]
        public string? CustomerPurchaseOrder { get; set; }

        [StringLength(50)]
        public string? InvoiceNumber { get; set; }

        [StringLength(50)]
        public string? QuoteNumber { get; set; }

        public int? TaskTemplateId { get; set; }

        [StringLength(9)]
        public string? Taxability { get; set; }
    }
}
