//
//  QBDInventoryConsumptionSyncsController.cs
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
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Net;

namespace Brizbee.Api.Controllers
{
    public class QBDInventoryConsumptionSyncsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly SqlContext _context;

        public QBDInventoryConsumptionSyncsController(IConfiguration configuration, SqlContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        // GET: api/QBDInventoryConsumptionSyncs
        [HttpGet("api/QBDInventoryConsumptionSyncs")]
        public IActionResult GetQBDInventoryConsumptionSyncs(
            [FromQuery] int skip = 0, [FromQuery] int pageSize = 1000,
            [FromQuery] string orderBy = "QBDINVENTORYCONSUMPTIONSYNCS/CREATEDAT", [FromQuery] string orderByDirection = "ASC")
        {
            if (pageSize > 1000) { return BadRequest(); }

            var currentUser = CurrentUser();

            // Ensure that user is authorized.
            if (!currentUser.CanSyncInventoryConsumptions)
                return Forbid();

            var total = 0;
            List<QBDInventoryConsumptionSync> syncs = new List<QBDInventoryConsumptionSync>();
            using (var connection = new SqlConnection(_configuration.GetConnectionString("SqlContext")))
            {
                connection.Open();

                // Determine the order by columns.
                var orderByFormatted = "";
                switch (orderBy.ToUpperInvariant())
                {
                    case "QBDINVENTORYCONSUMPTIONSYNCS/CREATEDAT":
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
                        [QBDInventoryConsumptionSyncs] AS S
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
                        S.RecordingMethod AS Sync_RecordingMethod,
                        S.ValueMethod AS Sync_ValueMethod,
                        S.HostProductName AS Sync_HostProductName,
                        S.Hostname AS Sync_Hostname,
                        S.HostCompanyFileName AS Sync_HostCompanyFileName,
                        S.ConsumptionsCount AS Sync_ConsumptionsCount,
                        S.TxnIDs AS Sync_TxnIDs,

                        U.Id AS User_Id,
                        U.Name AS User_Name
                    FROM
                        [QBDInventoryConsumptionSyncs] AS S
                    INNER JOIN
                        [Users] AS U ON S.[CreatedByUserId] = U.[Id]
                    WHERE
                        S.[OrganizationId] = @OrganizationId
                    ORDER BY
                        {orderByFormatted} {orderByDirectionFormatted}
                    OFFSET @Skip ROWS
                    FETCH NEXT @PageSize ROWS ONLY;";

                var results = connection.Query<QBDInventoryConsumptionSyncExpanded>(recordsSql, parameters);

                foreach (var result in results)
                {
                    syncs.Add(new QBDInventoryConsumptionSync()
                    {
                        Id = result.Sync_Id,
                        CreatedAt = result.Sync_CreatedAt,
                        CreatedByUserId = result.Sync_CreatedByUserId,
                        OrganizationId = result.Sync_OrganizationId,
                        RecordingMethod = result.Sync_RecordingMethod,
                        ValueMethod = result.Sync_ValueMethod,
                        HostProductName = result.Sync_HostProductName,
                        Hostname = result.Sync_Hostname,
                        HostCompanyFileName = result.Sync_HostCompanyFileName,
                        ConsumptionsCount = result.Sync_ConsumptionsCount,
                        TxnIDs = result.Sync_TxnIDs,
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

        // POST: api/QBDInventoryConsumptionSyncs/Reverse
        [HttpPost("api/QBDInventoryConsumptionSyncs/Reverse")]
        public IActionResult PostReverse([FromQuery] long id)
        {
            var currentUser = CurrentUser();

            var sync = _context.QBDInventoryConsumptionSyncs.Find(id);

            if (sync == null) return BadRequest();

            try
            {
                var consumptions = _context.QBDInventoryConsumptions
                    .Where(c => c.QBDInventoryConsumptionSyncId == sync.Id)
                    .ToList();

                // Mark the consumptions as unsynced.
                foreach (var consumption in consumptions)
                {
                    consumption.QBDInventoryConsumptionSyncId = null;
                }

                // Record the reverse details.
                sync.ReversedAt = DateTime.UtcNow;
                sync.ReversedByUserId = currentUser.Id;

                _context.SaveChanges();

                return Ok();
            }
            catch (DbUpdateException ex)
            {
                return BadRequest(ex.Message);
            }
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

    public class QBDInventoryConsumptionSyncExpanded
    {
        // Sync Details

        public long Sync_Id { get; set; }

        public DateTime Sync_CreatedAt { get; set; }

        public int Sync_CreatedByUserId { get; set; }

        public int Sync_OrganizationId { get; set; }

        public string Sync_RecordingMethod { get; set; }

        public string Sync_ValueMethod { get; set; }

        public string Sync_HostProductName { get; set; }

        public string Sync_Hostname { get; set; }

        public string Sync_HostCompanyFileName { get; set; }

        public int Sync_ConsumptionsCount { get; set; }

        public string Sync_TxnIDs { get; set; }


        // User Details

        public int User_Id { get; set; }

        public string User_Name { get; set; }
    }
}