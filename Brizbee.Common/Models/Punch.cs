//
//  Punch.cs
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
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Brizbee.Common.Models
{
    public partial class Punch
    {
        public int? CommitId { get; set; }

        [ForeignKey("CommitId")]
        public virtual Commit Commit { get; set; }

        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime CreatedAt { get; set; }

        [Required]
        public Guid Guid { get; set; }

        [Key]
        public int Id { get; set; }

        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime InAt { get; set; }

        public string InAtTimeZone { get; set; }

        [StringLength(20)]
        public string LatitudeForInAt { get; set; }

        [StringLength(20)]
        public string LongitudeForInAt { get; set; }

        [StringLength(20)]
        public string LatitudeForOutAt { get; set; }

        [StringLength(20)]
        public string LongitudeForOutAt { get; set; }

        [NotMapped]
        public int Minutes
        {
            get
            {
                if (this.OutAt.HasValue)
                {
                    return Convert.ToInt32((this.OutAt.Value - this.InAt).TotalMinutes);
                }
                else
                {
                    return 0;
                }
            }
        }

        [NotMapped]
        public int CumulativeMinutes { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime? OutAt { get; set; }

        public string OutAtTimeZone { get; set; }

        public string SourceForInAt { get; set; }

        public string SourceForOutAt { get; set; }

        [StringLength(12)]
        public string InAtSourceHardware { get; set; } // Web, Mobile, Phone, Dashboard

        [StringLength(30)]
        public string InAtSourceHostname { get; set; }

        [StringLength(30)]
        public string InAtSourceIpAddress { get; set; }

        [StringLength(30)]
        public string InAtSourceOperatingSystem { get; set; }

        [StringLength(20)]
        public string InAtSourceOperatingSystemVersion { get; set; }

        [StringLength(30)]
        public string InAtSourceBrowser { get; set; }

        [StringLength(20)]
        public string InAtSourceBrowserVersion { get; set; }

        [StringLength(30)]
        public string InAtSourcePhoneNumber { get; set; }

        [StringLength(12)]
        public string OutAtSourceHardware { get; set; } // Web, Mobile, Phone, Dashboard

        [StringLength(30)]
        public string OutAtSourceHostname { get; set; }

        [StringLength(30)]
        public string OutAtSourceIpAddress { get; set; }

        [StringLength(30)]
        public string OutAtSourceOperatingSystem { get; set; }

        [StringLength(20)]
        public string OutAtSourceOperatingSystemVersion { get; set; }

        [StringLength(30)]
        public string OutAtSourceBrowser { get; set; }

        [StringLength(20)]
        public string OutAtSourceBrowserVersion { get; set; }

        [StringLength(30)]
        public string OutAtSourcePhoneNumber { get; set; }

        public int? ServiceRateId { get; set; } // Populated later by administrators

        [ForeignKey("ServiceRateId")]
        public virtual Rate ServiceRate { get; set; }

        public int? PayrollRateId { get; set; } // Populated later by administrators

        [ForeignKey("PayrollRateId")]
        public virtual Rate PayrollRate { get; set; }

        [Required]
        public int TaskId { get; set; }

        [ForeignKey("TaskId")]
        public virtual Task Task { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}
