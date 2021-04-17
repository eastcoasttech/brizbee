﻿//
//  PunchesExpandedController.cs
//  BRIZBEE API
//
//  Copyright (C) 2020 East Coast Technology Services, LLC
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

using Brizbee.Common.Models;
using Dapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Brizbee.Web.Controllers
{
    public class PunchesExpandedController : ApiController
    {
        private SqlContext _context = new SqlContext();
        private JsonSerializerSettings settings = new JsonSerializerSettings()
        {
            NullValueHandling = NullValueHandling.Ignore,
            StringEscapeHandling = StringEscapeHandling.EscapeHtml,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        // GET: api/PunchesExpanded
        [HttpGet]
        [Route("api/PunchesExpanded")]
        public HttpResponseMessage GetPunches([FromUri] DateTime min, [FromUri] DateTime max,
            [FromUri] int skip = 0, [FromUri] int pageSize = 1000,
            [FromUri] string orderBy = "PUNCHES/INAT", [FromUri] string orderByDirection = "ASC",
            [FromUri] int[] jobIds = null, [FromUri] string[] jobNames = null,
            [FromUri] int[] taskIds = null, [FromUri] string[] taskNames = null,
            [FromUri] int[] customerIds = null, [FromUri] string[] customerNames = null)
        {
            if (pageSize > 1000) { Request.CreateResponse(HttpStatusCode.BadRequest); }

            var currentUser = CurrentUser();

            // Ensure Administrator.
            if (currentUser.Role != "Administrator")
                Request.CreateResponse(HttpStatusCode.BadRequest);

            // Determine the number of records to skip.
            //int skip = (pageNumber - 1) * pageSize;

            var total = 0;
            List<Punch> punches = new List<Punch>();
            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["SqlContext"].ToString()))
            {
                connection.Open();

                // Determine the order by columns.
                var orderByFormatted = "";
                switch (orderBy.ToUpperInvariant())
                {
                    case "PUNCHES/CREATEDAT":
                        orderByFormatted = "P.[CreatedAt]";
                        break;
                    case "PUNCHES/INAT":
                        orderByFormatted = "P.[InAt]";
                        break;
                    case "PUNCHES/OUTAT":
                        orderByFormatted = "P.[OutAt]";
                        break;
                    case "CUSTOMERS/NUMBER":
                        orderByFormatted = "C.[Number]";
                        break;
                    case "CUSTOMERS/NAME":
                        orderByFormatted = "C.[Name]";
                        break;
                    case "JOBS/NUMBER":
                        orderByFormatted = "J.[Number]";
                        break;
                    case "JOBS/NAME":
                        orderByFormatted = "J.[Name]";
                        break;
                    case "TASKS/NUMBER":
                        orderByFormatted = "T.[Number]";
                        break;
                    case "TASKS/NAME":
                        orderByFormatted = "T.[Name]";
                        break;
                    case "SERVICERATE/NAME":
                        orderByFormatted = "SR.[Name]";
                        break;
                    case "PAYROLLRATE/NAME":
                        orderByFormatted = "PR.[Name]";
                        break;
                    case "USERS/NAME":
                        orderByFormatted = "U.[Name]";
                        break;
                    default:
                        orderByFormatted = "P.[CreatedAt]";
                        break;
                }

                // Determine the order direction.
                var orderByDirectionFormatted = "";
                switch (orderByDirection.ToUpperInvariant())
                {
                    case "ASC":
                        orderByDirectionFormatted = "ASC";
                        break;
                    case "DESC":
                        orderByDirectionFormatted = "DESC";
                        break;
                    default:
                        orderByDirectionFormatted = "ASC";
                        break;
                }

                var whereClause = "";
                var parameters = new DynamicParameters();

                // Common clause.
                parameters.Add("@Min", min);
                parameters.Add("@Max", max);
                parameters.Add("@OrganizationId", currentUser.OrganizationId);

                // Clause for job ids.
                if (jobIds != null && jobIds.Any())
                {
                    whereClause += "AND J.[Id] IN (@JobIds)";
                    parameters.Add("@JobIds", jobIds);
                }

                // Clause for job names.
                if (jobNames != null && jobNames.Any())
                {
                    whereClause += "AND J.[Name] IN (@JobNames)";
                    parameters.Add("@JobNames", jobNames);
                }

                // Clause for task ids.
                if (taskIds != null && taskIds.Any())
                {
                    whereClause += "AND T.[Id] IN (@TaskIds)";
                    parameters.Add("@TaskIds", taskIds);
                }

                // Clause for task names.
                if (taskNames != null && taskNames.Any())
                {
                    whereClause += "AND T.[Name] IN (@TaskNames)";
                    parameters.Add("@TaskNames", taskNames);
                }

                // Clause for customer ids.
                if (customerIds != null && customerIds.Any())
                {
                    whereClause += "AND C.[Id] IN (@CustomerIds)";
                    parameters.Add("@CustomerIds", customerIds);
                }

                // Clause for customer names.
                if (customerNames != null && customerNames.Any())
                {
                    whereClause += "AND C.[Name] IN (@CustomerNames)";
                    parameters.Add("@CustomerNames", customerNames);
                }

                // Get the count.
                var countSql = $@"
                    SELECT
                        COUNT(*)
                    FROM
                        [Punches] AS P
                    INNER JOIN
                        [Tasks] AS T ON P.[TaskId] = T.[Id]
                    INNER JOIN
                        [Jobs] AS J ON T.[JobId] = J.[Id]
                    INNER JOIN
                        [Customers] AS C ON J.[CustomerId] = C.[Id]
                    INNER JOIN
                        [Users] AS U ON P.[UserId] = U.[Id]
                    WHERE
                        U.[OrganizationId] = @OrganizationId AND
                        P.[InAt] >= @Min AND
                        P.[InAt] <= @Max {whereClause};";

                total = connection.QuerySingle<int>(countSql, parameters);

                // Paging parameters.
                parameters.Add("@Skip", skip);
                parameters.Add("@PageSize", pageSize);

                // Get the records.
                var recordsSql = $@"
                    SELECT
                        P.[Id] AS Punch_Id,
                        P.[CommitId] AS Punch_CommitId,
                        P.[CreatedAt] AS Punch_CreatedAt,
                        P.[Guid] AS Punch_Guid,
                        P.[InAt] AS Punch_InAt,
                        P.[InAtTimeZone] AS Punch_InAtTimeZone,
                        P.[LatitudeForInAt] AS Punch_LatitudeForInAt,
                        P.[LongitudeForInAt] AS Punch_LongitudeForInAt,
                        P.[LatitudeForOutAt] AS Punch_LatitudeForOutAt,
                        P.[LongitudeForOutAt] AS Punch_LongitudeForOutAt,
                        P.[OutAt] AS Punch_OutAt,
                        P.[OutAtTimeZone] AS Punch_OutAtTimeZone,
                        P.[SourceForInAt] AS Punch_SourceForInAt,
                        P.[SourceForOutAt] AS Punch_SourceForOutAt,
                        P.[TaskId] AS Punch_TaskId,
                        P.[UserId] AS Punch_UserId,
                        P.[InAtSourceHardware] AS Punch_InAtSourceHardware,
                        P.[InAtSourceHostname] AS Punch_InAtSourceHostname,
                        P.[InAtSourceIpAddress] AS Punch_InAtSourceIpAddress,
                        P.[InAtSourceOperatingSystem] AS Punch_InAtSourceOperatingSystem,
                        P.[InAtSourceOperatingSystemVersion] AS Punch_InAtSourceOperatingSystemVersion,
                        P.[InAtSourceBrowser] AS Punch_InAtSourceBrowser,
                        P.[InAtSourceBrowserVersion] AS Punch_InAtSourceBrowserVersion,
                        P.[InAtSourcePhoneNumber] AS Punch_InAtSourcePhoneNumber,
                        P.[OutAtSourceHardware] AS Punch_OutAtSourceHardware,
                        P.[OutAtSourceHostname] AS Punch_OutAtSourceHostname,
                        P.[OutAtSourceIpAddress] AS Punch_OutAtSourceIpAddress,
                        P.[OutAtSourceOperatingSystem] AS Punch_OutAtSourceOperatingSystem,
                        P.[OutAtSourceOperatingSystemVersion] AS Punch_OutAtSourceOperatingSystemVersion,
                        P.[OutAtSourceBrowser] AS Punch_OutAtSourceBrowser,
                        P.[OutAtSourceBrowserVersion] AS Punch_OutAtSourceBrowserVersion,
                        P.[OutAtSourcePhoneNumber] AS Punch_OutAtSourcePhoneNumber,
                        P.[ServiceRateId] AS Punch_ServiceRateId,
                        P.[PayrollRateId] AS Punch_PayrollRateId,

                        DATEDIFF(minute, P.[InAt], P.[OutAt]) AS Punch_Minutes,
                        
                        SUM(DATEDIFF(minute, P.[InAt], P.[OutAt])) OVER(PARTITION BY UserId ORDER BY InAt ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS Punch_CumulativeMinutes,

                        C.[Id] AS Customer_Id,
                        C.[CreatedAt] AS Customer_CreatedAt,
                        C.[Description] AS Customer_Description,
                        C.[Name] AS Customer_Name,
                        C.[Number] AS Customer_Number,
                        C.[OrganizationId] AS Customer_OrganizationId,

                        J.[Id] AS Job_Id,
                        J.[CreatedAt] AS Job_CreatedAt,
                        J.[CustomerId] AS Job_CustomerId,
                        J.[Description] AS Job_Description,
                        J.[Name] AS Job_Name,
                        J.[Number] AS Job_Number,
                        J.[QuickBooksCustomerJob] AS Job_QuickBooksCustomerJob,

                        T.[Id] AS Task_Id,
                        T.[CreatedAt] AS Task_CreatedAt,
                        T.[JobId] AS Task_JobId,
                        T.[Name] AS Task_Name,
                        T.[Number] AS Task_Number,
                        T.[QuickBooksPayrollItem] AS Task_QuickBooksPayrollItem,
                        T.[QuickBooksServiceItem] AS Task_QuickBooksServiceItem,
                        T.[BaseServiceRateId] AS Task_BaseServiceRateId,
                        T.[BasePayrollRateId] AS Task_BasePayrollRateId,

                        U.[Id] AS User_Id,
                        U.[CreatedAt] AS User_CreatedAt,
                        U.[EmailAddress] AS User_EmailAddress,
                        U.[IsDeleted] AS User_IsDeleted,
                        U.[Name] AS User_Name,
                        U.[OrganizationId] AS User_OrganizationId,
                        U.[Role] AS User_Role,
                        U.[TimeZone] AS User_TimeZone,
                        U.[UsesMobileClock] AS User_UsesMobileClock,
                        U.[UsesTouchToneClock] AS User_UsesTouchToneClock,
                        U.[UsesWebClock] AS User_UsesWebClock,
                        U.[UsesTimesheets] AS User_UsesTimesheets,
                        U.[RequiresLocation] AS User_RequiresLocation,
                        U.[RequiresPhoto] AS User_RequiresPhoto,
                        U.[QBOGivenName] AS User_QBOGivenName,
                        U.[QBOMiddleName] AS User_QBOMiddleName,
                        U.[QBOFamilyName] AS User_QBOFamilyName,
                        U.[AllowedPhoneNumbers] AS User_AllowedPhoneNumbers,
                        U.[QuickBooksEmployee] AS User_QuickBooksEmployee,

                        PR.[Id] AS PayrollRate_Id,
                        PR.[CreatedAt] AS PayrollRate_CreatedAt,
                        PR.[IsDeleted] AS PayrollRate_IsDeleted,
                        PR.[Name] AS PayrollRate_Name,
                        PR.[OrganizationId] AS PayrollRate_OrganizationId,
                        PR.[ParentRateId] AS PayrollRate_ParentRateId,
                        PR.[QBDPayrollItem] AS PayrollRate_QBDPayrollItem,
                        PR.[QBOPayrollItem] AS PayrollRate_QBOPayrollItem,
                        PR.[Type] AS PayrollRate_Type,

                        SR.[Id] AS ServiceRate_Id,
                        SR.[CreatedAt] AS ServiceRate_CreatedAt,
                        SR.[IsDeleted] AS ServiceRate_IsDeleted,
                        SR.[Name] AS ServiceRate_Name,
                        SR.[OrganizationId] AS ServiceRate_OrganizationId,
                        SR.[ParentRateId] AS ServiceRate_ParentRateId,
                        SR.[QBDServiceItem] AS ServiceRate_QBDServiceItem,
                        SR.[QBOServiceItem] AS ServiceRate_QBOServiceItem,
                        SR.[Type] AS ServiceRate_Type
                    FROM
                        [Punches] AS P
                    INNER JOIN
                        [Tasks] AS T ON P.[TaskId] = T.[Id]
                    INNER JOIN
                        [Jobs] AS J ON T.[JobId] = J.[Id]
                    INNER JOIN
                        [Customers] AS C ON J.[CustomerId] = C.[Id]
                    INNER JOIN
                        [Users] AS U ON P.[UserId] = U.[Id]
                    LEFT OUTER JOIN
                        [Rates] AS PR ON P.[PayrollRateId] = PR.[Id]
                    LEFT OUTER JOIN
                        [Rates] AS SR ON P.[ServiceRateId] = SR.[Id]
                    WHERE
                        U.[OrganizationId] = @OrganizationId AND
                        P.[InAt] >= @Min AND
                        P.[InAt] <= @Max {whereClause}
                    ORDER BY
                        {orderByFormatted} {orderByDirectionFormatted}
                        {(orderByFormatted == "P.[InAt]" ? "" : $", P.[InAt] {orderByDirectionFormatted}")}
                    OFFSET @Skip ROWS
                    FETCH NEXT @PageSize ROWS ONLY;";

                var results = connection.Query<PunchExpanded>(recordsSql, parameters);

                foreach (var result in results)
                {
                    punches.Add(new Punch()
                    {
                        Id = result.Punch_Id,
                        CommitId = result.Punch_CommitId,
                        CreatedAt = result.Punch_CreatedAt,
                        Guid = result.Punch_Guid,
                        InAt = result.Punch_InAt.ToUniversalTime(),
                        InAtTimeZone = result.Punch_InAtTimeZone,
                        LatitudeForInAt = result.Punch_LatitudeForInAt,
                        LongitudeForInAt = result.Punch_LongitudeForInAt,
                        LatitudeForOutAt = result.Punch_LatitudeForOutAt,
                        LongitudeForOutAt = result.Punch_LongitudeForOutAt,
                        OutAt = result.Punch_OutAt.HasValue ? result.Punch_OutAt.Value.ToUniversalTime() : default(DateTime?),
                        OutAtTimeZone = result.Punch_OutAtTimeZone,
                        SourceForInAt = result.Punch_SourceForInAt,
                        SourceForOutAt = result.Punch_SourceForOutAt,
                        TaskId = result.Punch_TaskId,
                        UserId = result.Punch_UserId,
                        InAtSourceHardware = result.Punch_InAtSourceHardware,
                        InAtSourceHostname = result.Punch_InAtSourceHostname,
                        InAtSourceIpAddress = result.Punch_InAtSourceIpAddress,
                        InAtSourceOperatingSystem = result.Punch_InAtSourceOperatingSystem,
                        InAtSourceOperatingSystemVersion = result.Punch_InAtSourceOperatingSystemVersion,
                        InAtSourceBrowser = result.Punch_InAtSourceBrowser,
                        InAtSourceBrowserVersion = result.Punch_InAtSourceBrowserVersion,
                        InAtSourcePhoneNumber = result.Punch_InAtSourcePhoneNumber,
                        OutAtSourceHardware = result.Punch_OutAtSourceHardware,
                        OutAtSourceHostname = result.Punch_OutAtSourceHostname,
                        OutAtSourceIpAddress = result.Punch_OutAtSourceIpAddress,
                        OutAtSourceOperatingSystem = result.Punch_OutAtSourceOperatingSystem,
                        OutAtSourceOperatingSystemVersion = result.Punch_OutAtSourceOperatingSystemVersion,
                        OutAtSourceBrowser = result.Punch_OutAtSourceBrowser,
                        OutAtSourceBrowserVersion = result.Punch_OutAtSourceBrowserVersion,
                        OutAtSourcePhoneNumber = result.Punch_OutAtSourcePhoneNumber,
                        PayrollRateId = result.Punch_PayrollRateId,
                        ServiceRateId = result.Punch_ServiceRateId,
                        CumulativeMinutes = result.Punch_CumulativeMinutes,
                        PayrollRate = new Rate()
                        {
                            Id = result.PayrollRate_Id.GetValueOrDefault(),
                            IsDeleted = result.PayrollRate_IsDeleted.GetValueOrDefault(),
                            CreatedAt = result.PayrollRate_CreatedAt.GetValueOrDefault(),
                            Name = result.PayrollRate_Name,
                            OrganizationId = result.PayrollRate_OrganizationId.GetValueOrDefault(),
                            ParentRateId = result.PayrollRate_ParentRateId,
                            QBDPayrollItem = result.PayrollRate_QBDPayrollItem,
                            QBOPayrollItem = result.PayrollRate_QBOPayrollItem,
                            Type = result.PayrollRate_Type
                        },
                        ServiceRate = new Rate()
                        {
                            Id = result.ServiceRate_Id.GetValueOrDefault(),
                            IsDeleted = result.ServiceRate_IsDeleted.GetValueOrDefault(),
                            CreatedAt = result.ServiceRate_CreatedAt.GetValueOrDefault(),
                            Name = result.ServiceRate_Name,
                            OrganizationId = result.ServiceRate_OrganizationId.GetValueOrDefault(),
                            ParentRateId = result.ServiceRate_ParentRateId,
                            QBDServiceItem = result.ServiceRate_QBDServiceItem,
                            QBOServiceItem = result.ServiceRate_QBOServiceItem,
                            Type = result.ServiceRate_Type
                        },
                        Task = new Task()
                        {
                            Id = result.Task_Id,
                            CreatedAt = result.Task_CreatedAt,
                            JobId = result.Task_JobId,
                            Name = result.Task_Name,
                            Number = result.Task_Number,
                            QuickBooksPayrollItem = result.Task_QuickBooksPayrollItem,
                            QuickBooksServiceItem = result.Task_QuickBooksServiceItem,
                            BaseServiceRateId = result.Task_BaseServiceRateId,
                            BasePayrollRateId = result.Task_BasePayrollRateId,
                            Job = new Job()
                            {
                                Id = result.Job_Id,
                                CreatedAt = result.Job_CreatedAt,
                                CustomerId = result.Job_CustomerId,
                                Description = result.Job_Description,
                                Name = result.Job_Name,
                                Number = result.Job_Number,
                                QuickBooksCustomerJob = result.Job_QuickBooksCustomerJob,
                                Customer = new Customer()
                                {
                                    Id = result.Customer_Id,
                                    CreatedAt = result.Customer_CreatedAt,
                                    Description = result.Customer_Description,
                                    Name = result.Customer_Name,
                                    Number = result.Customer_Number,
                                    OrganizationId = result.Customer_OrganizationId
                                }
                            }
                        },
                        User = new User()
                        {
                            Id = result.User_Id,
                            CreatedAt = result.User_CreatedAt,
                            EmailAddress = result.User_EmailAddress,
                            IsDeleted = result.User_IsDeleted,
                            Name = result.User_Name,
                            OrganizationId = result.User_OrganizationId,
                            Role = result.User_Role,
                            TimeZone = result.User_TimeZone,
                            UsesMobileClock = result.User_UsesMobileClock,
                            UsesTouchToneClock = result.User_UsesTouchToneClock,
                            UsesWebClock = result.User_UsesWebClock,
                            UsesTimesheets = result.User_UsesTimesheets,
                            RequiresLocation = result.User_RequiresLocation,
                            RequiresPhoto = result.User_RequiresPhoto,
                            QBOGivenName = result.User_QBOGivenName,
                            QBOMiddleName = result.User_QBOMiddleName,
                            QBOFamilyName = result.User_QBOFamilyName,
                            AllowedPhoneNumbers = result.User_AllowedPhoneNumbers,
                            QuickBooksEmployee = result.User_QuickBooksEmployee
                        }
                    });
                }

                connection.Close();
            }

            // Determine page count.
            int pageCount = total > 0
                ? (int)Math.Ceiling(total / (double)pageSize)
                : 0;

            // Create the response
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(punches, settings),
                    System.Text.Encoding.UTF8,
                    "application/json")
            };

            // Set headers for paging.
            //response.Headers.Add("X-Paging-PageNumber", pageNumber.ToString(CultureInfo.InvariantCulture));
            response.Headers.Add("X-Paging-PageSize", pageSize.ToString(CultureInfo.InvariantCulture));
            response.Headers.Add("X-Paging-PageCount", pageCount.ToString(CultureInfo.InvariantCulture));
            response.Headers.Add("X-Paging-TotalRecordCount", total.ToString(CultureInfo.InvariantCulture));

            return response;
        }

        //// GET: api/PunchesExpanded/5
        //[ResponseType(typeof(Punch))]
        //public IHttpActionResult GetPunch(int id)
        //{
        //    Punch punch = _context.Punches.Find(id);
        //    if (punch == null)
        //    {
        //        return NotFound();
        //    }

        //    return Ok(punch);
        //}

        //// PUT: api/PunchesExpanded/5
        //[ResponseType(typeof(void))]
        //public IHttpActionResult PutPunch(int id, Punch punch)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    if (id != punch.Id)
        //    {
        //        return BadRequest();
        //    }

        //    _context.Entry(punch).State = EntityState.Modified;

        //    try
        //    {
        //        _context.SaveChanges();
        //    }
        //    catch (DbUpdateConcurrencyException)
        //    {
        //        if (!PunchExists(id))
        //        {
        //            return NotFound();
        //        }
        //        else
        //        {
        //            throw;
        //        }
        //    }

        //    return StatusCode(HttpStatusCode.NoContent);
        //}

        //// POST: api/PunchesExpanded
        //[ResponseType(typeof(Punch))]
        //public IHttpActionResult PostPunch(Punch punch)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    _context.Punches.Add(punch);
        //    _context.SaveChanges();

        //    return CreatedAtRoute("DefaultApi", new { id = punch.Id }, punch);
        //}

        //// DELETE: api/PunchesExpanded/5
        //[ResponseType(typeof(Punch))]
        //public IHttpActionResult DeletePunch(int id)
        //{
        //    Punch punch = _context.Punches.Find(id);
        //    if (punch == null)
        //    {
        //        return NotFound();
        //    }

        //    _context.Punches.Remove(punch);
        //    _context.SaveChanges();

        //    return Ok(punch);
        //}

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            base.Dispose(disposing);
        }

        //private bool PunchExists(int id)
        //{
        //    return _context.Punches.Count(e => e.Id == id) > 0;
        //}

        public User CurrentUser()
        {
            if (ActionContext.RequestContext.Principal.Identity.Name.Length > 0)
            {
                var currentUserId = int.Parse(ActionContext.RequestContext.Principal.Identity.Name);
                return _context.Users
                    .Where(u => u.Id == currentUserId)
                    .FirstOrDefault();
            }
            else
            {
                return null;
            }
        }
    }

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

        public string Punch_SourceForInAt { get; set; }

        public string Punch_SourceForOutAt { get; set; }

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