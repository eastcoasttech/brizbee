//
//  User.cs
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
using System.Runtime.Serialization;

namespace Brizbee.Common.Models
{
    public partial class User
    {
        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime CreatedAt { get; set; }

        [EmailAddress]
        [StringLength(254)]
        public string EmailAddress { get; set; }

        [Key]
        public int Id { get; set; }

        [Required]
        public bool IsDeleted { get; set; }

        [Required]
        [StringLength(128)]
        public string Name { get; set; }

        [Required]
        public int OrganizationId { get; set; }

        [ForeignKey("OrganizationId")]
        public virtual Organization Organization { get; set; }

        // Password is ignored in BrizbeeWebContext configuration
        public string Password { get; set; }

        [IgnoreDataMember]
        public string PasswordSalt { get; set; }

        [IgnoreDataMember]
        public string PasswordHash { get; set; }

        [Required]
        public string Pin { get; set; }

        public string QuickBooksEmployee { get; set; } // DEPRECATED

        public string QBOGivenName { get; set; }

        public string QBOMiddleName { get; set; }

        public string QBOFamilyName { get; set; }

        public bool RequiresLocation { get; set; }

        public bool RequiresPhoto { get; set; }

        [Required]
        [StringLength(128)]
        public string Role { get; set; }

        [Required]
        public string TimeZone { get; set; }

        public bool UsesMobileClock { get; set; }

        public bool UsesTouchToneClock { get; set; }

        public bool UsesWebClock { get; set; }

        public bool UsesTimesheets { get; set; }

        /// <summary>
        /// Comma-separated list of phone numbers that are
        /// allowed to use the touch-tone telephone clock.
        /// </summary>
        [StringLength(260)]
        public string AllowedPhoneNumbers { get; set; }

        /// <summary>
        /// Comma-separated list of phone numbers to send
        /// SMS notifications.
        /// </summary>
        [StringLength(260)]
        public string NotificationMobileNumbers { get; set; }
    }
}
