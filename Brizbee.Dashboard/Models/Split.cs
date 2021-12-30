//
//  Split.cs
//  BRIZBEE Common Library
//
//  Copyright (C) 2020 East Coast Technology Services, LLC
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

namespace Brizbee.Dashboard.Models
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
