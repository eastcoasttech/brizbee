//
//  TaskExpanded.cs
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
    public class TaskExpanded
    {
        // Task Details

        public int Task_Id { get; set; }

        public DateTime Task_CreatedAt { get; set; }

        public string Task_Name { get; set; }

        public string Task_Number { get; set; }

        public int Task_JobId { get; set; }

        public string Task_QuickBooksPayrollItem { get; set; }

        public string Task_QuickBooksServiceItem { get; set; }

        public int? Task_BaseServiceRateId { get; set; }

        public int? Task_BasePayrollRateId { get; set; }


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


        // Base Payroll Rate Details

        public int? BasePayrollRate_Id { get; set; }

        public DateTime? BasePayrollRate_CreatedAt { get; set; }

        public bool? BasePayrollRate_IsDeleted { get; set; }

        public string BasePayrollRate_Name { get; set; }

        public int? BasePayrollRate_OrganizationId { get; set; }

        public int? BasePayrollRate_ParentRateId { get; set; }

        public string BasePayrollRate_QBDPayrollItem { get; set; }

        public string BasePayrollRate_QBOPayrollItem { get; set; }

        public string BasePayrollRate_Type { get; set; }


        // Base Service Rate Details

        public int? BaseServiceRate_Id { get; set; }

        public DateTime? BaseServiceRate_CreatedAt { get; set; }

        public bool? BaseServiceRate_IsDeleted { get; set; }

        public string BaseServiceRate_Name { get; set; }

        public int? BaseServiceRate_OrganizationId { get; set; }

        public int? BaseServiceRate_ParentRateId { get; set; }

        public string BaseServiceRate_QBDServiceItem { get; set; }

        public string BaseServiceRate_QBOServiceItem { get; set; }

        public string BaseServiceRate_Type { get; set; }
    }
}
