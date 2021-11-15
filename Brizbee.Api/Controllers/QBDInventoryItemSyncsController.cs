//
//  QBDInventoryItemSyncsController.cs
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
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Globalization;
using System.Net;

namespace Brizbee.Api.Controllers
{
    public class QBDInventoryItemSyncsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly SqlContext _context;

        public QBDInventoryItemSyncsController(IConfiguration configuration, SqlContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        // GET: api/QBDInventoryItemSyncs
        [HttpGet]
        [Route("api/QBDInventoryItemSyncs")]
        public IActionResult GetQBDInventoryItemSyncs(
            [FromQuery] int skip = 0, [FromQuery] int pageSize = 1000,
            [FromQuery] string orderBy = "QBDINVENTORYITEMSYNCS/CREATEDAT", [FromQuery] string orderByDirection = "ASC")
        {
            if (pageSize > 1000) { return BadRequest(); }

            var currentUser = CurrentUser();

            // Ensure that user is authorized.
            if (!currentUser.CanSyncInventoryItems)
                return Forbid();

            var total = 0;
            List<QBDInventoryItemSync> syncs = new List<QBDInventoryItemSync>(0);
            using (var connection = new SqlConnection(_configuration.GetConnectionString("SqlContext")))
            {
                connection.Open();

                // Determine the order by columns.
                var orderByFormatted = "";
                switch (orderBy.ToUpperInvariant())
                {
                    case "QBDINVENTORYITEMSYNCS/CREATEDAT":
                        orderByFormatted = "S.[CreatedAt]";
                        break;
                    case "USERS/NAME":
                        orderByFormatted = "U.[Name]";
                        break;
                    default:
                        orderByFormatted = "S.[CreatedAt]";
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

                var parameters = new DynamicParameters();

                // Common clause.
                parameters.Add("@OrganizationId", currentUser.OrganizationId);

                // Get the count.
                var countSql = $@"
                    SELECT
                        COUNT(*)
                    FROM
                        [QBDInventoryItemSyncs] AS S
                    WHERE
                        S.[OrganizationId] = @OrganizationId;";

                total = connection.QuerySingle<int>(countSql, parameters);

                // Paging parameters.
                parameters.Add("@Skip", skip);
                parameters.Add("@PageSize", pageSize);

                // Get the records.
                var recordsSql = $@"
                    SELECT
                        S.Id AS Sync_Id,
                        S.CreatedAt AS Sync_CreatedAt,
                        S.CreatedByUserId AS Sync_CreatedByUserId,
                        S.OrganizationId AS Sync_OrganizationId,
                        S.HostProductName AS Sync_HostProductName,
                        S.Hostname AS Sync_Hostname,

                        U.Id AS User_Id,
                        U.Name AS User_Name
                    FROM
                        [QBDInventoryItemSyncs] AS S
                    INNER JOIN
                        [Users] AS U ON S.[CreatedByUserId] = U.[Id]
                    WHERE
                        S.[OrganizationId] = @OrganizationId
                    ORDER BY
                        {orderByFormatted} {orderByDirectionFormatted}
                    OFFSET @Skip ROWS
                    FETCH NEXT @PageSize ROWS ONLY;";

                var results = connection.Query<QBDInventoryItemSyncExpanded>(recordsSql, parameters);

                foreach (var result in results)
                {
                    syncs.Add(new QBDInventoryItemSync()
                    {
                        Id = result.Sync_Id,
                        CreatedAt = result.Sync_CreatedAt,
                        CreatedByUserId = result.Sync_CreatedByUserId,
                        OrganizationId = result.Sync_OrganizationId,
                        HostProductName = result.Sync_HostProductName,
                        Hostname = result.Sync_Hostname,
                        CreatedByUser = new User()
                        {
                            Id = result.User_Id,
                            Name = result.User_Name
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

            return new JsonResult(syncs)
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

    public class QBDInventoryItemSyncExpanded
    {
        // Sync Details

        public long Sync_Id { get; set; }

        public DateTime Sync_CreatedAt { get; set; }

        public int Sync_CreatedByUserId { get; set; }

        public int Sync_OrganizationId { get; set; }

        public string Sync_HostProductName { get; set; }

        public string Sync_Hostname { get; set; }


        // User Details

        public int User_Id { get; set; }

        public string User_Name { get; set; }
    }
}