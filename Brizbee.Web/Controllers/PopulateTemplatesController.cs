//
//  PopulateTemplatesController.cs
//  BRIZBEE API
//
//  Copyright (C) 2021 East Coast Technology Services, LLC
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
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Brizbee.Web.Controllers
{
    public class PopulateTemplatesController : ApiController
    {
        private SqlContext _context = new SqlContext();
        private JsonSerializerSettings settings = new JsonSerializerSettings()
        {
            NullValueHandling = NullValueHandling.Ignore,
            StringEscapeHandling = StringEscapeHandling.EscapeHtml,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        // GET: api/PopulateTemplates
        [HttpGet]
        [Route("api/PopulateTemplates")]
        public HttpResponseMessage GetPopulateTemplates([FromUri] int pageNumber = 1,
            [FromUri] int skip = 0, [FromUri] int pageSize = 1000,
            [FromUri] string orderBy = "POPULATE_TEMPLATES/NAME", [FromUri] string orderByDirection = "ASC")
        {
            if (pageSize > 1000) { Request.CreateResponse(HttpStatusCode.BadRequest); }

            var currentUser = CurrentUser();

            var total = 0;
            var templates = new List<PopulateTemplate>(0);
            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["SqlContext"].ToString()))
            {
                connection.Open();

                // Determine the order by columns.
                var orderByFormatted = "";
                switch (orderBy.ToUpperInvariant())
                {
                    case "POPULATE_TEMPLATES/CREATEDAT":
                        orderByFormatted = "[T].[CreatedAt]";
                        break;
                    case "POPULATE_TEMPLATES/NAME":
                        orderByFormatted = "[T].[Name]";
                        break;
                    default:
                        orderByFormatted = "[T].[Name]";
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
                        [PopulateTemplates] AS [T]
                    WHERE
                        [T].[OrganizationId] = @OrganizationId;";

                total = connection.QuerySingle<int>(countSql, parameters);

                // Paging parameters.
                parameters.Add("@Skip", skip);
                parameters.Add("@PageSize", pageSize);

                // Get the records.
                var recordsSql = $@"
                    SELECT
                        [T].[Id],
                        [T].[CreatedAt],
                        [T].[OrganizationId],
                        [T].[Name],
                        [T].[Template]
                    FROM
                        [PopulateTemplates] AS [T]
                    WHERE
                        [T].[OrganizationId] = @OrganizationId
                    ORDER BY
                        {orderByFormatted} {orderByDirectionFormatted}
                    OFFSET @Skip ROWS
                    FETCH NEXT @PageSize ROWS ONLY;";

                var results = connection.Query<PopulateTemplateResult>(recordsSql, parameters);

                foreach (var result in results)
                {
                    templates.Add(new PopulateTemplate()
                    {
                        Id = result.Id,
                        CreatedAt = result.CreatedAt,
                        Name = result.Name,
                        OrganizationId = result.OrganizationId,
                        Template = result.Template
                    });
                }

                connection.Close();
            }

            // Determine page count.
            int pageCount = total > 0
                ? (int)Math.Ceiling(total / (double)pageSize)
                : 0;

            // Create the response
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(templates, settings),
                    System.Text.Encoding.UTF8,
                    "application/json")
            };

            // Set headers for paging.
            //response.Headers.Add("X-Paging-PageNumber", pageNumber.ToString(CultureInfo.InvariantCulture));
            response.Headers.Add("X-Paging-PageSize", pageSize.ToString(CultureInfo.InvariantCulture));
            response.Headers.Add("X-Paging-PageCount", pageCount.ToString(CultureInfo.InvariantCulture));
            response.Headers.Add("X-Paging-TotalRecordCount", total.ToString(CultureInfo.InvariantCulture));

            return response;
        }

        // POST: api/PopulateTemplates
        [HttpPost]
        [Route("api/PopulateTemplates")]
        public IHttpActionResult Post([FromBody] PopulateTemplate populateTemplate)
        {
            var currentUser = CurrentUser();

            try
            {
                // Attempt to find an existing template to replace.
                var existingTemplate = _context.PopulateTemplates
                    .Where(t => t.OrganizationId == currentUser.OrganizationId)
                    .Where(t => t.Name == populateTemplate.Name)
                    .FirstOrDefault();

                if (existingTemplate != null)
                {
                    // Update the existing template.
                    existingTemplate.Template = populateTemplate.Template;

                    return Ok(existingTemplate);
                }
                else
                {
                    // Set defaults for the new template.
                    populateTemplate.CreatedAt = DateTime.UtcNow;
                    populateTemplate.OrganizationId = currentUser.OrganizationId;

                    _context.PopulateTemplates.Add(populateTemplate);

                    _context.SaveChanges();

                    return Ok(populateTemplate);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
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

        public User CurrentUser()
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
    }

    class PopulateTemplateResult
    {
        public DateTime CreatedAt { get; set; }

        public int Id { get; set; }

        public int OrganizationId { get; set; }

        public string Name { get; set; }

        public string Template { get; set; }
    }
}
