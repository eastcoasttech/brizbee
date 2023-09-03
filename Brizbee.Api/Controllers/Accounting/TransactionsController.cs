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
using Brizbee.Core.Models.Accounting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Brizbee.Api.Controllers.Accounting;

[Route("api/Accounting/[controller]")]
[ApiController]
public class TransactionsController : ControllerBase
{
    private readonly SqlContext _context;

    public TransactionsController(SqlContext context)
    {
        _context = context;
    }

    // GET: api/Accounting/Transactions
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Transaction>>> GetTransactions()
    {
        return await _context.Transactions!
            .ToListAsync();
    }

    // GET api/Accounting/Transactions/5
    [HttpGet("{id:long}")]
    public async Task<ActionResult<Transaction>> GetTransaction(long id)
    {
        var transaction = await _context.Transactions!.FindAsync(id);

        if (transaction == null)
        {
            return NotFound();
        }

        return transaction;
    }

    // PUT api/Accounting/Transactions/5
    [HttpPut("{id:long}")]
    public async Task<IActionResult> UpdateTransaction(long id, [FromBody] Transaction transactionDto)
    {
        if (id != transactionDto.Id)
        {
            return BadRequest();
        }

        var transaction = await _context.Transactions!.FindAsync(id);
        if (transaction == null)
        {
            return NotFound();
        }

        transaction.EnteredOn = transactionDto.EnteredOn;
        transaction.ReferenceNumber = transactionDto.ReferenceNumber;
        transaction.Description = transactionDto.Description;

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

    // POST api/Accounting/Transactions
    [HttpPost]
    public async Task<ActionResult<Transaction>> CreateTransaction([FromBody] Transaction transactionDto)
    {
        var currentUser = CurrentUser();

        // Validate that all the debits and credits equal
        // and do not add up to zero.
        var creditSum = transactionDto.Entries!.Where(e => e.Type == "C").Sum(e => e.Amount);
        var debitSum = transactionDto.Entries!.Where(e => e.Type == "D").Sum(e => e.Amount);

        if (creditSum != debitSum)
        {
            return BadRequest();
        }

        if (creditSum == 0.00M)
        {
            return BadRequest();
        }

        var transaction = new Transaction
        {
            EnteredOn = transactionDto.EnteredOn,
            Description = transactionDto.Description,
            CreatedAt = DateTime.UtcNow,
            OrganizationId = currentUser.OrganizationId,
            ReferenceNumber = transactionDto.ReferenceNumber,
            VoucherType = "GEN"
        };

        _context.Transactions!.Add(transaction);

        await _context.SaveChangesAsync();

        foreach (var entryDto in transactionDto.Entries!)
        {
            var entry = new Entry()
            {
                AccountId = entryDto.AccountId,
                Amount = entryDto.Amount,
                CreatedAt = DateTime.UtcNow,
                TransactionId = transaction.Id,
                Description = entryDto.Description,
                Type = entryDto.Type
            };

            _context.Entries!.Add(entry);
        }

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetTransaction),
            new { id = transaction.Id },
            transaction);
    }

    // DELETE api/Accounting/Transactions/5
    [HttpDelete("{id:int}")]
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
        const string type = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";
        var claim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == type)!.Value;
        var currentUserId = int.Parse(claim);
        return _context.Users!
            .FirstOrDefault(u => u.Id == currentUserId)!;
    }
}
