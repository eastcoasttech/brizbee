//
//  Task.cs
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

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Brizbee.Core.Models
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
        public virtual Job? Job { get; set; }

        [Required]
        [StringLength(100)]
        public string? Name { get; set; }

        [Required]
        [StringLength(10)]
        public string? Number { get; set; }

        [Required]
        public int Order { get; set; }

        [StringLength(20)]
        public string? Group { get; set; }

        public string? QuickBooksPayrollItem { get; set; }

        public string? QuickBooksServiceItem { get; set; }

        public int? BaseServiceRateId { get; set; } // optional, but necessary for populating punches later

        [ForeignKey("BaseServiceRateId")]
        public virtual Rate? BaseServiceRate { get; set; }

        public int? BasePayrollRateId { get; set; } // optional, but necessary for populating punches later

        [ForeignKey("BasePayrollRateId")]
        public virtual Rate? BasePayrollRate { get; set; }
    }
}
