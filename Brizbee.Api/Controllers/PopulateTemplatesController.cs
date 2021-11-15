//
//  PopulateTemplatesController.cs
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
    public class PopulateTemplatesController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly SqlContext _context;

        public PopulateTemplatesController(IConfiguration configuration, SqlContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        // GET: api/PopulateTemplates
        [HttpGet("api/PopulateTemplates")]
        public IActionResult GetPopulateTemplates([FromQuery] int pageNumber = 1,
            [FromQuery] int skip = 0, [FromQuery] int pageSize = 1000,
            [FromQuery] string orderBy = "POPULATE_TEMPLATES/NAME", [FromQuery] string orderByDirection = "ASC")
        {
            if (pageSize > 1000) { return BadRequest(); }

            var currentUser = CurrentUser();

            var total = 0;
            var templates = new List<PopulateTemplate>(0);
            using (var connection = new SqlConnection(_configuration.GetConnectionString("SqlContext")))
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
                        [T].[Template],
                        [T].[RateType]
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
                        Template = result.Template,
                        RateType = result.RateType
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

            return new JsonResult(templates)
            {
                StatusCode = (int)HttpStatusCode.OK
            };
        }

        // POST: api/PopulateTemplates
        [HttpPost("api/PopulateTemplates")]
        public IActionResult Post([FromBody] PopulateTemplate populateTemplate)
        {
            var currentUser = CurrentUser();

            try
            {
                // Attempt to find an existing template to replace.
                var existingTemplate = _context.PopulateTemplates
                    .Where(t => t.OrganizationId == currentUser.OrganizationId)
                    .Where(t => t.RateType == populateTemplate.RateType)
                    .Where(t => t.Name == populateTemplate.Name)
                    .FirstOrDefault();

                if (existingTemplate != null)
                {
                    // Update the existing template.
                    existingTemplate.Template = populateTemplate.Template;

                    // Validate the model.
                    ModelState.ClearValidationState(nameof(existingTemplate));
                    if (!TryValidateModel(existingTemplate, nameof(existingTemplate)))
                        return BadRequest();

                    _context.SaveChanges();

                    return Ok(existingTemplate);
                }
                else
                {
                    // Set defaults for the new template.
                    populateTemplate.CreatedAt = DateTime.UtcNow;
                    populateTemplate.OrganizationId = currentUser.OrganizationId;

                    // Validate the model.
                    ModelState.ClearValidationState(nameof(populateTemplate));
                    if (!TryValidateModel(populateTemplate, nameof(populateTemplate)))
                        return BadRequest();

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

    class PopulateTemplateResult
    {
        public DateTime CreatedAt { get; set; }

        public int Id { get; set; }

        public int OrganizationId { get; set; }

        public string Name { get; set; }

        public string Template { get; set; }

        public string RateType { get; set; }
    }
}
