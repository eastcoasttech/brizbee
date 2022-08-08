//
//  TransactionsController.cs
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
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Brizbee.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly SqlContext _context;

        public TransactionsController(IConfiguration configuration, SqlContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        // GET: api/Transactions
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Transaction>>> GetTransactions()
        {
            return await _context.Transactions!
                .ToListAsync();
        }

        // GET api/Transactions/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Transaction>> GetTransaction(int id)
        {
            var transaction = await _context.Transactions!.FindAsync(id);

            if (transaction == null)
            {
                return NotFound();
            }

            return transaction;
        }

        // PUT api/Transactions/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTransaction(long id, [FromBody] Transaction transactionDTO)
        {
            if (id != transactionDTO.Id)
            {
                return BadRequest();
            }

            var transaction = await _context.Transactions!.FindAsync(id);
            if (transaction == null)
            {
                return NotFound();
            }

            transaction.EnteredOn = transactionDTO.EnteredOn;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException) when (!TransactionExists(id))
            {
                return NotFound();
            }

            return NoContent();
        }

        // POST api/Transactions
        [HttpPost]
        public async Task<ActionResult<Transaction>> CreateTransaction([FromBody] Transaction transactionDTO)
        {
            var currentUser = CurrentUser();

            var transaction = new Transaction
            {
                EnteredOn = transactionDTO.EnteredOn,
                Description = transactionDTO.Description,
                CreatedAt = DateTime.UtcNow,
                OrganizationId = currentUser.OrganizationId
            };
            
            _context.Transactions!.Add(transaction);

            foreach (var entryDTO in transactionDTO.Entries!)
            {
                var entry = new Entry()
                {
                    AccountId = entryDTO.AccountId,
                    Amount = entryDTO.Amount,
                    CreatedAt = DateTime.UtcNow,
                    TransactionId = transaction.Id
                };
                
                _context.Entries!.Add(entry);
            }

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetTransaction),
                new { id = transaction.Id },
                transaction);
        }

        // DELETE api/Transactions/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTransaction(int id)
        {
            var transaction = await _context.Transactions!.FindAsync(id);

            if (transaction == null)
            {
                return NotFound();
            }

            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TransactionExists(long id)
        {
            return _context.Transactions!.Any(x => x.Id == id);
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
