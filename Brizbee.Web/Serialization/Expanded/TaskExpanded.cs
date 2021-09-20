using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

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
