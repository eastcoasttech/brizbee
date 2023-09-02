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

namespace Brizbee.Dashboard.Models
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

        public string QuickBooksEmployee { get; set; }

        public string QuickBooksVendor { get; set; }

        public string QBOGivenName { get; set; }

        public string QBOMiddleName { get; set; }

        public string QBOFamilyName { get; set; }

        public bool RequiresLocation { get; set; }

        public bool RequiresPhoto { get; set; }

        [StringLength(128)]
        public string Role { get; set; }

        [Required]
        public string TimeZone { get; set; }

        public bool UsesMobileClock { get; set; }

        public bool UsesTouchToneClock { get; set; }

        public bool UsesWebClock { get; set; }

        public bool UsesTimesheets { get; set; }

        /// <summary>
        /// Indicates whether or not to send an Email every day
        /// showing punches for users who were punched in through midnight.
        /// </summary>
        public bool ShouldSendMidnightPunchEmail { get; set; }

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

        /// <summary>
        /// Indicates whether or not the user is active. Inactive
        /// users do not appear in drop downs and reports and cannot
        /// punch in or otherwise use the dashboard.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Indicates whether or not the user can view punches.
        /// </summary>
        public bool CanViewPunches { get; set; }

        /// <summary>
        /// Indicates whether or not the user can create punches.
        /// </summary>
        public bool CanCreatePunches { get; set; }

        /// <summary>
        /// Indicates whether or not the user can modify punches.
        /// </summary>
        public bool CanModifyPunches { get; set; }

        /// <summary>
        /// Indicates whether or not the user can delete punches.
        /// </summary>
        public bool CanDeletePunches { get; set; }

        /// <summary>
        /// Indicates whether or not the user can split and populate punches.
        /// </summary>
        public bool CanSplitAndPopulatePunches { get; set; }

        /// <summary>
        /// Indicates whether or not the user can view reports.
        /// </summary>
        public bool CanViewReports { get; set; }

        /// <summary>
        /// Indicates whether or not the user can view punch locks.
        /// </summary>
        public bool CanViewLocks { get; set; }

        /// <summary>
        /// Indicates whether or not the user can create punch locks.
        /// </summary>
        public bool CanCreateLocks { get; set; }

        /// <summary>
        /// Indicates whether or not the user can undo punch locks.
        /// </summary>
        public bool CanUndoLocks { get; set; }

        /// <summary>
        /// Indicates whether or not the user can view timecards.
        /// </summary>
        public bool CanViewTimecards { get; set; }

        /// <summary>
        /// Indicates whether or not the user can create timecards.
        /// </summary>
        public bool CanCreateTimecards { get; set; }

        /// <summary>
        /// Indicates whether or not the user can modify timecards.
        /// </summary>
        public bool CanModifyTimecards { get; set; }

        /// <summary>
        /// Indicates whether or not the user can delete timecards.
        /// </summary>
        public bool CanDeleteTimecards { get; set; }

        /// <summary>
        /// Indicates whether or not the user can view users.
        /// </summary>
        public bool CanViewUsers { get; set; }

        /// <summary>
        /// Indicates whether or not the user can create users.
        /// </summary>
        public bool CanCreateUsers { get; set; }

        /// <summary>
        /// Indicates whether or not the user can modify users.
        /// </summary>
        public bool CanModifyUsers { get; set; }

        /// <summary>
        /// Indicates whether or not the user can delete users.
        /// </summary>
        public bool CanDeleteUsers { get; set; }

        /// <summary>
        /// Indicates whether or not the user can view inventory items.
        /// </summary>
        public bool CanViewInventoryItems { get; set; }

        /// <summary>
        /// Indicates whether or not the user can modify inventory items.
        /// </summary>
        public bool CanModifyInventoryItems { get; set; }

        /// <summary>
        /// Indicates whether or not the user can sync inventory items.
        /// </summary>
        public bool CanSyncInventoryItems { get; set; }

        /// <summary>
        /// Indicates whether or not the user can view inventory consumptions.
        /// </summary>
        public bool CanViewInventoryConsumptions { get; set; }

        /// <summary>
        /// Indicates whether or not the user can sync inventory consumptions.
        /// </summary>
        public bool CanSyncInventoryConsumptions { get; set; }

        /// <summary>
        /// Indicates whether or not the user can delete inventory consumptions.
        /// </summary>
        public bool CanDeleteInventoryConsumptions { get; set; }

        /// <summary>
        /// Indicates whether or not the user can view rates.
        /// </summary>
        public bool CanViewRates { get; set; }

        /// <summary>
        /// Indicates whether or not the user can create rates.
        /// </summary>
        public bool CanCreateRates { get; set; }

        /// <summary>
        /// Indicates whether or not the user can modify rates.
        /// </summary>
        public bool CanModifyRates { get; set; }

        /// <summary>
        /// Indicates whether or not the user can delete rates.
        /// </summary>
        public bool CanDeleteRates { get; set; }

        /// <summary>
        /// Indicates whether or not the user can view organization details.
        /// </summary>
        public bool CanViewOrganizationDetails { get; set; }

        /// <summary>
        /// Indicates whether or not the user can modify organization details.
        /// </summary>
        public bool CanModifyOrganizationDetails { get; set; }

        /// <summary>
        /// Indicates whether or not the user can view customers.
        /// </summary>
        public bool CanViewCustomers { get; set; }

        /// <summary>
        /// Indicates whether or not the user can create customers.
        /// </summary>
        public bool CanCreateCustomers { get; set; }

        /// <summary>
        /// Indicates whether or not the user can modify customers.
        /// </summary>
        public bool CanModifyCustomers { get; set; }

        /// <summary>
        /// Indicates whether or not the user can delete customers.
        /// </summary>
        public bool CanDeleteCustomers { get; set; }

        /// <summary>
        /// Indicates whether or not the user can view projects.
        /// </summary>
        public bool CanViewProjects { get; set; }

        /// <summary>
        /// Indicates whether or not the user can create projects.
        /// </summary>
        public bool CanCreateProjects { get; set; }

        /// <summary>
        /// Indicates whether or not the user can modify projects.
        /// </summary>
        public bool CanModifyProjects { get; set; }

        /// <summary>
        /// Indicates whether or not the user can delete projects.
        /// </summary>
        public bool CanDeleteProjects { get; set; }
        
        /// <summary>
        /// Indicates whether or not the user can merge projects.
        /// </summary>
        public bool CanMergeProjects { get; set; }

        /// <summary>
        /// Indicates whether or not the user can view tasks.
        /// </summary>
        public bool CanViewTasks { get; set; }

        /// <summary>
        /// Indicates whether or not the user can create tasks.
        /// </summary>
        public bool CanCreateTasks { get; set; }

        /// <summary>
        /// Indicates whether or not the user can modify tasks.
        /// </summary>
        public bool CanModifyTasks { get; set; }

        /// <summary>
        /// Indicates whether or not the user can delete tasks.
        /// </summary>
        public bool CanDeleteTasks { get; set; }

        /// <summary>
        /// Indicates whether or not the user can view audits.
        /// </summary>
        public bool CanViewAudits { get; set; }
    }
}
