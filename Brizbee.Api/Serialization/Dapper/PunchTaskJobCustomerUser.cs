using System;

namespace Brizbee.Api.Serialization.Dapper
{
    public class PunchTaskJobCustomerUser
    {
        public int? Punch_CommitId { get; set; }

        public DateTime Punch_CreatedAt { get; set; }

        public Guid Punch_Guid { get; set; }

        public int Punch_Id { get; set; }

        public DateTime Punch_InAt { get; set; }

        public string Punch_InAtTimeZone { get; set; }

        public string Punch_LatitudeForInAt { get; set; }

        public string Punch_LongitudeForInAt { get; set; }

        public string Punch_LatitudeForOutAt { get; set; }

        public string Punch_LongitudeForOutAt { get; set; }

        public DateTime? Punch_OutAt { get; set; }

        public string Punch_OutAtTimeZone { get; set; }

        public string Punch_SourceForInAt { get; set; }

        public string Punch_SourceForOutAt { get; set; }

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

        public int Punch_TaskId { get; set; }

        public int Punch_UserId { get; set; }


        public DateTime Customer_CreatedAt { get; set; }

        public string Customer_Description { get; set; }

        public int Customer_Id { get; set; }

        public string Customer_Name { get; set; }

        public string Customer_Number { get; set; }

        public int Customer_OrganizationId { get; set; }


        public DateTime Job_CreatedAt { get; set; }

        public int Job_CustomerId { get; set; }

        public string Job_Description { get; set; }

        public int Job_Id { get; set; }

        public string Job_Name { get; set; }

        public string Job_Number { get; set; }

        public string Job_QuickBooksCustomerJob { get; set; }


        public DateTime Task_CreatedAt { get; set; }

        public int Task_Id { get; set; }

        public int Task_JobId { get; set; }

        public string Task_Name { get; set; }

        public string Task_Number { get; set; }

        public string Task_QuickBooksPayrollItem { get; set; }

        public string Task_QuickBooksServiceItem { get; set; }

        public int? Task_BaseServiceRateId { get; set; }

        public int? Task_BasePayrollRateId { get; set; }


        public DateTime User_CreatedAt { get; set; }

        public string User_EmailAddress { get; set; }

        public int User_Id { get; set; }

        public bool User_IsDeleted { get; set; }

        public string User_Name { get; set; }

        public int User_OrganizationId { get; set; }

        public string User_QBOGivenName { get; set; }

        public string User_QBOMiddleName { get; set; }

        public string User_QBOFamilyName { get; set; }

        public bool User_RequiresLocation { get; set; }

        public bool User_RequiresPhoto { get; set; }

        public string User_Role { get; set; }

        public string User_TimeZone { get; set; }

        public bool User_UsesMobileClock { get; set; }

        public bool User_UsesTouchToneClock { get; set; }

        public bool User_UsesWebClock { get; set; }

        public bool User_UsesTimesheets { get; set; }
    }
}
