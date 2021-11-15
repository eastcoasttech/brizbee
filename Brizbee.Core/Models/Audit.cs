//
//  Audit.cs
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
    public class Audit
    {
        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime CreatedAt { get; set; }

        [Key]
        public long Id { get; set; }

        [Required]
        public int OrganizationId { get; set; }

        [ForeignKey("OrganizationId")]
        public virtual Organization? Organization { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [Required]
        public long ObjectId { get; set; }

        [Required]
        [StringLength(10)]
        public string? Action { get; set; }

        /// <summary>
        /// JSON representation of the object before the change.
        /// </summary>
        [Required]
        public string? Before { get; set; }

        /// <summary>
        /// JSON representation of the object after the change.
        /// </summary>
        [Required]
        public string? After { get; set; }

        [NotMapped]
        public string? ObjectType { get; set; }
    }
}
