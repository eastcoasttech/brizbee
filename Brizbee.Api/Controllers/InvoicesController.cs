//
//  InvoicesController.cs
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
using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Brizbee.Api.Controllers
{
    [Route("api/[controller]")]
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

        // GET: api/Invoices
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Invoice>>> GetInvoices()
        {
            return await _context.Invoices!
                .ToListAsync();
        }

        // GET api/Invoices/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Invoice>> GetInvoice(long id)
        {
            var invoice = await _context.Invoices!.FindAsync(id);

            if (invoice == null)
            {
                return NotFound();
            }

            return invoice;
        }

        // POST api/Invoices
        [HttpPost]
        public async Task<ActionResult<Invoice>> CreateInvoice([FromBody] Invoice invoiceDTO)
        {
            var currentUser = CurrentUser();
            var nowUtc = DateTime.UtcNow;

            var totalAmount = invoiceDTO.LineItems!.Sum(x => x.UnitAmount * x.Quantity);

            using var databaseTransaction = _context.Database.BeginTransaction();

            try
            {
                // ------------------------------------------------------------
                // Record the transaction and entries for this invoice.
                // ------------------------------------------------------------

                var salesAccount = _context.Accounts!.FirstOrDefault(x => x.Name == "Sales");
                var arAccount = _context.Accounts!.FirstOrDefault(x => x.Name == "Accounts Receivable");

                var transaction = new Transaction()
                {
                    EnteredOn = invoiceDTO.EnteredOn,
                    CreatedAt = nowUtc,
                    Description = "",
                    OrganizationId = currentUser.OrganizationId,
                    ReferenceNumber = ""
                };
                
                _context.Transactions!.Add(transaction);
                
                await _context.SaveChangesAsync();

                var debitEntry = new Entry()
                {
                    AccountId = arAccount!.Id,
                    Amount = totalAmount,
                    CreatedAt = nowUtc,
                    TransactionId = transaction.Id,
                    Description = "",
                    Type = "D"
                };
                
                var creditEntry = new Entry()
                {
                    AccountId = salesAccount!.Id,
                    Amount = totalAmount,
                    CreatedAt = nowUtc,
                    TransactionId = transaction.Id,
                    Description = "",
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
                    EnteredOn = invoiceDTO.EnteredOn,
                    CreatedAt = nowUtc,
                    OrganizationId = currentUser.OrganizationId,
                    Number = invoiceDTO.Number,
                    CustomerId = invoiceDTO.CustomerId,
                    TotalAmount = totalAmount,
                    TransactionId = transaction.Id
                };
            
                _context.Invoices!.Add(invoice);

                await _context.SaveChangesAsync();
                
                
                // ------------------------------------------------------------
                // Record the line items for the invoice.
                // ------------------------------------------------------------

                foreach (var lineItemDTO in invoiceDTO.LineItems!)
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

                return  StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        // DELETE api/Invoices/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInvoice(int id)
        {
            var invoice = await _context.Invoices!.FindAsync(id);

            if (invoice == null)
            {
                return NotFound();
            }

            _context.Invoices.Remove(invoice);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool InvoiceExists(long id)
        {
            return _context.Invoices!.Any(x => x.Id == id);
        }

        private User CurrentUser()
        {
            var type = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";
            var claim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == type)!.Value;
            var currentUserId = int.Parse(claim);
            return _context.Users!
                .FirstOrDefault(u => u.Id == currentUserId)!;
        }
    }
}
