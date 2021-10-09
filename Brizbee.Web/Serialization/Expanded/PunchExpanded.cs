//
//  PunchExpanded.cs
//  BRIZBEE API
//
//  Copyright (C) 2019-2021 East Coast Technology Services, LLC
//
//  This file is part of the BRIZBEE API.
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

namespace Brizbee.Web.Serialization.Expanded
{
    public class PunchExpanded
    {
        // Punch Details

        public int Punch_Id { get; set; }

        public int? Punch_CommitId { get; set; }

        public DateTime Punch_CreatedAt { get; set; }

        public Guid Punch_Guid { get; set; }

        public DateTime Punch_InAt { get; set; }

        public string Punch_InAtTimeZone { get; set; }

        public string Punch_LatitudeForInAt { get; set; }

        public string Punch_LongitudeForInAt { get; set; }

        public string Punch_LatitudeForOutAt { get; set; }

        public string Punch_LongitudeForOutAt { get; set; }

        public DateTime? Punch_OutAt { get; set; }

        public string Punch_OutAtTimeZone { get; set; }

        public int Punch_TaskId { get; set; }

        public int Punch_UserId { get; set; }

        public string Punch_InAtSourceHardware { get; set; }

        public string Punch_InAtSourceHostname { get; set; }

        public string Punch_InAtSourceIpAddress { get; set; }

        public string Punch_InAtSourceOperatingSystem { get; set; }

        public string Punch_InAtSourceOperatingSystemVersion { get; set; }

        public string Punch_InAtSourceBrowser { get; set; }

        public string Punch_InAtSourceBrowserVersion { get; set; }

        public string Punch_InAtSourcePhoneNumber { get; set; }

        public string Punch_OutAtSourceHardware { get; set; }

        public string Punch_OutAtSourceHostname { get; set; }

        public string Punch_OutAtSourceIpAddress { get; set; }

        public string Punch_OutAtSourceOperatingSystem { get; set; }

        public string Punch_OutAtSourceOperatingSystemVersion { get; set; }

        public string Punch_OutAtSourceBrowser { get; set; }

        public string Punch_OutAtSourceBrowserVersion { get; set; }

        public string Punch_OutAtSourcePhoneNumber { get; set; }

        public int? Punch_ServiceRateId { get; set; }

        public int? Punch_PayrollRateId { get; set; }

        public int Punch_Minutes { get; set; }

        public int Punch_CumulativeMinutes { get; set; }


        // Customer Details

        public int Customer_Id { get; set; }

        public DateTime Customer_CreatedAt { get; set; }

        public string Customer_Description { get; set; }

        public string Customer_Name { get; set; }

        public string Customer_Number { get; set; }

        public int Customer_OrganizationId { get; set; }


        // Job Details

        public int Job_Id { get; set; }

        public DateTime Job_CreatedAt { get; set; }

        public int Job_CustomerId { get; set; }

        public string Job_Description { get; set; }

        public string Job_Name { get; set; }

        public string Job_Number { get; set; }

        public string Job_QuickBooksCustomerJob { get; set; }


        // Task Details

        public int Task_Id { get; set; }

        public DateTime Task_CreatedAt { get; set; }

        public int Task_JobId { get; set; }

        public string Task_Name { get; set; }

        public string Task_Number { get; set; }

        public string Task_QuickBooksPayrollItem { get; set; }

        public string Task_QuickBooksServiceItem { get; set; }

        public int Task_BaseServiceRateId { get; set; }

        public int Task_BasePayrollRateId { get; set; }


        // User Details

        public int User_Id { get; set; }

        public DateTime User_CreatedAt { get; set; }

        public string User_EmailAddress { get; set; }

        public bool User_IsDeleted { get; set; }

        public string User_Name { get; set; }

        public int User_OrganizationId { get; set; }

        public string User_Role { get; set; }

        public string User_TimeZone { get; set; }

        public bool User_UsesMobileClock { get; set; }

        public bool User_UsesTouchToneClock { get; set; }

        public bool User_UsesWebClock { get; set; }

        public bool User_UsesTimesheets { get; set; }

        public bool User_RequiresLocation { get; set; }

        public bool User_RequiresPhoto { get; set; }

        public string User_QBOGivenName { get; set; }

        public string User_QBOMiddleName { get; set; }

        public string User_QBOFamilyName { get; set; }

        public string User_AllowedPhoneNumbers { get; set; }

        public string User_QuickBooksEmployee { get; set; }


        // Payroll Rate Details

        public int? PayrollRate_Id { get; set; }

        public DateTime? PayrollRate_CreatedAt { get; set; }

        public bool? PayrollRate_IsDeleted { get; set; }

        public string PayrollRate_Name { get; set; }

        public int? PayrollRate_OrganizationId { get; set; }

        public int? PayrollRate_ParentRateId { get; set; }

        public string PayrollRate_QBDPayrollItem { get; set; }

        public string PayrollRate_QBOPayrollItem { get; set; }

        public string PayrollRate_Type { get; set; }


        // Service Rate Details

        public int? ServiceRate_Id { get; set; }

        public DateTime? ServiceRate_CreatedAt { get; set; }

        public bool? ServiceRate_IsDeleted { get; set; }

        public string ServiceRate_Name { get; set; }

        public int? ServiceRate_OrganizationId { get; set; }

        public int? ServiceRate_ParentRateId { get; set; }

        public string ServiceRate_QBDServiceItem { get; set; }

        public string ServiceRate_QBOServiceItem { get; set; }

        public string ServiceRate_Type { get; set; }
    }
}