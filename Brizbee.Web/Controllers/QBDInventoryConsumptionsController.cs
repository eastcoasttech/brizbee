//
//  QBDInventoryConsumptionsController.cs
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
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Brizbee.Web.Controllers
{
    public class QBDInventoryConsumptionsController : ApiController
    {
        private SqlContext _context = new SqlContext();
        private JsonSerializerSettings settings = new JsonSerializerSettings()
        {
            NullValueHandling = NullValueHandling.Ignore,
            StringEscapeHandling = StringEscapeHandling.EscapeHtml,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        // GET: api/QBDInventoryConsumptions
        [HttpGet]
        [Route("api/QBDInventoryConsumptions")]
        public HttpResponseMessage GetQBDInventoryConsumptions(
            [FromUri] int skip = 0, [FromUri] int pageSize = 1000,
            [FromUri] string orderBy = "QBDINVENTORYCONSUMPTIONS/CREATEDAT", [FromUri] string orderByDirection = "ASC")
        {
            if (pageSize > 1000) { Request.CreateResponse(HttpStatusCode.BadRequest); }

            var currentUser = CurrentUser();

            // Ensure Administrator.
            if (currentUser.Role != "Administrator")
                Request.CreateResponse(HttpStatusCode.BadRequest);

            var total = 0;
            List<QBDInventoryConsumption> consumptions = new List<QBDInventoryConsumption>();
            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["SqlContext"].ToString()))
            {
                connection.Open();

                // Determine the order by columns.
                var orderByFormatted = "";
                switch (orderBy.ToUpperInvariant())
                {
                    case "QBDINVENTORYCONSUMPTIONS/CREATEDAT":
                        orderByFormatted = "C.[CreatedAt]";
                        break;
                    default:
                        orderByFormatted = "C.[CreatedAt]";
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
                        [QBDInventoryConsumptions] AS C
                    WHERE
                        C.[OrganizationId] = @OrganizationId;";

                total = connection.QuerySingle<int>(countSql, parameters);

                // Paging parameters.
                parameters.Add("@Skip", skip);
                parameters.Add("@PageSize", pageSize);

                // Get the records.
                var recordsSql = $@"
                    SELECT
                        C.*
                    FROM
                        [QBDInventoryConsumptions] AS C
                    WHERE
                        C.[OrganizationId] = @OrganizationId
                    ORDER BY
                        {orderByFormatted} {orderByDirectionFormatted}
                    OFFSET @Skip ROWS
                    FETCH NEXT @PageSize ROWS ONLY;";

                var results = connection.Query<QBDInventoryConsumption>(recordsSql, parameters);

                consumptions = results.ToList();

                connection.Close();
            }

            // Determine page count.
            int pageCount = total > 0
                ? (int)Math.Ceiling(total / (double)pageSize)
                : 0;

            // Create the response
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(consumptions, settings),
                    System.Text.Encoding.UTF8,
                    "application/json")
            };

            // Set headers for paging.
            response.Headers.Add("X-Paging-PageSize", pageSize.ToString(CultureInfo.InvariantCulture));
            response.Headers.Add("X-Paging-PageCount", pageCount.ToString(CultureInfo.InvariantCulture));
            response.Headers.Add("X-Paging-TotalRecordCount", total.ToString(CultureInfo.InvariantCulture));

            return response;
        }

        // GET: api/QBDInventoryConsumptions/Unsynced
        [HttpGet]
        [Route("api/QBDInventoryConsumptions/Unsynced")]
        public IHttpActionResult GetUnsynced()
        {
            IEnumerable<string> pageNumberHeaders;
            Request.Headers.TryGetValues("X-Paging-PageNumber", out pageNumberHeaders);

            IEnumerable<string> pageSizeHeaders;
            Request.Headers.TryGetValues("X-Paging-PageSize", out pageSizeHeaders);

            // Validate page number.
            if (pageNumberHeaders == null) { return BadRequest("Header X-Paging-PageNumber must be provided."); }
            var pageNumber = int.Parse(pageNumberHeaders.First(), CultureInfo.InvariantCulture);

            // Validate page size.
            if (pageSizeHeaders == null) { return BadRequest("Header X-Paging-PageSize must be provided."); }

            var pageSize = int.Parse(pageSizeHeaders.First(), CultureInfo.InvariantCulture);
            if (pageSize > 1000) { return BadRequest("Cannot exceed 1000 records per page in a single request"); }

            var currentUser = CurrentUser();

            var consumptions = _context.QBDInventoryConsumptions
                .Include("QBDInventoryItem")
                .Include("QBDInventorySite")
                .Where(a => a.OrganizationId == currentUser.OrganizationId)
                .Where(a => !a.QBDInventoryConsumptionSyncId.HasValue);

            // Determine the number of records to skip.
            int skip = (pageNumber - 1) * pageSize;

            // Get total number of records.
            var total = consumptions.Count();

            // Determine page count.
            int pageCount = total > 0
                ? (int)Math.Ceiling(total / (double)pageSize)
                : 0;

            // Set headers for paging.
            Request.Headers.Add("X-Paging-PageNumber", pageNumber.ToString(CultureInfo.InvariantCulture));
            Request.Headers.Add("X-Paging-PageSize", pageSize.ToString(CultureInfo.InvariantCulture));
            Request.Headers.Add("X-Paging-PageCount", pageCount.ToString(CultureInfo.InvariantCulture));
            Request.Headers.Add("X-Paging-TotalRecordCount", total.ToString(CultureInfo.InvariantCulture));

            var records = consumptions
                .OrderBy(a => a.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .ToList();

            return Ok(records);
        }

        // POST: api/QBDInventoryConsumptions/Sync
        [HttpPost]
        [Route("api/QBDInventoryConsumptions/Sync")]
        public IHttpActionResult PostSync([FromBody] long[] ids)
        {
            var currentUser = CurrentUser();

            var consumptions = _context.QBDInventoryConsumptions
                .Where(a => a.OrganizationId == currentUser.OrganizationId)
                .Where(a => !a.QBDInventoryConsumptionSyncId.HasValue)
                .Where(a => ids.Contains(a.Id));

            // Record the sync.
            var sync = new QBDInventoryConsumptionSync()
            {
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = currentUser.Id,
                OrganizationId = currentUser.OrganizationId
            };
            _context.QBDInventoryConsumptionSyncs.Add(sync);

            // Assign the consumptions to the sync.
            foreach (var consumption in consumptions)
            {
                consumption.QBDInventoryConsumptionSyncId = consumption.Id;
            }

            try
            {
                _context.SaveChanges();

                return Ok();
            }
            catch (DbEntityValidationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (DbUpdateException ex)
            {
                return BadRequest(ex.ToString());
            }
        }

        private User CurrentUser()
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
