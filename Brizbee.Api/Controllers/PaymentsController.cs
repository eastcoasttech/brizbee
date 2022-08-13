//
//  PaymentsController.cs
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
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Brizbee.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly SqlContext _context;

        public PaymentsController(IConfiguration configuration, SqlContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        // GET: api/Payments
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Payment>>> GetPayments()
        {
            return await _context.Payments!
                .ToListAsync();
        }

        // GET api/Payments/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Payment>> GetPayment(long id)
        {
            var payment = await _context.Payments!.FindAsync(id);

            if (payment == null)
            {
                return NotFound();
            }

            return payment;
        }

        // POST api/Payments
        [HttpPost]
        public async Task<ActionResult<Payment>> CreatePayment([FromBody] Payment paymentDTO)
        {
            var currentUser = CurrentUser();
            var nowUtc = DateTime.UtcNow;

            using var databaseTransaction = _context.Database.BeginTransaction();

            try
            {
                // ------------------------------------------------------------
                // Record the transaction and entries for this payment.
                // ------------------------------------------------------------

                var undepositedAccount = _context.Accounts!.FirstOrDefault(x => x.Name == "Undeposited Funds");
                var arAccount = _context.Accounts!.FirstOrDefault(x => x.Name == "Accounts Receivable");

                var transaction = new Transaction()
                {
                    EnteredOn = paymentDTO.EnteredOn,
                    CreatedAt = nowUtc,
                    Description = "",
                    OrganizationId = currentUser.OrganizationId,
                    ReferenceNumber = ""
                };
                
                _context.Transactions!.Add(transaction);
                
                await _context.SaveChangesAsync();

                var debitEntry = new Entry()
                {
                    AccountId = undepositedAccount!.Id,
                    Amount = paymentDTO.Amount,
                    CreatedAt = nowUtc,
                    TransactionId = transaction.Id,
                    Description = "",
                    Type = "D"
                };
                
                var creditEntry = new Entry()
                {
                    AccountId = arAccount!.Id,
                    Amount = paymentDTO.Amount,
                    CreatedAt = nowUtc,
                    TransactionId = transaction.Id,
                    Description = "",
                    Type = "C"
                };
                
                _context.Entries!.Add(debitEntry);
                _context.Entries!.Add(creditEntry);
                
                await _context.SaveChangesAsync();
                

                // ------------------------------------------------------------
                // Record the payment.
                // ------------------------------------------------------------

                var payment = new Payment
                {
                    EnteredOn = paymentDTO.EnteredOn,
                    CreatedAt = nowUtc,
                    ReferenceNumber = paymentDTO.ReferenceNumber,
                    InvoiceId = paymentDTO.InvoiceId,
                    Amount = paymentDTO.Amount,
                    TransactionId = transaction.Id
                };
            
                _context.Payments!.Add(payment);

                await _context.SaveChangesAsync();
                

                // ------------------------------------------------------------
                // Commit the transaction.
                // ------------------------------------------------------------

                await databaseTransaction.CommitAsync();

                return CreatedAtAction(
                    nameof(GetPayment),
                    new { id = payment.Id },
                    payment);
            }
            catch (Exception ex)
            {
                await databaseTransaction.RollbackAsync();

                return  StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        // DELETE api/Payments/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePayment(long id)
        {
            var payment = await _context.Payments!.FindAsync(id);

            if (payment == null)
            {
                return NotFound();
            }

            using var databaseTransaction = _context.Database.BeginTransaction();

            try
            {
                // ------------------------------------------------------------
                // Delete the transaction for the payment.
                // ------------------------------------------------------------

                var transaction = await _context.Transactions!.FindAsync(payment.TransactionId);
                _context.Transactions.Remove(transaction!);
                await _context.SaveChangesAsync();


                // ------------------------------------------------------------
                // Delete the payment.
                // ------------------------------------------------------------

                _context.Payments.Remove(payment);
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

                return  StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        private bool PaymentExists(long id)
        {
            return _context.Payments!.Any(x => x.Id == id);
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
