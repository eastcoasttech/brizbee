//
//  VendorsController.cs
//  BRIZBEE API
//
//  Copyright (C) 2019-2023 East Coast Technology Services, LLC
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
using Brizbee.Core.Models.Accounting;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Globalization;
using System.Net;

namespace Brizbee.Api.Controllers.Accounting;

[Route("api/Accounting/[controller]")]
[ApiController]
public class VendorsController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly SqlContext _context;

    public VendorsController(IConfiguration configuration, SqlContext context)
    {
        _configuration = configuration;
        _context = context;
    }

    // GET: api/Accounting/Vendors
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Vendor>>> GetVendors(
        [FromQuery] int skip = 0, [FromQuery] int pageSize = 1000,
        [FromQuery] string orderBy = "VENDORS/NAME", [FromQuery] string orderByDirection = "ASC")
    {
        if (pageSize > 1000)
        {
            BadRequest();
        }

        var currentUser = CurrentUser();

        var vendors = new List<Vendor>();
        await using var connection = new SqlConnection(_configuration.GetConnectionString("SqlContext"));

        connection.Open();

        // Determine the order by columns.
        var orderByFormatted = orderBy.ToUpperInvariant() switch
        {
            "VENDORS/NUMBER" => "[V].[Number]",
            "VENDORS/NAME" => "[V].[Name]",
            _ => "[V].[Name]"
        };

        // Determine the order direction.
        var orderByDirectionFormatted = orderByDirection.ToUpperInvariant() switch
        {
            "ASC" => "ASC",
            "DESC" => "DESC",
            _ => "ASC"
        };

        var parameters = new DynamicParameters();

        // Common clause.
        parameters.Add("@OrganizationId", currentUser.OrganizationId);

        // Get the count.
        const string countSql = $@"
            SELECT
                COUNT(*)
            FROM [dbo].[Vendors] AS [V]
            WHERE
                [V].[OrganizationId] = @OrganizationId;";

        var total = await connection.QuerySingleAsync<int>(countSql, parameters);

        // Paging parameters.
        parameters.Add("@Skip", skip);
        parameters.Add("@PageSize", pageSize);

        // Get the records.
        var recordsSql = $@"
            SELECT
                [V].[CreatedAt],
                [V].[Id],
                [V].[Name],
                [V].[Number],
                [V].[OrganizationId]
            FROM [dbo].[Vendors] AS [V]
            WHERE
                [V].[OrganizationId] = @OrganizationId
            ORDER BY
                {orderByFormatted} {orderByDirectionFormatted}
            OFFSET @Skip ROWS
            FETCH NEXT @PageSize ROWS ONLY;";

        var results = await connection.QueryAsync<Vendor>(recordsSql, parameters);

        vendors.AddRange(results);

        // Determine page count.
        var pageCount = total > 0
            ? (int)Math.Ceiling(total / (double)pageSize)
            : 0;

        // Set headers for paging.
        HttpContext.Response.Headers.Add("X-Paging-PageSize", pageSize.ToString(CultureInfo.InvariantCulture));
        HttpContext.Response.Headers.Add("X-Paging-PageCount", pageCount.ToString(CultureInfo.InvariantCulture));
        HttpContext.Response.Headers.Add("X-Paging-TotalRecordCount", total.ToString(CultureInfo.InvariantCulture));

        return new JsonResult(vendors)
        {
            StatusCode = (int)HttpStatusCode.OK
        };
    }

    private User CurrentUser()
    {
        const string type = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";
        var claim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == type)!.Value;
        var currentUserId = int.Parse(claim);
        return _context.Users!
            .FirstOrDefault(u => u.Id == currentUserId)!;
    }
}
