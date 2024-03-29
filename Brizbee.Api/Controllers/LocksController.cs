﻿//
//  LocksController.cs
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
using Brizbee.Api.Services;
using Brizbee.Core.Models;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Net;

namespace Brizbee.Api.Controllers
{
    public class LocksController : ODataController
    {
        private readonly IConfiguration _configuration;
        private readonly SqlContext _context;

        public LocksController(IConfiguration configuration, SqlContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        // GET: api/Locks
        [HttpGet("api/Locks")]
        public IActionResult GetLocks(
            [FromQuery] int skip = 0, [FromQuery] int pageSize = 1000,
            [FromQuery] string orderBy = "LOCK/CREATEDAT", [FromQuery] string orderByDirection = "ASC")
        {
            if (pageSize > 1000) { return BadRequest();  }

            var currentUser = CurrentUser();

            var total = 0;
            var locks = new List<Commit>(0);
            using (var connection = new SqlConnection(_configuration.GetConnectionString("SqlContext")))
            {
                connection.Open();

                // Determine the order by columns.
                var orderByFormatted = "";
                switch (orderBy.ToUpperInvariant())
                {
                    case "LOCK/CREATEDAT":
                        orderByFormatted = "[C].[CreatedAt]";
                        break;
                    case "LOCK/INAT":
                        orderByFormatted = "[C].[InAt]";
                        break;
                    case "LOCK/OUTAT":
                        orderByFormatted = "[C].[OutAt]";
                        break;
                    case "LOCK/PUNCHCOUNT":
                        orderByFormatted = "[C].[PunchCount]";
                        break;
                    case "LOCK/QUICKBOOKSEXPORTEDAT":
                        orderByFormatted = "[C].[QuickBooksExportedAt]";
                        break;
                    case "USER/NAME":
                        orderByFormatted = "[U].[Name]";
                        break;
                    default:
                        orderByFormatted = "[C].[CreatedAt]";
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
                        [Commits] AS [C]
                    WHERE
                        [C].[OrganizationId] = @OrganizationId;";

                total = connection.QuerySingle<int>(countSql, parameters);

                // Paging parameters.
                parameters.Add("@Skip", skip);
                parameters.Add("@PageSize", pageSize);

                // Get the records.
                var recordsSql = $@"
                    SELECT
                        [C].[Id] AS [Lock_Id],
                        [C].[CreatedAt] AS [Lock_CreatedAt],
                        [C].[OrganizationId] AS [Lock_OrganizationId],
                        [C].[InAt] AS [Lock_InAt],
                        [C].[OutAt] AS [Lock_OutAt],
                        [C].[QuickBooksExportedAt] AS [Lock_QuickBooksExportedAt],
                        [C].[PunchCount] AS [Lock_PunchCount],
                        [C].[UserId] AS [Lock_UserId],
                        [C].[Guid] AS [Lock_Guid],

                        [U].[Id] AS [User_Id],
                        [U].[Name] AS [User_Name]
                    FROM
                        [Commits] AS [C]
                    INNER JOIN
                        [Users] AS [U] ON [U].[Id] = [C].[UserId]
                    WHERE
                        [C].[OrganizationId] = @OrganizationId
                    ORDER BY
                        {orderByFormatted} {orderByDirectionFormatted}
                    OFFSET @Skip ROWS
                    FETCH NEXT @PageSize ROWS ONLY;";

                var results = connection.Query<LockExpanded>(recordsSql, parameters);

                foreach (var result in results)
                {
                    locks.Add(new Commit()
                    {
                        Id = result.Lock_Id,
                        CreatedAt = result.Lock_CreatedAt,
                        OrganizationId = result.Lock_OrganizationId,
                        InAt = result.Lock_InAt,
                        OutAt = result.Lock_OutAt,
                        QuickBooksExportedAt = result.Lock_QuickBooksExportedAt,
                        PunchCount = result.Lock_PunchCount,
                        UserId = result.Lock_UserId,
                        Guid = result.Lock_Guid,
                        User = new User()
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

            return new JsonResult(locks)
            {
                StatusCode = (int)HttpStatusCode.OK
            };
        }

        // GET: api/Locks/{id}
        [HttpGet("api/Locks/{id}")]
        public IActionResult GetLock(int id)
        {
            var currentUser = CurrentUser();

            // Ensure that user is authorized.
            if (!currentUser.CanViewLocks)
                return Forbid();

            var commit = _context.Commits
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .Where(c => c.Id == id)
                .FirstOrDefault();

            if (commit == null)
                return NotFound();

            return Ok(commit);
        }

        // POST: api/Locks
        [HttpPost("api/Locks")]
        public IActionResult Post([FromBody] Commit commit)
        {
            var currentUser = CurrentUser();

            // Ensure that user is authorized.
            if (!currentUser.CanCreateLocks)
                return Forbid();

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var inAt = new DateTime(commit.InAt.Year, commit.InAt.Month, commit.InAt.Day, 0, 0, 0, DateTimeKind.Unspecified);
                    var outAt = new DateTime(commit.OutAt.Year, commit.OutAt.Month, commit.OutAt.Day, 23, 59, 59, DateTimeKind.Unspecified);

                    // Ensure that no two commits overlap
                    var overlap = _context.Commits
                        .Where(c => c.OrganizationId == currentUser.OrganizationId)
                        .Where(c => (inAt < c.OutAt) && (c.InAt < outAt))
                        .FirstOrDefault();
                    if (overlap != null)
                    {
                        return BadRequest(string.Format(
                                "The commit overlaps another commit: {0} thru {1}",
                                overlap.InAt.ToString("yyyy-MM-dd"),
                                overlap.OutAt.ToString("yyyy-MM-dd")
                            ));
                    }

                    // Auto-generated
                    commit.CreatedAt = DateTime.UtcNow;
                    commit.Guid = Guid.NewGuid();
                    commit.OrganizationId = currentUser.OrganizationId;
                    commit.UserId = currentUser.Id;
                    commit.InAt = inAt;
                    commit.OutAt = outAt;

                    _context.Commits.Add(commit);

                    _context.SaveChanges();

                    // Split the punches at midnight.
                    var service = new PunchService(_context);
                    int[] userIds = _context.Users
                        .Where(u => u.OrganizationId == currentUser.OrganizationId)
                        .Select(u => u.Id)
                        .ToArray();
                    var originalPunchesTracked = _context.Punches
                        .Where(p => userIds.Contains(p.UserId))
                        .Where(p => p.OutAt.HasValue)
                        .Where(p => p.InAt.Date >= inAt.Date)
                        .Where(p => p.InAt.Date <= outAt.Date)
                        .Where(p => !p.CommitId.HasValue); // Only uncommited punches
                    var originalPunchesNotTracked = originalPunchesTracked
                        .AsNoTracking() // Will be manipulated in memory
                        .ToList();
                    var splitPunches = service.SplitAtMidnight(originalPunchesNotTracked, currentUser);

                    // Delete the old punches and save the new ones.
                    _context.Punches.RemoveRange(originalPunchesTracked);
                    _context.SaveChanges();

                    // Save the commit id with the new punches.
                    foreach (var punch in splitPunches)
                    {
                        punch.CommitId = commit.Id;
                    }
                    commit.PunchCount = splitPunches.Count();

                    _context.Punches.AddRange(splitPunches);
                    _context.SaveChanges();

                    transaction.Commit();

                    return Ok(commit);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    return BadRequest(ex.ToString());
                }
            }
        }

        // POST: api/Locks/{id}/Undo
        [HttpPost("api/Locks/{id}/Undo")]
        public IActionResult PostUndo(int id)
        {
            var currentUser = CurrentUser();

            // Ensure that user is authorized.
            if (!currentUser.CanUndoLocks)
                return Forbid();

            var commit = _context.Commits
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .FirstOrDefault(c => c.Id == id);

            if (commit == null)
                return NotFound();

            var punches = _context.Punches
                .Where(p => p.CommitId == commit.Id)
                .ToList();
            punches.ForEach(p => {
                p.CommitId = null;
            });

            _context.Commits.Remove(commit);

            var qboExports = _context.QuickBooksOnlineExports
                .Where(x => x.CommitId == id)
                .ToList();
            qboExports.ForEach(x => {
                _context.QuickBooksOnlineExports.Remove(x);
            });

            var qbdExports = _context.QuickBooksDesktopExports
                .Where(x => x.CommitId == id)
                .ToList();
            qbdExports.ForEach(x => {
                _context.QuickBooksDesktopExports.Remove(x);
            });

            _context.SaveChanges();

            return Ok();
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
