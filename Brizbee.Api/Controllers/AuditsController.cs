//
//  AuditsController.cs
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

using Brizbee.Core.Models;
using Brizbee.Core.Serialization.Records;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Globalization;
using System.Net;

namespace Brizbee.Api.Controllers
{
    public class AuditsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly SqlContext _context;

        public AuditsController(IConfiguration configuration, SqlContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        // GET: api/Audits
        [HttpGet("api/Audits")]
        public IActionResult GetAudits([FromQuery] DateTime min, [FromQuery] DateTime max, 
            [FromQuery] int skip = 0, [FromQuery] int pageSize = 1000,
            [FromQuery] string orderBy = "AUDITS/CREATEDAT", [FromQuery] string orderByDirection = "DESC",
            [FromQuery] int[] userIds = null)
        {
            if (pageSize > 1000) { BadRequest(); }

            var currentUser = CurrentUser();

            // Ensure that user is authorized.
            if (!currentUser.CanViewAudits)
                BadRequest();

            var total = 0;
            List<Audit> audits = new List<Audit>(0);
            using (var connection = new SqlConnection(_configuration.GetConnectionString("SqlContext")))
            {
                connection.Open();

                // Determine the order by columns.
                var orderByFormatted = "";
                switch (orderBy.ToUpperInvariant())
                {
                    case "AUDITS/CREATEDAT":
                        orderByFormatted = "[CreatedAt]";
                        break;
                    default:
                        orderByFormatted = "[CreatedAt]";
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

                // Clause for user ids.
                if (userIds != null && userIds.Any())
                {
                    whereClause += $" AND [UserId] IN ({string.Join(",", userIds)})";
                }

                // Get the count.
                var countSql = $@"
                    SELECT
                        COUNT(*)
                    FROM
                        (
                            SELECT
                                [Id]
                            FROM
                                [PunchAudits]
                            WHERE
                                [OrganizationId] = @OrganizationId AND
                                [CreatedAt] BETWEEN @Min AND @Max {whereClause}
                            UNION ALL
                            SELECT
                                [Id]
                            FROM
                                [TimeCardAudits]
                            WHERE
                                [OrganizationId] = @OrganizationId AND
                                [CreatedAt] BETWEEN @Min AND @Max {whereClause}
                        ) [Audits];";

                total = connection.QueryFirst<int>(countSql, parameters);

                // Paging parameters.
                parameters.Add("@Skip", skip);
                parameters.Add("@PageSize", pageSize);

                // Get the records.
                var recordsSql = $@"
                    SELECT
                        [Id] AS [Task_Id],
                        [CreatedAt] AS [Task_CreatedAt],
                        [OrganizationId] AS [Task_OrganizationId],
                        [UserId] AS [Task_UserId],
                        [ObjectId] AS [Task_ObjectId],
                        [Action] AS [Task_Action],
                        [Before] AS [Task_Before],
                        [After] AS [Task_After],
                        'PUNCH' AS [Task_ObjectType]
                    FROM
                        [PunchAudits]
                    WHERE
                        [OrganizationId] = @OrganizationId AND
                        [CreatedAt] BETWEEN @Min AND @Max {whereClause}
                    UNION ALL
                    SELECT
                        [Id] AS [Task_Id],
                        [CreatedAt] AS [Task_CreatedAt],
                        [OrganizationId] AS [Task_OrganizationId],
                        [UserId] AS [Task_UserId],
                        [ObjectId] AS [Task_ObjectId],
                        [Action] AS [Task_Action],
                        [Before] AS [Task_Before],
                        [After] AS [Task_After],
                        'TIMECARD' AS [Task_ObjectType]
                    FROM
                        [TimeCardAudits]
                    WHERE
                        [OrganizationId] = @OrganizationId AND
                        [CreatedAt] BETWEEN @Min AND @Max {whereClause}
                    ORDER BY
                        {orderByFormatted} {orderByDirectionFormatted}
                    OFFSET @Skip ROWS
                    FETCH NEXT @PageSize ROWS ONLY;";

                var records = connection.Query<AuditRecord>(recordsSql, parameters);

                foreach (var record in records)
                {
                    audits.Add(new Audit()
                    {
                        Id = record.Audit_Id,
                        CreatedAt = record.Audit_CreatedAt,
                        OrganizationId = record.Audit_OrganizationId,
                        UserId = record.Audit_UserId,
                        ObjectId = record.Audit_ObjectId,
                        Action = record.Audit_Action,
                        Before = record.Audit_Before,
                        After = record.Audit_After,
                        ObjectType = record.Audit_ObjectType
                    });
                }

                connection.Close();
            }

            // Set headers.
            HttpContext.Response.Headers.Add("X-Paging-TotalRecordCount", total.ToString(CultureInfo.InvariantCulture));

            return new JsonResult(audits)
            {
                StatusCode = (int)HttpStatusCode.OK
            };
        }

        // GET: api/Audits/Punches
        [HttpGet("api/Audits/Punches")]
        public IActionResult GetPunchAudits([FromQuery] DateTime min, [FromQuery] DateTime max,
            [FromQuery] int skip = 0, [FromQuery] int pageSize = 1000,
            [FromQuery] string orderBy = "AUDITS/CREATEDAT", [FromQuery] string orderByDirection = "DESC",
            [FromQuery] int[] userIds = null, [FromQuery] int[] objectIds = null)
        {
            if (pageSize > 1000) { BadRequest(); }

            var currentUser = CurrentUser();

            // Ensure that user is authorized.
            if (!currentUser.CanViewAudits)
                BadRequest();

            var audits = Audits("PUNCH", orderBy, orderByDirection, skip, pageSize, userIds, objectIds, currentUser, min, max);

            // Set headers.
            HttpContext.Response.Headers.Add("X-Paging-TotalRecordCount", audits.Item2.ToString(CultureInfo.InvariantCulture));

            return new JsonResult(audits.Item1)
            {
                StatusCode = (int)HttpStatusCode.OK
            };
        }

        // GET: api/Audits/TimeCards
        [HttpGet("api/Audits/TimeCards")]
        public IActionResult GetTimeCardsAudits([FromQuery] DateTime min, [FromQuery] DateTime max,
            [FromQuery] int skip = 0, [FromQuery] int pageSize = 1000,
            [FromQuery] string orderBy = "AUDITS/CREATEDAT", [FromQuery] string orderByDirection = "DESC",
            [FromQuery] int[] userIds = null, [FromQuery] int[] objectIds = null)
        {
            if (pageSize > 1000) { BadRequest(); }

            var currentUser = CurrentUser();

            // Ensure that user is authorized.
            if (!currentUser.CanViewAudits)
                BadRequest();

            var audits = Audits("TIMECARD", orderBy, orderByDirection, skip, pageSize, userIds, objectIds, currentUser, min, max);

            // Set headers.
            HttpContext.Response.Headers.Add("X-Paging-TotalRecordCount", audits.Item2.ToString(CultureInfo.InvariantCulture));

            return new JsonResult(audits.Item1)
            {
                StatusCode = (int)HttpStatusCode.OK
            };
        }

        private (List<Audit>, int) Audits(string objectType, string orderBy, string orderByDirection, int skip, int pageSize, int[] userIds, int[] objectIds, User currentUser, DateTime min, DateTime max)
        {
            string tableName;
            if (objectType.ToUpperInvariant() == "PUNCH")
                tableName = "PunchAudits";
            else if (objectType.ToUpperInvariant() == "TIMECARD")
                tableName = "TimeCardAudits";
            else
                throw new ArgumentException();

            var total = 0;
            List<Audit> audits = new List<Audit>(0);
            using (var connection = new SqlConnection(_configuration.GetConnectionString("SqlContext")))
            {
                connection.Open();

                // Determine the order by columns.
                var orderByFormatted = "";
                switch (orderBy.ToUpperInvariant())
                {
                    case "AUDITS/CREATEDAT":
                        orderByFormatted = "[A].[CreatedAt]";
                        break;
                    default:
                        orderByFormatted = "[A].[CreatedAt]";
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

                // Clause for user ids.
                if (userIds != null && userIds.Any())
                {
                    whereClause += $" AND [A].[UserId] IN ({string.Join(",", userIds)})";
                }

                // Clause for object ids.
                if (objectIds != null && objectIds.Any())
                {
                    whereClause += $" AND [A].[ObjectId] IN ({string.Join(",", objectIds)})";
                }

                // Get the count.
                var countSql = $@"
                    SELECT
                        COUNT(*)
                    FROM
                        [{tableName}] AS [A]
                    WHERE
                        [A].[OrganizationId] = @OrganizationId AND
                        [A].[CreatedAt] BETWEEN @Min AND @Max {whereClause};";

                total = connection.QueryFirst<int>(countSql, parameters);

                // Paging parameters.
                parameters.Add("@Skip", skip);
                parameters.Add("@PageSize", pageSize);

                // Get the records.
                var recordsSql = $@"
                    SELECT
                        [A].[Id] AS [Audit_Id],
                        [A].[CreatedAt] AS [Audit_CreatedAt],
                        [A].[OrganizationId] AS [Audit_OrganizationId],
                        [A].[UserId] AS [Audit_UserId],
                        [A].[ObjectId] AS [Audit_ObjectId],
                        [A].[Action] AS [Audit_Action],
                        [A].[Before] AS [Audit_Before],
                        [A].[After] AS [Audit_After],
                        '{objectType.ToUpper()}' AS [Audit_ObjectType],

                        [U].[Id] AS [User_Id],
                        [U].[Name] AS [User_Name]
                    FROM
                        [{tableName}] AS [A]
                    JOIN
                        [Users] AS [U] ON [U].[Id] = [A].[UserId]
                    WHERE
                        [A].[OrganizationId] = @OrganizationId AND
                        [A].[CreatedAt] BETWEEN @Min AND @Max {whereClause}
                    ORDER BY
                        {orderByFormatted} {orderByDirectionFormatted}
                    OFFSET @Skip ROWS
                    FETCH NEXT @PageSize ROWS ONLY;";

                var results = connection.Query<AuditRecord>(recordsSql, parameters);

                foreach (var result in results)
                {
                    audits.Add(new Audit()
                    {
                        Id = result.Audit_Id,
                        CreatedAt = result.Audit_CreatedAt,
                        OrganizationId = result.Audit_OrganizationId,
                        UserId = result.Audit_UserId,
                        ObjectId = result.Audit_ObjectId,
                        Action = result.Audit_Action,
                        Before = result.Audit_Before,
                        After = result.Audit_After,
                        ObjectType = result.Audit_ObjectType,

                        User = new User()
                        {
                            Id = result.User_Id,
                            Name = result.User_Name
                        }
                    });
                }

                connection.Close();
            }

            return (audits, total);
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
