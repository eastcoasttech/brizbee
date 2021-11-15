//
//  TasksExpandedController.cs
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
using System.Globalization;
using System.Net;

namespace Brizbee.Api.Controllers
{
    public class TasksExpandedController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly SqlContext _context;

        public TasksExpandedController(IConfiguration configuration, SqlContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        // GET: api/TasksExpanded/ForPunches
        [HttpGet("api/TasksExpanded/ForPunches")]
        public IActionResult GetTasksForPunches([FromQuery] DateTime min, [FromQuery] DateTime max,
            [FromQuery] string orderBy = "TASKS/NUMBER", [FromQuery] string orderByDirection = "ASC")
        {
            if ((max - min).TotalDays > 90)
                return BadRequest();

            var currentUser = CurrentUser();

            // Ensure that user is authorized.
            if (!currentUser.CanSplitAndPopulatePunches)
                return Forbid();

            var total = 0;
            List<Brizbee.Core.Models.Task> tasks = new List<Brizbee.Core.Models.Task>(0);
            using (var connection = new SqlConnection(_configuration.GetConnectionString("SqlContext")))
            {
                connection.Open();

                // Determine the order by columns.
                var orderByFormatted = "";
                switch (orderBy.ToUpperInvariant())
                {
                    case "JOBS/NUMBER":
                        orderByFormatted = "[J].[Number]";
                        break;
                    case "JOBS/NAME":
                        orderByFormatted = "[J].[Name]";
                        break;
                    case "TASKS/NUMBER":
                        orderByFormatted = "[T].[Number]";
                        break;
                    case "TASKS/NAME":
                        orderByFormatted = "[T].[Name]";
                        break;
                    default:
                        orderByFormatted = "[T].[NUMBER]";
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

                // Get the count.
                var countSql = $@"
                    SELECT
                        COUNT(DISTINCT([P].[TaskId]))
                    FROM
                        [Punches] AS [P]
                    INNER JOIN
                        [Users] AS [U] ON [P].[UserId] = [U].[Id]
                    WHERE
	                    [U].[OrganizationId] = @OrganizationId AND
	                    CAST([P].[InAt] AS DATE) BETWEEN @Min AND @Max;";

                total = connection.QueryFirst<int>(countSql, new
                {
                    OrganizationId = currentUser.OrganizationId,
                    Min = min,
                    Max = max
                });

                // Get the records.
                var recordsSql = $@"
                    SELECT
                        [T].[Id] AS Task_Id,
                        [T].[CreatedAt] AS Task_CreatedAt,
                        [T].[JobId] AS Task_JobId,
                        [T].[Name] AS Task_Name,
                        [T].[Number] AS Task_Number,
                        [T].[QuickBooksPayrollItem] AS Task_QuickBooksPayrollItem,
                        [T].[QuickBooksServiceItem] AS Task_QuickBooksServiceItem,
                        [T].[BaseServiceRateId] AS Task_BaseServiceRateId,
                        [T].[BasePayrollRateId] AS Task_BasePayrollRateId,

                        [J].[Id] AS Job_Id,
                        [J].[CreatedAt] AS Job_CreatedAt,
                        [J].[CustomerId] AS Job_CustomerId,
                        [J].[Description] AS Job_Description,
                        [J].[Name] AS Job_Name,
                        [J].[Number] AS Job_Number,
                        [J].[QuickBooksCustomerJob] AS Job_QuickBooksCustomerJob,

                        [C].[Id] AS Customer_Id,
                        [C].[CreatedAt] AS Customer_CreatedAt,
                        [C].[Description] AS Customer_Description,
                        [C].[Name] AS Customer_Name,
                        [C].[Number] AS Customer_Number,
                        [C].[OrganizationId] AS Customer_OrganizationId,

                        [PR].[Id] AS BasePayrollRate_Id,
                        [PR].[CreatedAt] AS BasePayrollRate_CreatedAt,
                        [PR].[IsDeleted] AS BasePayrollRate_IsDeleted,
                        [PR].[Name] AS BasePayrollRate_Name,
                        [PR].[OrganizationId] AS BasePayrollRate_OrganizationId,
                        [PR].[ParentRateId] AS BasePayrollRate_ParentRateId,
                        [PR].[QBDPayrollItem] AS BasePayrollRate_QBDPayrollItem,
                        [PR].[QBOPayrollItem] AS BasePayrollRate_QBOPayrollItem,
                        [PR].[Type] AS BasePayrollRate_Type,

                        [SR].[Id] AS BaseServiceRate_Id,
                        [SR].[CreatedAt] AS BaseServiceRate_CreatedAt,
                        [SR].[IsDeleted] AS BaseServiceRate_IsDeleted,
                        [SR].[Name] AS BaseServiceRate_Name,
                        [SR].[OrganizationId] AS BaseServiceRate_OrganizationId,
                        [SR].[ParentRateId] AS BaseServiceRate_ParentRateId,
                        [SR].[QBDServiceItem] AS BaseServiceRate_QBDServiceItem,
                        [SR].[QBOServiceItem] AS BaseServiceRate_QBOServiceItem,
                        [SR].[Type] AS BaseServiceRate_Type
                    FROM
                        [Tasks] AS [T]
                    INNER JOIN
                        [Jobs] AS [J] ON [J].[Id] = [T].[JobId]
                    INNER JOIN
                        [Customers] AS [C] ON [C].[Id] = [J].[CustomerId]
                    LEFT OUTER JOIN
                        [Rates] AS [PR] ON [T].[BasePayrollRateId] = [PR].[Id]
                    LEFT OUTER JOIN
                        [Rates] AS [SR] ON [T].[BaseServiceRateId] = [SR].[Id]
                    WHERE
                        [T].[Id] IN
                        (
                            SELECT
	                            [T].[Id]
                            FROM
	                            [Punches] AS [P]
                            INNER JOIN
	                            [Tasks] AS [T] ON [T].[Id] = [P].[TaskId]
                            INNER JOIN
	                            [Users] AS [U] ON [U].[Id] = [P].[UserId]
                            WHERE
	                            [U].[OrganizationId] = @OrganizationId AND
	                            CAST([P].[InAt] AS DATE) BETWEEN @Min AND @Max
                            GROUP BY
	                            [T].[Id]
                        )
                    ORDER BY
                        {orderByFormatted} {orderByDirectionFormatted};";

                var results = connection.Query<TaskExpanded>(recordsSql, new
                {
                    OrganizationId = currentUser.OrganizationId,
                    Min = min,
                    Max = max
                });

                foreach (var result in results)
                {
                    tasks.Add(new Brizbee.Core.Models.Task()
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
                        },
                        BasePayrollRate = new Rate()
                        {
                            Id = result.BasePayrollRate_Id.GetValueOrDefault(),
                            IsDeleted = result.BasePayrollRate_IsDeleted.GetValueOrDefault(),
                            CreatedAt = result.BasePayrollRate_CreatedAt.GetValueOrDefault(),
                            Name = result.BasePayrollRate_Name,
                            OrganizationId = result.BasePayrollRate_OrganizationId.GetValueOrDefault(),
                            ParentRateId = result.BasePayrollRate_ParentRateId,
                            QBDPayrollItem = result.BasePayrollRate_QBDPayrollItem,
                            QBOPayrollItem = result.BasePayrollRate_QBOPayrollItem,
                            Type = result.BasePayrollRate_Type
                        },
                        BaseServiceRate = new Rate()
                        {
                            Id = result.BaseServiceRate_Id.GetValueOrDefault(),
                            IsDeleted = result.BaseServiceRate_IsDeleted.GetValueOrDefault(),
                            CreatedAt = result.BaseServiceRate_CreatedAt.GetValueOrDefault(),
                            Name = result.BaseServiceRate_Name,
                            OrganizationId = result.BaseServiceRate_OrganizationId.GetValueOrDefault(),
                            ParentRateId = result.BaseServiceRate_ParentRateId,
                            QBDServiceItem = result.BaseServiceRate_QBDServiceItem,
                            QBOServiceItem = result.BaseServiceRate_QBOServiceItem,
                            Type = result.BaseServiceRate_Type
                        },
                    });
                }

                connection.Close();
            }

            // Set headers.
            HttpContext.Response.Headers.Add("X-Paging-TotalRecordCount", total.ToString(CultureInfo.InvariantCulture));

            return new JsonResult(tasks)
            {
                StatusCode = (int)HttpStatusCode.OK
            };
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
