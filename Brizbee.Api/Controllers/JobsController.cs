//
//  JobsController.cs
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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Brizbee.Api.Controllers
{
    [ApiController]
    [Authorize]
    public class JobsController : ControllerBase
    {
        private readonly IConfiguration Configuration;
        private readonly SqlContext _context;

        public JobsController(IConfiguration configuration, SqlContext context)
        {
            Configuration = configuration;
            _context = context;
        }

        // GET: api/Jobs
        [HttpGet("api/Jobs")]
        public ActionResult<IEnumerable<Job>> GetJobs([FromQuery] int pageNumber = 1,
            [FromQuery] string direction = "ASC", [FromQuery] int pageSize = 1000, [FromQuery] string orderBy = "JOBS/NAME",
            [FromQuery] int[] jobIds = null, [FromQuery] string[] jobNumbers = null, [FromQuery] string[] jobNames = null,
            [FromQuery] int[] customerIds = null, [FromQuery] string[] customerNumbers = null, [FromQuery] string[] customerNames = null)
        {
            // Determine the number of records to skip.
            int skip = (pageNumber - 1) * pageSize;

            var currentUser = CurrentUser();

            // Validate order.
            var allowed = new string[] { "JOBS/CREATEDAT", "JOBS/NAME", "JOBS/NUMBER" };
            if (!allowed.Contains(orderBy.ToUpperInvariant()))
            {
                //return BadRequest();
            }
            orderBy = "J.Name";

            // Validate direction.
            direction = direction.ToUpperInvariant();
            if (direction != "ASC" || direction != "DESC")
            {
                //return BadRequest();
            }

            // Build where clauses.
            var whereClauses = "";

            if (jobIds != null && jobIds.Length > 0)
            {
                whereClauses += $" AND J.Id IN ({string.Join(',', jobIds)})";
            }

            if (jobNumbers != null && jobNumbers.Length > 0)
            {
                whereClauses += $" AND J.Number IN ({string.Join(',', jobNumbers.Select(x => string.Format("'{0}'", x)))})";
            }

            if (jobNames != null && jobNames.Length > 0)
            {
                whereClauses += $" AND J.Name IN ({string.Join(',', jobNames.Select(x => string.Format("'{0}'", x)))})";
            }

            if (customerIds != null && customerIds.Length > 0)
            {
                whereClauses += $" AND C.Id IN ({string.Join(',', customerIds)})";
            }

            if (customerNumbers != null && customerNumbers.Length > 0)
            {
                whereClauses += $" AND C.Number IN ({string.Join(',', customerNumbers.Select(x => string.Format("'{0}'", x)))})";
            }

            if (customerNames != null && customerNames.Length > 0)
            {
                whereClauses += $" AND C.Name IN ({string.Join(',', customerNames.Select(x => string.Format("'{0}'", x)))})";
            }

            var records = new List<Job>();
            using (var connection = new SqlConnection(Configuration["ConnectionStrings:SqlContext"]))
            {
                connection.Open();

                var sql = $@"
                    SELECT
                        J.Id,
	                    J.CreatedAt,
                        J.Name,
                        J.Number,
                        J.Description,
                        J.QuickBooksCustomerJob,
                        J.CustomerId,

                        C.Id,
	                    C.CreatedAt,
                        C.Name,
                        C.Number,
                        C.Description,
                        C.OrganizationId
                    FROM
	                    [Jobs] AS J
                    INNER JOIN
                        [Customers] AS C ON J.CustomerId = C.Id
                    WHERE
	                    C.[OrganizationId] = @OrganizationId
                        {whereClauses}
                    ORDER BY
	                    {orderBy} {direction}
                    OFFSET @Skip ROWS
                    FETCH NEXT @PageSize ROWS ONLY;";

                records = connection.Query<Job, Customer, Job>(sql, (j, c) =>
                {
                    j.Customer = c;
                    return j;
                },
                new { OrganizationId = currentUser.OrganizationId, Skip = skip, PageSize = pageSize }).ToList();

                connection.Close();
            }

            // Get total number of records
            var total = _context.Customers
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .Count();

            // Determine page count
            int pageCount = total > 0
                ? (int)Math.Ceiling(total / (double)pageSize)
                : 0;

            // Set headers for paging
            Response.Headers.Add("X-Paging-PageNumber", pageNumber.ToString(CultureInfo.InvariantCulture));
            Response.Headers.Add("X-Paging-PageSize", pageSize.ToString(CultureInfo.InvariantCulture));
            Response.Headers.Add("X-Paging-PageCount", pageCount.ToString(CultureInfo.InvariantCulture));
            Response.Headers.Add("X-Paging-TotalRecordCount", total.ToString(CultureInfo.InvariantCulture));

            return records;
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