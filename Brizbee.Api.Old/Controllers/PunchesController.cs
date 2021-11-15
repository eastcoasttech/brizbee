//
//  PunchesController.cs
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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Brizbee.Api.Controllers
{
    [ApiController]
    [Authorize]
    public class PunchesController : ControllerBase
    {
        private readonly SqlContext _context;
        private readonly ILogger<PunchesController> _logger;
        private readonly IConfiguration Configuration;

        public PunchesController(ILogger<PunchesController> logger, SqlContext context, IConfiguration configuration)
        {
            _logger = logger;
            _context = context;
            Configuration = configuration;
        }

        // GET: api/Punches
        [HttpGet("api/Punches")]
        public IActionResult GetPunches([FromQuery] DateTime inAt, [FromQuery] DateTime outAt, [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 1000, [FromQuery] string orderBy = "Punches/InAt",
            [FromQuery] int[] jobIds = null, [FromQuery] string[] jobNames = null,
            [FromQuery] int[] taskIds = null, [FromQuery] string[] taskNames = null,
            [FromQuery] int[] customerIds = null, [FromQuery] string[] customerNames = null)
        {
            if (pageSize > 1000) { return BadRequest(); }

            var currentUser = CurrentUser();

            // Ensure Administrator.
            if (currentUser.Role != "Administrator")
                return BadRequest();

            // Determine the number of records to skip.
            int skip = (pageNumber - 1) * pageSize;

            var punches = _context.Punches
                .Include(p => p.ServiceRate)
                .Include(p => p.PayrollRate)
                .Include(p => p.Task)
                .Include(p => p.Task.Job)
                .Include(p => p.Task.Job.Customer)
                .Include(p => p.User)
                .Where(p => p.InAt >= inAt)
                .Where(p => p.OutAt <= outAt)
                .OrderBy(p => p.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .ToList();

            var total = _context.Punches
                .Where(p => p.InAt >= inAt)
                .Where(p => p.OutAt <= outAt)
                .Count();

            //var total = 0;
            //List<Punch> punches = new List<Punch>();
            //using (var connection = new SqlConnection(Configuration.GetConnectionString("SqlContext")))
            //{
            //    connection.Open();

            //    // Determine the order by columns.
            //    var orderByFormatted = "";
            //    switch (orderBy.ToUpperInvariant())
            //    {
            //        case "PUNCHES/CREATEDAT":
            //            orderByFormatted = "P.[CreatedAt]";
            //            break;
            //        case "PUNCHES/INAT":
            //            orderByFormatted = "P.[InAt]";
            //            break;
            //        case "PUNCHES/OUTAT":
            //            orderByFormatted = "P.[OutAt]";
            //            break;
            //        default:
            //            orderByFormatted = "P.[CreatedAt]";
            //            break;
            //    }

            //    var whereClause = "";
            //    var parameters = new DynamicParameters();

            //    // Common clause.
            //    parameters.Add("@InAt", inAt);
            //    parameters.Add("@OutAt", outAt);

            //    // Clause for job ids.
            //    if (jobIds != null && jobIds.Any())
            //    {
            //        whereClause += "AND J.[Id] IN (@JobIds)";
            //        parameters.Add("@JobIds", jobIds);
            //    }

            //    // Clause for job names.
            //    if (jobNames != null && jobNames.Any())
            //    {
            //        whereClause += "AND J.[Name] IN (@JobNames)";
            //        parameters.Add("@JobNames", jobNames);
            //    }

            //    // Clause for task ids.
            //    if (taskIds != null && taskIds.Any())
            //    {
            //        whereClause += "AND T.[Id] IN (@TaskIds)";
            //        parameters.Add("@TaskIds", taskIds);
            //    }

            //    // Clause for task names.
            //    if (taskNames != null && taskNames.Any())
            //    {
            //        whereClause += "AND T.[Name] IN (@TaskNames)";
            //        parameters.Add("@TaskNames", taskNames);
            //    }

            //    // Clause for customer ids.
            //    if (customerIds != null && customerIds.Any())
            //    {
            //        whereClause += "AND C.[Id] IN (@CustomerIds)";
            //        parameters.Add("@CustomerIds", customerIds);
            //    }

            //    // Clause for customer names.
            //    if (customerNames != null && customerNames.Any())
            //    {
            //        whereClause += "AND C.[Name] IN (@CustomerNames)";
            //        parameters.Add("@CustomerNames", customerNames);
            //    }

            //    // Get the count.
            //    var countSql = $@"
            //        SELECT
            //            COUNT(*)
            //        FROM
            //            [Punches] AS P
            //        INNER JOIN
            //            [Tasks] AS T ON P.[TaskId] = T.[Id]
            //        INNER JOIN
            //            [Jobs] AS J ON T.[JobId] = J.[Id]
            //        INNER JOIN
            //            [Customers] AS C ON J.[CustomerId] = C.[Id]
            //        WHERE
            //            P.[InAt] >= @InAt AND P.[OutAt] <= @OutAt {whereClause};";

            //    total = connection.QuerySingle<int>(countSql, parameters);

            //    // Paging parameters.
            //    parameters.Add("@Skip", skip);
            //    parameters.Add("@PageSize", pageSize);

            //    // Get the records.
            //    var recordsSql = $@"
            //        SELECT
            //            P.[Id] AS Punch_Id,
            //            P.[CommitId] AS Punch_CommitId,
            //            P.[CreatedAt] AS Punch_CreatedAt,
            //            P.[Guid] AS Punch_Guid,
            //            P.[InAt] AS Punch_InAt,
            //            P.[InAtTimeZone] AS Punch_InAtTimeZone,
            //            P.[LatitudeForInAt] AS Punch_LatitudeForInAt,
            //            P.[LongitudeForInAt] AS Punch_LongitudeForInAt,
            //            P.[LatitudeForOutAt] AS Punch_LatitudeForOutAt,
            //            P.[LongitudeForOutAt] AS Punch_LongitudeForOutAt,
            //            P.[OutAt] AS Punch_OutAt,
            //            P.[OutAtTimeZone] AS Punch_OutAtTimeZone,
            //            P.[SourceForInAt] AS Punch_SourceForInAt,
            //            P.[SourceForOutAt] AS Punch_SourceForOutAt,
            //            P.[TaskId] AS Punch_TaskId,
            //            P.[UserId] AS Punch_UserId,
            //            P.[InAtSourceHardware] AS Punch_InAtSourceHardware,
            //            P.[InAtSourceHostname] AS Punch_InAtSourceHostname,
            //            P.[InAtSourceIpAddress] AS Punch_InAtSourceIpAddress,
            //            P.[InAtSourceOperatingSystem] AS Punch_InAtSourceOperatingSystem,
            //            P.[InAtSourceOperatingSystemVersion] AS Punch_InAtSourceOperatingSystemVersion,
            //            P.[InAtSourceBrowser] AS Punch_InAtSourceBrowser,
            //            P.[InAtSourceBrowserVersion] AS Punch_InAtSourceBrowserVersion,
            //            P.[InAtSourcePhoneNumber] AS Punch_InAtSourcePhoneNumber,
            //            P.[OutAtSourceHardware] AS Punch_OutAtSourceHardware,
            //            P.[OutAtSourceHostname] AS Punch_OutAtSourceHostname,
            //            P.[OutAtSourceIpAddress] AS Punch_OutAtSourceIpAddress,
            //            P.[OutAtSourceOperatingSystem] AS Punch_OutAtSourceOperatingSystem,
            //            P.[OutAtSourceOperatingSystemVersion] AS Punch_OutAtSourceOperatingSystemVersion,
            //            P.[OutAtSourceBrowser] AS Punch_OutAtSourceBrowser,
            //            P.[OutAtSourceBrowserVersion] AS Punch_OutAtSourceBrowserVersion,
            //            P.[OutAtSourcePhoneNumber] AS Punch_OutAtSourcePhoneNumber,
            //            P.[ServiceRateId] AS Punch_ServiceRateId,
            //            P.[PayrollRateId] AS Punch_PayrollRateId,



            //            C.[Id] AS Customer_Id,
            //            C.[CreatedAt] AS Customer_CreatedAt,
            //            C.[Description] AS Customer_Description,
            //            C.[Name] AS Customer_Name,
            //            C.[Number] AS Customer_Number,
            //            C.[OrganizationId] AS Customer_OrganizationId,

            //            J.[Id] AS Job_Id,
            //            J.[CreatedAt] AS Job_CreatedAt,
            //            J.[CustomerId] AS Job_CustomerId,
            //            J.[Description] AS Job_Description,
            //            J.[Name] AS Job_Name,
            //            J.[Number] AS Job_Number,
            //            J.[QuickBooksCustomerJob] AS Job_QuickBooksCustomerJob,

            //            T.[Id] AS Task_Id,
            //            T.[CreatedAt] AS Task_CreatedAt,
            //            T.[JobId] AS Task_JobId,
            //            T.[Name] AS Task_Name,
            //            T.[Number] AS Task_Number,
            //            T.[QuickBooksPayrollItem] AS Task_QuickBooksPayrollItem,
            //            T.[QuickBooksServiceItem] AS Task_QuickBooksServiceItem,
            //            T.[BaseServiceRateId] AS Task_BaseServiceRateId,
            //            T.[BasePayrollRateId] AS Task_BasePayrollRateId,

            //            U.[Id] AS User_Id,
            //            U.[CreatedAt] AS User_CreatedAt,
            //            U.[EmailAddress] AS User_EmailAddress,
            //            U.[IsDeleted] AS User_IsDeleted,
            //            U.[Name] AS User_Name,
            //            U.[OrganizationId] AS User_OrganizationId,
            //            U.[Role] AS User_Role,
            //            U.[TimeZone] AS User_TimeZone,
            //            U.[UsesMobileClock] AS User_UsesMobileClock,
            //            U.[UsesTouchToneClock] AS User_UsesTouchToneClock,
            //            U.[UsesWebClock] AS User_UsesWebClock,
            //            U.[UsesTimesheets] AS User_UsesTimesheets,
            //            U.[RequiresLocation] AS User_RequiresLocation,
            //            U.[RequiresPhoto] AS User_RequiresPhoto,
            //            U.[QBOGivenName] AS User_QBOGivenName,
            //            U.[QBOMiddleName] AS User_QBOMiddleName,
            //            U.[QBOFamilyName] AS User_QBOFamilyName
            //        FROM
            //            [Punches] AS P
            //        INNER JOIN
            //            [Tasks] AS T ON P.[TaskId] = T.[Id]
            //        INNER JOIN
            //            [Jobs] AS J ON T.[JobId] = J.[Id]
            //        INNER JOIN
            //            [Customers] AS C ON J.[CustomerId] = C.[Id]
            //        INNER JOIN
            //            [Users] AS U ON P.[UserId] = U.[Id]
            //        WHERE
            //         P.[InAt] >= @InAt AND P.[OutAt] <= @OutAt {whereClause}
            //        ORDER BY
            //         {orderByFormatted}
            //        OFFSET @Skip ROWS
            //        FETCH NEXT @PageSize ROWS ONLY;";

            //    var results = connection.Query<PunchTaskJobCustomerUser>(recordsSql, parameters);

            //    foreach (var result in results)
            //    {
            //        punches.Add(new Punch()
            //        {
            //            Id = result.Punch_Id,
            //            CommitId = result.Punch_CommitId,
            //            CreatedAt = result.Punch_CreatedAt,
            //            Guid = result.Punch_Guid,
            //            InAt = result.Punch_InAt,
            //            OutAt = result.Punch_OutAt,
            //            TaskId = result.Punch_TaskId,
            //            UserId = result.Punch_UserId,
            //            PayrollRateId = result.Punch_PayrollRateId,
            //            ServiceRateId = result.Punch_ServiceRateId,
            //            PayrollRate = new Rate()
            //            {
            //                Id = 
            //            },
            //            ServiceRate = new Rate()
            //            {
            //            },
            //            Task = new Task()
            //            {
            //                Id = result.Task_Id,
            //                Name = result.Task_Name,
            //                Number = result.Task_Number,
            //                BasePayrollRateId = result.Task_BasePayrollRateId,
            //                BaseServiceRateId = result.Task_BaseServiceRateId,
            //                CreatedAt = result.Task_CreatedAt,
            //                Job = new Job()
            //                {
            //                    Id = result.Job_Id,
            //                    Name = result.Job_Name,
            //                    Number = result.Job_Number,
            //                    CreatedAt = result.Job_CreatedAt,
            //                    Description = result.Job_Description,
            //                    QuickBooksCustomerJob = result.Job_QuickBooksCustomerJob,
            //                    Customer = new Customer()
            //                    {
            //                        Id = result.Customer_Id,
            //                        Name = result.Customer_Name,
            //                        Number = result.Customer_Number,
            //                        Description = result.Customer_Description,
            //                        CreatedAt = result.Customer_CreatedAt,
            //                        OrganizationId = result.Customer_OrganizationId
            //                    }
            //                }
            //            },
            //            User = new User()
            //            {
            //                Id = result.User_Id,
            //                Name = result.User_Name,
            //                EmailAddress = result.User_EmailAddress
            //            }
            //        });
            //    }

            //    connection.Close();
            //}

            // Determine page count.
            int pageCount = total > 0
                ? (int)Math.Ceiling(total / (double)pageSize)
                : 0;

            // Set headers for paging.
            Response.Headers.Add("X-Paging-PageNumber", pageNumber.ToString(CultureInfo.InvariantCulture));
            Response.Headers.Add("X-Paging-PageSize", pageSize.ToString(CultureInfo.InvariantCulture));
            Response.Headers.Add("X-Paging-PageCount", pageCount.ToString(CultureInfo.InvariantCulture));
            Response.Headers.Add("X-Paging-TotalRecordCount", total.ToString(CultureInfo.InvariantCulture));

            return Ok(punches);
        }

        // GET: api/Punches/5
        [HttpGet("api/Punches/{id}")]
        public async Task<ActionResult<Punch>> GetPunch(int id)
        {
            var currentUser = CurrentUser();

            // Ensure Administrator.
            if (currentUser.Role != "Administrator")
                return BadRequest();

            // Find within the organization.
            var userIds = _context.Users
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .Select(u => u.Id);
            var punch = await _context.Punches
                .Where(p => userIds.Contains(p.UserId))
                .Where(p => p.Id == id)
                .FirstOrDefaultAsync();

            if (punch == null)
            {
                return NotFound();
            }

            return punch;
        }

        // POST: api/Punches
        [HttpPost("api/Punches")]
        public async Task<ActionResult<Punch>> PostPunch(Punch punch)
        {
            var currentUser = CurrentUser();

            // Ensure Administrator.
            if (currentUser.Role != "Administrator")
                return BadRequest();

            // Ensure the same organization.
            var userIds = _context.Users
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .Select(u => u.Id);

            if (!userIds.Contains(punch.UserId))
                return BadRequest();

            _context.Punches.Add(punch);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetPunch", new { id = punch.Id }, punch);
        }

        // PUT: api/Punches/5
        [HttpPut("api/Punches/{id}")]
        public async Task<ActionResult> PutPunch(int id, Punch patch)
        {
            var currentUser = CurrentUser();

            // Ensure Administrator.
            if (currentUser.Role != "Administrator")
                return BadRequest();

            // Find within the organization.
            var userIds = _context.Users
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .Select(u => u.Id);
            var punch = await _context.Punches
                .Where(p => userIds.Contains(p.UserId))
                .Where(p => p.Id == id)
                .FirstOrDefaultAsync();

            if (punch == null)
            {
                return NotFound();
            }

            // Apply the changes.
            punch.InAt = patch.InAt;
            punch.OutAt = patch.OutAt;

            try
            {
                _context.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw;
            }

            return NoContent();
        }

        // DELETE: api/Punches/5
        [HttpDelete("api/Punches/{id}")]
        public async Task<ActionResult> DeletePunch(int id)
        {
            var currentUser = CurrentUser();

            // Ensure Administrator.
            if (currentUser.Role != "Administrator")
                return BadRequest();

            // Find within the organization.
            var userIds = _context.Users
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .Select(u => u.Id);
            var punch = await _context.Punches
                .Where(p => userIds.Contains(p.UserId))
                .Where(p => p.Id == id)
                .FirstOrDefaultAsync();

            if (punch == null)
            {
                return NotFound();
            }

            _context.Punches.Remove(punch);
            await _context.SaveChangesAsync();

            return NoContent();
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