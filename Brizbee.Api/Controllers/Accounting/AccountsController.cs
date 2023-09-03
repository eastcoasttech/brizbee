//
//  AccountsController.cs
//  BRIZBEE API
//
//  Copyright (C) 2019-2022 East Coast Technology Services, LLC
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
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Net;

namespace Brizbee.Api.Controllers.Accounting;

[Route("api/Accounting/[controller]")]
[ApiController]
public class AccountsController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly SqlContext _context;

    public AccountsController(IConfiguration configuration, SqlContext context)
    {
        _configuration = configuration;
        _context = context;
    }

    // GET: api/Accounting/Accounts
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Account>>> GetAccounts(
        [FromQuery] int skip = 0, [FromQuery] int pageSize = 1000,
        [FromQuery] string orderBy = "ACCOUNTS/NAME", [FromQuery] string orderByDirection = "ASC")
    {
        if (pageSize > 1000)
        {
            BadRequest();
        }

        var currentUser = CurrentUser();

        var accounts = new List<Account>();
        await using var connection = new SqlConnection(_configuration.GetConnectionString("SqlContext"));

        connection.Open();

        // Determine the order by columns.
        var orderByFormatted = orderBy.ToUpperInvariant() switch
        {
            "ACCOUNTS/NUMBER" => "[A].[Number]",
            "ACCOUNTS/NAME" => "[A].[Name]",
            _ => "[A].[Name]"
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
            FROM [dbo].[Accounts] AS [A]
            WHERE
                [A].[OrganizationId] = @OrganizationId;";

        var total = await connection.QuerySingleAsync<int>(countSql, parameters);

        // Paging parameters.
        parameters.Add("@Skip", skip);
        parameters.Add("@PageSize", pageSize);

        // Get the records.
        var recordsSql = $@"
            SELECT
                [A].[CreatedAt],
                [A].[Description],
                [A].[Id],
                [A].[Name],
                [A].[Number],
                [A].[OrganizationId],
                [A].[Type],
                [A].[NormalBalance]
            FROM [dbo].[Accounts] AS [A]
            WHERE
                [A].[OrganizationId] = @OrganizationId
            ORDER BY
                {orderByFormatted} {orderByDirectionFormatted}
            OFFSET @Skip ROWS
            FETCH NEXT @PageSize ROWS ONLY;";

        var results = await connection.QueryAsync<Account>(recordsSql, parameters);

        accounts.AddRange(results);

        // Determine page count.
        var pageCount = total > 0
            ? (int)Math.Ceiling(total / (double)pageSize)
            : 0;

        // Set headers for paging.
        HttpContext.Response.Headers.Add("X-Paging-PageSize", pageSize.ToString(CultureInfo.InvariantCulture));
        HttpContext.Response.Headers.Add("X-Paging-PageCount", pageCount.ToString(CultureInfo.InvariantCulture));
        HttpContext.Response.Headers.Add("X-Paging-TotalRecordCount", total.ToString(CultureInfo.InvariantCulture));

        return new JsonResult(accounts)
        {
            StatusCode = (int)HttpStatusCode.OK
        };
    }

    // GET api/Accounting/Accounts/5
    [HttpGet("{id:long}")]
    public async Task<ActionResult<Account>> GetAccount(long id)
    {
        var account = await _context.Accounts!.FindAsync(id);

        if (account == null)
        {
            return NotFound();
        }

        return account;
    }

    // PUT api/Accounting/Accounts/5
    [HttpPut("{id:long}")]
    public async Task<IActionResult> UpdateAccount(long id, [FromBody] Account accountDto)
    {
        if (id != accountDto.Id)
        {
            return BadRequest();
        }

        var account = await _context.Accounts!.FindAsync(id);
        if (account == null)
        {
            return NotFound();
        }

        account.Name = accountDto.Name;
        account.Number = accountDto.Number;
        account.Description = accountDto.Description;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException) when (!AccountExists(id))
        {
            return NotFound();
        }

        return NoContent();
    }

    // POST api/Accounting/Accounts
    [HttpPost]
    public async Task<ActionResult<Account>> CreateAccount([FromBody] Account accountDto)
    {
        var currentUser = CurrentUser();

        var validTypes = new[]
        {
            "Bank",
            "Accounts Receivable",
            "Other Current Asset",
            "Fixed Asset",
            "Other Asset",
            "Expense",
            "Other Expense",
            "Accounts Payable",
            "Credit Card",
            "Other Current Liability",
            "Long Term Liability",
            "Equity",
            "Income",
            "Cost of Goods Sold",
            "Other Income"
        };

        if (!validTypes.Contains(accountDto.Type))
        {
            return BadRequest();
        }

        if (DuplicateNameExists(accountDto.Name))
        {
            return BadRequest();
        }

        if (DuplicateNumberExists(accountDto.Number))
        {
            return BadRequest();
        }

        var account = new Account
        {
            Name = accountDto.Name,
            Description = accountDto.Description,
            CreatedAt = DateTime.UtcNow,
            Number = accountDto.Number,
            OrganizationId = currentUser.OrganizationId,
            Type = accountDto.Type
        };

        _context.Accounts!.Add(account);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetAccount),
            new { id = account.Id },
            account);
    }

    // DELETE api/Accounting/Accounts/5
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteAccount(long id)
    {
        var account = await _context.Accounts!.FindAsync(id);

        if (account == null)
        {
            return NotFound();
        }

        _context.Accounts.Remove(account);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool AccountExists(long id)
    {
        return _context.Accounts!.Any(x => x.Id == id);
    }

    private bool DuplicateNameExists(string name)
    {
        return _context.Accounts!.Any(x => x.Name == name);
    }

    private bool DuplicateNumberExists(int number)
    {
        return _context.Accounts!.Any(x => x.Number == number);
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
