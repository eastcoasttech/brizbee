//
//  JobsExpandedController.cs
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
using Brizbee.Core.Serialization;
using Brizbee.Core.Serialization.Statistics;
using CsvHelper;
using CsvHelper.Configuration;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Net;
using System.Text;

namespace Brizbee.Api.Controllers
{
    public class JobsExpandedController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly SqlContext _context;

        public JobsExpandedController(IConfiguration configuration, SqlContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        // GET: api/JobsExpanded
        [HttpGet("api/JobsExpanded")]
        public IActionResult GetJobs(
            [FromQuery] int skip = 0, [FromQuery] int pageSize = 1000,
            [FromQuery] string orderBy = "JOBS/NAME", [FromQuery] string orderByDirection = "ASC",
            [FromQuery] int[] jobIds = null, [FromQuery] string[] jobNumbers = null, [FromQuery] string[] jobNames = null,
            [FromQuery] int[] customerIds = null, [FromQuery] string[] customerNumbers = null, [FromQuery] string[] customerNames = null)
        {
            if (pageSize > 1000) { BadRequest(); }

            var currentUser = CurrentUser();

            // Ensure that user is authorized.
            if (!currentUser.CanViewProjects)
                return Forbid();

            var total = 0;
            var jobs = new List<Job>();
            using (var connection = new SqlConnection(_configuration.GetConnectionString("SqlContext")))
            {
                connection.Open();

                // Determine the order by columns.
                var orderByFormatted = "";
                switch (orderBy.ToUpperInvariant())
                {
                    case "JOBS/CREATEDAT":
                        orderByFormatted = "[J].[CreatedAt]";
                        break;
                    case "JOBS/NUMBER":
                        orderByFormatted = "[J].[Number]";
                        break;
                    case "JOBS/NAME":
                        orderByFormatted = "[J].[Name]";
                        break;
                    case "CUSTOMERS/NUMBER":
                        orderByFormatted = "[C].[Number]";
                        break;
                    case "CUSTOMERS/NAME":
                        orderByFormatted = "[C].[Name]";
                        break;
                    default:
                        orderByFormatted = "[J].[Name]";
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

                var whereClauses = "";
                var parameters = new DynamicParameters();

                // Common clause.
                parameters.Add("@OrganizationId", currentUser.OrganizationId);

                // Clause for job ids.
                if (jobIds != null && jobIds.Length > 0)
                {
                    whereClauses += $" AND [J].[Id] IN ({string.Join(",", jobIds)})";
                }

                // Clause for job numbers.
                if (jobNumbers != null && jobNumbers.Length > 0)
                {
                    whereClauses += $" AND [J].[Number] IN ({string.Join(",", jobNumbers.Select(x => string.Format("'{0}'", x)))})";
                }

                // Clause for job names.
                if (jobNames != null && jobNames.Length > 0)
                {
                    whereClauses += $" AND [J].[Name] IN ({string.Join(",", jobNames.Select(x => string.Format("'{0}'", x)))})";
                }

                // Clause for customer ids.
                if (customerIds != null && customerIds.Length > 0)
                {
                    whereClauses += $" AND [C].[Id] IN ({string.Join(",", customerIds)})";
                }

                // Clause for customer numbers.
                if (customerNumbers != null && customerNumbers.Length > 0)
                {
                    whereClauses += $" AND [C].[Number] IN ({string.Join(",", customerNumbers.Select(x => string.Format("'{0}'", x)))})";
                }

                // Clause for customer names.
                if (customerNames != null && customerNames.Length > 0)
                {
                    whereClauses += $" AND [C].[Name] IN ({string.Join(",", customerNames.Select(x => string.Format("'{0}'", x)))})";
                }

                // Get the count.
                var countSql = $@"
                    SELECT
                        COUNT(*)
                    FROM
                        [Jobs] AS [J]
                    INNER JOIN
                        [Customers] AS [C] ON [J].[CustomerId] = [C].[Id]
                    WHERE
                        [C].[OrganizationId] = @OrganizationId {whereClauses};";

                total = connection.QuerySingle<int>(countSql, parameters);

                // Paging parameters.
                parameters.Add("@Skip", skip);
                parameters.Add("@PageSize", pageSize);

                // Get the records.
                var recordsSql = $@"
                    SELECT
                        [J].[Id] AS [Job_Id],
                        [J].[CreatedAt] AS [Job_CreatedAt],
                        [J].[Name] AS [Job_Name],
                        [J].[Number] AS [Job_Number],
                        [J].[Description] AS [Job_Description],
                        [J].[QuickBooksCustomerJob] AS [Job_QuickBooksCustomerJob],
                        [J].[QuoteNumber] AS [Job_QuoteNumber],
                        [J].[CustomerId] AS [Job_CustomerId],

                        [C].[Id] AS [Customer_Id],
                        [C].[CreatedAt] AS [Customer_CreatedAt],
                        [C].[Name] AS [Customer_Name],
                        [C].[Number] AS [Customer_Number],
                        [C].[Description] AS [Customer_Description],
                        [C].[OrganizationId] AS [Customer_OrganizationId]
                    FROM
                        [Jobs] AS [J]
                    INNER JOIN
                        [Customers] AS [C] ON [J].[CustomerId] = [C].[Id]
                    WHERE
                        [C].[OrganizationId] = @OrganizationId {whereClauses}
                    ORDER BY
                        {orderByFormatted} {orderByDirectionFormatted}
                    OFFSET @Skip ROWS
                    FETCH NEXT @PageSize ROWS ONLY;";

                var results = connection.Query<JobExpanded>(recordsSql, parameters);

                foreach (var result in results)
                {
                    jobs.Add(new Job()
                    {
                        Id = result.Job_Id,
                        CreatedAt = result.Job_CreatedAt,
                        Name = result.Job_Name,
                        Number = result.Job_Number,
                        Description = result.Job_Description,
                        QuickBooksCustomerJob = result.Job_QuickBooksCustomerJob,
                        QuoteNumber = result.Job_QuoteNumber,
                        CustomerId = result.Job_CustomerId,

                        Customer = new Customer()
                        {
                            Id = result.Customer_Id,
                            CreatedAt = result.Customer_CreatedAt,
                            Name = result.Customer_Name,
                            Number = result.Customer_Number,
                            Description = result.Customer_Description,
                            OrganizationId = result.Customer_OrganizationId
                        }
                    });
                }

                connection.Close();
            }

            // Determine page count.
            int pageCount = total > 0
                ? (int)Math.Ceiling(total / (double)pageSize)
                : 0;

            // Set headers for paging.
            HttpContext.Response.Headers.Add("X-Paging-PageSize", pageSize.ToString(CultureInfo.InvariantCulture));
            HttpContext.Response.Headers.Add("X-Paging-PageCount", pageCount.ToString(CultureInfo.InvariantCulture));
            HttpContext.Response.Headers.Add("X-Paging-TotalRecordCount", total.ToString(CultureInfo.InvariantCulture));

            return new JsonResult(jobs)
            {
                StatusCode = (int)HttpStatusCode.OK
            };
        }

        // GET: api/JobsExpanded/Export
        [HttpGet("api/JobsExpanded/Export")]
        public IActionResult Export([FromQuery] string filterStatus)
        {
            var currentUser = CurrentUser();

            string[] statusFilters;

            switch (filterStatus.ToUpperInvariant())
            {
                case "OPEN":
                    statusFilters = new string[] { "Open", "Needs Invoice" };
                    break;
                default:
                    statusFilters = new string[] { "Merged", "Closed" };
                    break;
            }

            var jobs = _context.Jobs
                .Include(j => j.Customer)
                .Where(j => j.Customer!.OrganizationId == currentUser.OrganizationId)
                .Where(j => statusFilters.Contains(j.Status))
                .Select(j => new
                {
                    CustomerNumber = j.Customer!.Number,
                    CustomerName = j.Customer.Name,
                    ProjectNumber = j.Number,
                    ProjectName = j.Name,
                    j.Status,
                    j.QuoteNumber,
                    j.CustomerWorkOrder,
                    j.CustomerPurchaseOrder,
                    j.InvoiceNumber
                })
                .ToList();

            var configuration = new CsvConfiguration(CultureInfo.CurrentCulture)
            {
                Delimiter = ","
            };
            using (var writer = new StringWriter())
            using (var csv = new CsvWriter(writer, configuration))
            {
                csv.WriteRecords(jobs);

                var bytes = Encoding.UTF8.GetBytes(writer.ToString());
                return File(bytes, "text/csv", fileDownloadName: $"{filterStatus.ToUpperInvariant()} PROJECTS - {DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}.csv");
            }
        }
        
        // GET: api/JobsExpanded/5/Statistics
        [HttpGet("api/JobsExpanded/{id:int}/Statistics")]
        public IActionResult Statistics(int id)
        {
            var project = _context.Jobs.Find(id);

            if (project == null)
                return NotFound();
            
            // Collect the statistics counting total minutes.
            var projectMinutesCountSql = @"
                SELECT
                    SUM(DATEDIFF(MINUTE, [P].[InAt], [P].[OutAt]))
                FROM [dbo].[Punches] AS [P]
                INNER JOIN [dbo].[Tasks] AS [T]
                    ON [T].[Id] = [P].[TaskId]
                WHERE
                    [T].[JobId] = @ProjectId;";

            var projectMinutesCount = _context.Database.GetDbConnection().QuerySingle<long>(projectMinutesCountSql, new
            {
                ProjectId = id
            });
            
            // Collect the statistics counting minutes per task.
            var taskMinutesCountsSql = @"
                SELECT
                    [P].[TaskId],
                    [T].[Number] AS [TaskNumber],
                    [T].[Name] AS [TaskName],
                    SUM(DATEDIFF(MINUTE, [P].[InAt], [P].[OutAt])) AS [MinutesCount]
                FROM [dbo].[Punches] AS [P]
                INNER JOIN [dbo].[Tasks] AS [T]
                    ON [T].[Id] = [P].[TaskId]
                WHERE
                    [T].[JobId] = @ProjectId
                GROUP BY
                    [P].[TaskId],
                    [T].[Number],
                    [T].[Name];";

            var taskMinutesCounts = _context.Database.GetDbConnection().Query<TaskStatistics>(taskMinutesCountsSql, new
            {
                ProjectId = id
            });
            
            // Collect the statistics counting minutes per user.
            var userMinutesCountsSql = @"
                SELECT
                    [P].[UserId],
                    [U].[Name] AS [UserName],
                    SUM(DATEDIFF(MINUTE, [P].[InAt], [P].[OutAt])) AS [MinutesCount]
                FROM [dbo].[Punches] AS [P]
                INNER JOIN [dbo].[Tasks] AS [T]
                    ON [T].[Id] = [P].[TaskId]
                INNER JOIN [dbo].[Users] AS [U]
                    ON [U].[Id] = [P].[UserId]
                WHERE
                    [T].[JobId] = @ProjectId
                GROUP BY
                    [P].[UserId],
                    [U].[Name];";

            var userMinutesCounts = _context.Database.GetDbConnection().Query<UserStatistics>(userMinutesCountsSql, new
            {
                ProjectId = id
            });
            
            // Collect the statistics counting minutes per date.
            var dateMinutesCountsSql = @"
                SELECT
                    CAST([P].[InAt] AS DATE) AS [Date],
                    SUM(DATEDIFF(MINUTE, [P].[InAt], [P].[OutAt])) AS [MinutesCount]
                FROM [dbo].[Punches] AS [P]
                INNER JOIN [dbo].[Tasks] AS [T]
                    ON [T].[Id] = [P].[TaskId]
                WHERE
                    [T].[JobId] = @ProjectId
                GROUP BY
                    CAST([P].[InAt] AS DATE);";

            var dateMinutesCounts = _context.Database.GetDbConnection().Query<DateStatistics>(dateMinutesCountsSql, new
            {
                ProjectId = id
            });
            
            // Collect the nunber of days worked.
            var workedDaysCountSql = @"
                SELECT
                    COUNT( [X].[TheDate] )
                FROM
                (
                    SELECT
                        [DTS].[TheDate]
                    FROM [dbo].[Punches] AS [P]
                    INNER JOIN [dbo].[Tasks] AS [T] ON
                        [T].[Id] = [P].[TaskId]
                    CROSS APPLY tvf_DatesInRange( CAST([P].[InAt] AS DATE), CAST([P].[OutAt] AS DATE)) AS [DTS]
                    WHERE
                        [T].[JobId] = @ProjectId
                    GROUP BY
                        [DTS].[TheDate]
                ) AS [X];";
            
            var workedDaysCount = _context.Database.GetDbConnection().QuerySingleOrDefault<int>(workedDaysCountSql, new
            {
                ProjectId = id
            });
            
            // Collect the nunber of days in the project duration.
            var durationDaysCountSql = @"
                SELECT
                    DATEDIFF( DAY, CAST(MIN( [P].[InAt] ) AS DATE), DATEADD( DAY, 1, CAST(MAX( [P].[OutAt] ) AS DATE)))
                FROM [dbo].[Punches] AS [P]
                INNER JOIN [dbo].[Tasks] AS [T] ON
                    [T].[Id] = [P].[TaskId]
                WHERE
                    [T].[JobId] = @ProjectId;";
            
            var durationDaysCount = _context.Database.GetDbConnection().QuerySingleOrDefault<int>(durationDaysCountSql, new
            {
                ProjectId = id
            });

            return Ok(new ProjectStatistics()
            {
                MinutesCount = projectMinutesCount,
                TaskStatistics = taskMinutesCounts.ToList(),
                UserStatistics = userMinutesCounts.ToList(),
                DateStatistics = dateMinutesCounts.ToList(),
                WorkedDaysCount = workedDaysCount,
                DurationDaysCount = durationDaysCount
            });
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