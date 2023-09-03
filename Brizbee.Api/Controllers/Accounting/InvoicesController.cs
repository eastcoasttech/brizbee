//
//  InvoicesController.cs
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

using Brizbee.Api.Serialization.Dto;
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
public class InvoicesController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly SqlContext _context;

    public InvoicesController(IConfiguration configuration, SqlContext context)
    {
        _configuration = configuration;
        _context = context;
    }

    // GET: api/Accounting/Invoices
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Invoice>>> GetInvoices(
        [FromQuery] int skip = 0, [FromQuery] int pageSize = 1000,
        [FromQuery] string orderBy = "INVOICES/ENTERED_ON", [FromQuery] string orderByDirection = "ASC")
    {
        if (pageSize > 1000)
        {
            BadRequest();
        }

        var currentUser = CurrentUser();

        var invoices = new List<Invoice>();
        await using var connection = new SqlConnection(_configuration.GetConnectionString("SqlContext"));

        connection.Open();

        // Determine the order by columns.
        var orderByFormatted = orderBy.ToUpperInvariant() switch
        {
            "INVOICES/ENTERED_ON" => "[I].[EnteredOn]",
            "INVOICES/NUMBER" => "[I].[Number]",
            _ => "[I].[Number]"
        };

        // Determine the order direction.
        var orderByDirectionFormatted = orderByDirection.ToUpperInvariant() switch
        {
            "ASC" => "ASC",
            "DESC" => "DESC",
            _ => "ASC"
        };

        var whereClauses = "";
        var parameters = new DynamicParameters();

        // Common clause.
        parameters.Add("@OrganizationId", currentUser.OrganizationId);

        // Get the count.
        var countSql = $@"
                SELECT
                    COUNT(*)
                FROM [dbo].[Invoices] AS [I]
                WHERE
                    [I].[OrganizationId] = @OrganizationId {whereClauses};";

        var total = await connection.QuerySingleAsync<int>(countSql, parameters);

        // Paging parameters.
        parameters.Add("@Skip", skip);
        parameters.Add("@PageSize", pageSize);

        // Get the records.
        var recordsSql = $@"
                SELECT
                    [I].[CreatedAt] AS [Invoice_CreatedAt],
                    [I].[CustomerId] AS [Invoice_CustomerId],
                    [I].[EnteredOn] AS [Invoice_EnteredOn],
                    [I].[Id] AS [Invoice_Id],
                    [I].[Number] AS [Invoice_Number],
                    [I].[OrganizationId] AS [Invoice_OrganizationId],
                    [I].[TotalAmount] AS [Invoice_TotalAmount],
                    [I].[TransactionId] AS [Invoice_TransactionId],

                    [C].[CreatedAt] AS [Customer_CreatedAt],
                    [C].[Description] AS [Customer_Description],
                    [C].[Id] AS [Customer_Id],
                    [C].[Name] AS [Customer_Name],
                    [C].[Number] AS [Customer_Number],
                    [C].[OrganizationId] AS [Customer_OrganizationId]
                FROM [dbo].[Invoices] AS [I]
                INNER JOIN [dbo].[Customers] AS [C]
                    ON [C].[Id] = [I].[CustomerId]
                WHERE
                    [I].[OrganizationId] = @OrganizationId {whereClauses}
                ORDER BY
                    {orderByFormatted} {orderByDirectionFormatted}
                OFFSET @Skip ROWS
                FETCH NEXT @PageSize ROWS ONLY;";

        var results = await connection.QueryAsync<InvoiceDto>(recordsSql, parameters);

        foreach (var result in results)
        {
            invoices.Add(new Invoice()
            {
                CreatedAt = result.Invoice_CreatedAt,
                CustomerId = result.Invoice_CustomerId,
                EnteredOn = result.Invoice_EnteredOn,
                Id = result.Invoice_Id,
                Number = result.Invoice_Number,
                OrganizationId = result.Invoice_OrganizationId,
                TotalAmount = result.Invoice_TotalAmount,
                TransactionId = result.Invoice_TransactionId,

                Customer = new Customer()
                {
                    CreatedAt = result.Customer_CreatedAt,
                    Description = result.Customer_Description,
                    Id = result.Customer_Id,
                    Name = result.Customer_Name,
                    Number = result.Customer_Number,
                    OrganizationId = result.Customer_OrganizationId
                }
            });
        }

        // Determine page count.
        var pageCount = total > 0
            ? (int)Math.Ceiling(total / (double)pageSize)
            : 0;

        // Set headers for paging.
        HttpContext.Response.Headers.Add("X-Paging-PageSize", pageSize.ToString(CultureInfo.InvariantCulture));
        HttpContext.Response.Headers.Add("X-Paging-PageCount", pageCount.ToString(CultureInfo.InvariantCulture));
        HttpContext.Response.Headers.Add("X-Paging-TotalRecordCount", total.ToString(CultureInfo.InvariantCulture));

        return new JsonResult(invoices)
        {
            StatusCode = (int)HttpStatusCode.OK
        };
    }

    // GET api/Accounting/Invoices/5
    [HttpGet("{id:long}")]
    public async Task<ActionResult<Invoice>> GetInvoice(long id)
    {
        var invoice = await _context.Invoices!
            .Include(x => x.Customer!)
            .Include(x => x.LineItems!)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (invoice == null)
        {
            return NotFound();
        }

        return invoice;
    }

    // POST api/Accounting/Invoices
    [HttpPost]
    public async Task<ActionResult<Invoice>> CreateInvoice([FromBody] Invoice invoiceDto)
    {
        var currentUser = CurrentUser();
        var nowUtc = DateTime.UtcNow;

        var totalAmount = invoiceDto.LineItems!.Sum(x => x.UnitAmount * x.Quantity);

        await using var databaseTransaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // ------------------------------------------------------------
            // Record the transaction and entries for this invoice.
            // ------------------------------------------------------------

            var salesAccount = _context.Accounts!.FirstOrDefault(x => x.Name == "Sales");
            var arAccount = _context.Accounts!.FirstOrDefault(x => x.Name == "Accounts Receivable");

            var transaction = new Transaction()
            {
                EnteredOn = invoiceDto.EnteredOn,
                CreatedAt = nowUtc,
                Description = string.Empty,
                OrganizationId = currentUser.OrganizationId,
                ReferenceNumber = string.Empty,
                VoucherType = "INV"
            };

            _context.Transactions!.Add(transaction);

            await _context.SaveChangesAsync();

            var debitEntry = new Entry()
            {
                AccountId = arAccount!.Id,
                Amount = totalAmount,
                CreatedAt = nowUtc,
                TransactionId = transaction.Id,
                Description = string.Empty,
                Type = "D"
            };

            var creditEntry = new Entry()
            {
                AccountId = salesAccount!.Id,
                Amount = totalAmount,
                CreatedAt = nowUtc,
                TransactionId = transaction.Id,
                Description = string.Empty,
                Type = "C"
            };

            _context.Entries!.Add(debitEntry);
            _context.Entries!.Add(creditEntry);

            await _context.SaveChangesAsync();


            // ------------------------------------------------------------
            // Record the invoice.
            // ------------------------------------------------------------

            var invoice = new Invoice
            {
                EnteredOn = invoiceDto.EnteredOn,
                CreatedAt = nowUtc,
                OrganizationId = currentUser.OrganizationId,
                Number = invoiceDto.Number,
                CustomerId = invoiceDto.CustomerId,
                TotalAmount = totalAmount,
                TransactionId = transaction.Id
            };

            _context.Invoices!.Add(invoice);

            await _context.SaveChangesAsync();


            // ------------------------------------------------------------
            // Record the line items for the invoice.
            // ------------------------------------------------------------

            foreach (var lineItemDTO in invoiceDto.LineItems!)
            {
                var lineItem = new LineItem()
                {
                    InvoiceId = invoice.Id,
                    Quantity = lineItemDTO.Quantity,
                    UnitAmount = lineItemDTO.UnitAmount,
                    TotalAmount = lineItemDTO.UnitAmount * lineItemDTO.Quantity,
                    CreatedAt = nowUtc,
                    Description = lineItemDTO.Description
                };

                _context.LineItems!.Add(lineItem);
            }

            await _context.SaveChangesAsync();


            // ------------------------------------------------------------
            // Commit the transaction.
            // ------------------------------------------------------------

            await databaseTransaction.CommitAsync();

            return CreatedAtAction(
                nameof(GetInvoice),
                new { id = invoice.Id },
                invoice);
        }
        catch (Exception ex)
        {
            await databaseTransaction.RollbackAsync();

            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    // DELETE api/Accounting/Invoices/5
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteInvoice(long id)
    {
        var invoice = await _context.Invoices!.FindAsync(id);

        if (invoice == null)
        {
            return NotFound();
        }

        await using var databaseTransaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // ------------------------------------------------------------
            // Delete the transaction for the invoice.
            // ------------------------------------------------------------

            var transaction = await _context.Transactions!.FindAsync(invoice.TransactionId);
            _context.Transactions.Remove(transaction!);
            await _context.SaveChangesAsync();


            // ------------------------------------------------------------
            // Delete the invoice.
            // ------------------------------------------------------------

            _context.Invoices.Remove(invoice);
            await _context.SaveChangesAsync();


            // ------------------------------------------------------------
            // Commit the transaction.
            // ------------------------------------------------------------

            await databaseTransaction.CommitAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            await databaseTransaction.RollbackAsync();

            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    private bool InvoiceExists(long id)
    {
        return _context.Invoices!.Any(x => x.Id == id);
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
