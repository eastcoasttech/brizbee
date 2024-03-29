﻿//
//  PunchesExpandedController.cs
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

using Brizbee.Api.Serialization.Expanded;
using Brizbee.Core.Models;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Net;

namespace Brizbee.Api.Controllers
{
    public class PunchesExpandedController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly SqlContext _context;

        public PunchesExpandedController(IConfiguration configuration, SqlContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        // GET: api/PunchesExpanded
        [HttpGet("api/PunchesExpanded")]
        public IActionResult GetPunches([FromQuery] DateTime min, [FromQuery] DateTime max,
            [FromQuery] int skip = 0, [FromQuery] int pageSize = 1000,
            [FromQuery] string orderBy = "PUNCHES/INAT", [FromQuery] string orderByDirection = "ASC",
            [FromQuery] int[] jobIds = null, [FromQuery] string[] jobNames = null,
            [FromQuery] int[] taskIds = null, [FromQuery] string[] taskNames = null,
            [FromQuery] int[] customerIds = null, [FromQuery] string[] customerNames = null,
            [FromQuery] int[] userIds = null, [FromQuery] string[] userNames = null)
        {
            if (pageSize > 1000) { return BadRequest(); }

            var currentUser = CurrentUser();

            // Ensure that user is authorized.
            if (!currentUser.CanViewPunches)
                return Forbid();

            var total = 0;
            List<Punch> punches = new List<Punch>(0);
            using (var connection = new SqlConnection(_configuration.GetConnectionString("SqlContext")))
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
                parameters.Add("@Min", new DateTime(min.Year, min.Month, min.Day, 0, 0, 0));
                parameters.Add("@Max", new DateTime(max.Year, max.Month, max.Day, 23, 59, 59));
                parameters.Add("@OrganizationId", currentUser.OrganizationId);

                // Clause for job ids.
                if (jobIds != null && jobIds.Any())
                {
                    whereClause += $" AND J.[Id] IN ({string.Join(",", jobIds)})";
                }

                // Clause for job names.
                if (jobNames != null && jobNames.Any())
                {
                    whereClause += $" AND J.[Name] IN ({string.Join(",", jobNames.Select(x => string.Format("'{0}'", x)))})";
                }

                // Clause for task ids.
                if (taskIds != null && taskIds.Any())
                {
                    whereClause += $" AND T.[Id] IN ({string.Join(",", taskIds)})";
                }

                // Clause for task names.
                if (taskNames != null && taskNames.Any())
                {
                    whereClause += $" AND T.[Name] IN ({string.Join(",", taskNames.Select(x => string.Format("'{0}'", x)))})";
                }

                // Clause for customer ids.
                if (customerIds != null && customerIds.Any())
                {
                    whereClause += $" AND C.[Id] IN ({string.Join(",", customerIds)})";
                }

                // Clause for customer names.
                if (customerNames != null && customerNames.Any())
                {
                    whereClause += $" AND C.[Name] IN ({string.Join(",", customerNames.Select(x => string.Format("'{0}'", x)))})";
                }

                // Clause for user ids.
                if (userIds != null && userIds.Any())
                {
                    whereClause += $" AND U.[Id] IN ({string.Join(",", userIds)})";
                }

                // Clause for user names.
                if (userNames != null && userNames.Any())
                {
                    whereClause += $" AND U.[Name] IN ({string.Join(",", userNames.Select(x => string.Format("'{0}'", x)))})";
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
                        P.[InAt] BETWEEN @Min AND @Max {whereClause};";

                total = connection.QuerySingle<int>(countSql, parameters, commandTimeout: 600);

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

                        DATEDIFF_BIG(minute, P.[InAt], P.[OutAt]) AS Punch_Minutes,
                        
                        SUM(DATEDIFF_BIG(minute, P.[InAt], P.[OutAt])) OVER(PARTITION BY UserId ORDER BY InAt ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS Punch_CumulativeMinutes,

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
                        U.[IsDeleted] = 0 AND
                        P.[InAt] BETWEEN @Min AND @Max {whereClause}
                    ORDER BY
                        {orderByFormatted} {orderByDirectionFormatted}
                        {(orderByFormatted == "P.[InAt]" ? "" : $", P.[InAt] {orderByDirectionFormatted}")}
                    OFFSET @Skip ROWS
                    FETCH NEXT @PageSize ROWS ONLY;";

                var results = connection.Query<PunchExpanded>(recordsSql, parameters, commandTimeout: 600);

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
                        Task = new Brizbee.Core.Models.Task()
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

            Trace.TraceInformation(min.ToString("yyyy-MM-dd HH:mm:ss zz"));
            Trace.TraceInformation(max.ToString("yyyy-MM-dd HH:mm:ss zz"));

            // Determine page count.
            int pageCount = total > 0
                ? (int)Math.Ceiling(total / (double)pageSize)
                : 0;

            // Set headers for paging.
            HttpContext.Response.Headers.Add("X-Paging-PageSize", pageSize.ToString(CultureInfo.InvariantCulture));
            HttpContext.Response.Headers.Add("X-Paging-PageCount", pageCount.ToString(CultureInfo.InvariantCulture));
            HttpContext.Response.Headers.Add("X-Paging-TotalRecordCount", total.ToString(CultureInfo.InvariantCulture));

            return new JsonResult(punches)
            {
                StatusCode = (int)HttpStatusCode.OK
            };
        }

        // GET: api/PunchesExpanded/Current
        [HttpGet("api/PunchesExpanded/Current")]
        [ProducesResponseType(typeof(Punch), StatusCodes.Status200OK)]
        public IActionResult GetCurrent()
        {
            User currentUser = CurrentUser();

            Punch currentPunch = null;
            using (var connection = new SqlConnection(_configuration.GetConnectionString("SqlContext")))
            {
                connection.Open();

                var currentPunchSql = @"
                    SELECT TOP(1)
                        [P].[Id] AS Punch_Id,
                        [P].[CommitId] AS Punch_CommitId,
                        [P].[CreatedAt] AS Punch_CreatedAt,
                        [P].[Guid] AS Punch_Guid,
                        [P].[InAt] AS Punch_InAt,
                        [P].[InAtTimeZone] AS Punch_InAtTimeZone,
                        [P].[LatitudeForInAt] AS Punch_LatitudeForInAt,
                        [P].[LongitudeForInAt] AS Punch_LongitudeForInAt,
                        [P].[LatitudeForOutAt] AS Punch_LatitudeForOutAt,
                        [P].[LongitudeForOutAt] AS Punch_LongitudeForOutAt,
                        [P].[OutAt] AS Punch_OutAt,
                        [P].[OutAtTimeZone] AS Punch_OutAtTimeZone,
                        [P].[TaskId] AS Punch_TaskId,
                        [P].[UserId] AS Punch_UserId,
                        [P].[InAtSourceHardware] AS Punch_InAtSourceHardware,
                        [P].[InAtSourceHostname] AS Punch_InAtSourceHostname,
                        [P].[InAtSourceIpAddress] AS Punch_InAtSourceIpAddress,
                        [P].[InAtSourceOperatingSystem] AS Punch_InAtSourceOperatingSystem,
                        [P].[InAtSourceOperatingSystemVersion] AS Punch_InAtSourceOperatingSystemVersion,
                        [P].[InAtSourceBrowser] AS Punch_InAtSourceBrowser,
                        [P].[InAtSourceBrowserVersion] AS Punch_InAtSourceBrowserVersion,
                        [P].[InAtSourcePhoneNumber] AS Punch_InAtSourcePhoneNumber,
                        [P].[OutAtSourceHardware] AS Punch_OutAtSourceHardware,
                        [P].[OutAtSourceHostname] AS Punch_OutAtSourceHostname,
                        [P].[OutAtSourceIpAddress] AS Punch_OutAtSourceIpAddress,
                        [P].[OutAtSourceOperatingSystem] AS Punch_OutAtSourceOperatingSystem,
                        [P].[OutAtSourceOperatingSystemVersion] AS Punch_OutAtSourceOperatingSystemVersion,
                        [P].[OutAtSourceBrowser] AS Punch_OutAtSourceBrowser,
                        [P].[OutAtSourceBrowserVersion] AS Punch_OutAtSourceBrowserVersion,
                        [P].[OutAtSourcePhoneNumber] AS Punch_OutAtSourcePhoneNumber,

                        [C].[Id] AS Customer_Id,
                        [C].[CreatedAt] AS Customer_CreatedAt,
                        [C].[Description] AS Customer_Description,
                        [C].[Name] AS Customer_Name,
                        [C].[Number] AS Customer_Number,
                        [C].[OrganizationId] AS Customer_OrganizationId,

                        [J].[Id] AS Job_Id,
                        [J].[CreatedAt] AS Job_CreatedAt,
                        [J].[CustomerId] AS Job_CustomerId,
                        [J].[Description] AS Job_Description,
                        [J].[Name] AS Job_Name,
                        [J].[Number] AS Job_Number,

                        [T].[Id] AS Task_Id,
                        [T].[CreatedAt] AS Task_CreatedAt,
                        [T].[JobId] AS Task_JobId,
                        [T].[Name] AS Task_Name,
                        [T].[Number] AS Task_Number
                    FROM
                        [Punches] AS P
                    INNER JOIN
                        [Tasks] AS T ON [P].[TaskId] = [T].[Id]
                    INNER JOIN
                        [Jobs] AS J ON [T].[JobId] = [J].[Id]
                    INNER JOIN
                        [Customers] AS C ON [J].[CustomerId] = [C].[Id]
                    WHERE
                        [P].[UserId] = @UserId AND
                        [P].[OutAt] IS NULL
                    ORDER BY
                        [InAt] DESC;";

                var results = connection.Query<PunchExpanded>(currentPunchSql, new { UserId = currentUser.Id }, commandTimeout: 600);

                if (results.Any())
                {
                    var result = results.FirstOrDefault();
                    currentPunch = new Punch()
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
                        Task = new Brizbee.Core.Models.Task()
                        {
                            Id = result.Task_Id,
                            CreatedAt = result.Task_CreatedAt,
                            JobId = result.Task_JobId,
                            Name = result.Task_Name,
                            Number = result.Task_Number,
                            Job = new Job()
                            {
                                Id = result.Job_Id,
                                CreatedAt = result.Job_CreatedAt,
                                CustomerId = result.Job_CustomerId,
                                Description = result.Job_Description,
                                Name = result.Job_Name,
                                Number = result.Job_Number,
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
                        }
                    };
                }

                connection.Close();
            }

            return Ok(currentPunch);
        }

        private User CurrentUser()
        {
            var type = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";
            var sub = HttpContext.User.Claims.FirstOrDefault(c => c.Type == type).Value;
            var currentUserId = int.Parse(sub);
            return _context.Users
                .Where(u => u.Id == currentUserId)
                .FirstOrDefault();
        }
    }
}