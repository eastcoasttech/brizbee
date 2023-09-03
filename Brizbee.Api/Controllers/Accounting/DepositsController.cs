//
//  DepositsController.cs
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
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Brizbee.Api.Controllers.Accounting;

[Route("api/Accounting/[controller]")]
[ApiController]
public class DepositsController : ControllerBase
{
    private readonly SqlContext _context;

    public DepositsController(SqlContext context)
    {
        _context = context;
    }

    // GET: api/Accounting/Deposits
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Deposit>>> GetDeposits()
    {
        return await _context.Deposits!
            .ToListAsync();
    }

    // GET api/Accounting/Deposits/5
    [HttpGet("{id:long}")]
    public async Task<ActionResult<Deposit>> GetDeposit(long id)
    {
        var deposit = await _context.Deposits!.FindAsync(id);

        if (deposit == null)
        {
            return NotFound();
        }

        return deposit;
    }

    // POST api/Accounting/Deposits
    [HttpPost]
    public async Task<ActionResult<Payment>> CreateDeposit([FromBody] Deposit depositDto)
    {
        var currentUser = CurrentUser();
        var nowUtc = DateTime.UtcNow;

        await using var databaseTransaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // ------------------------------------------------------------
            // Record the transaction and entries for this deposit.
            // ------------------------------------------------------------

            var undepositedAccount = _context.Accounts!.FirstOrDefault(x => x.Name == "Undeposited Funds");
            var bankAccount = _context.Accounts!.FirstOrDefault(x => x.Id == depositDto.BankAccountId);

            var transaction = new Transaction()
            {
                EnteredOn = depositDto.EnteredOn,
                CreatedAt = nowUtc,
                Description = "",
                OrganizationId = currentUser.OrganizationId,
                ReferenceNumber = "",
                VoucherType = "DEP"
            };

            _context.Transactions!.Add(transaction);

            await _context.SaveChangesAsync();

            var debitEntry = new Entry()
            {
                AccountId = bankAccount!.Id,
                Amount = depositDto.Amount,
                CreatedAt = nowUtc,
                TransactionId = transaction.Id,
                Description = "",
                Type = "D"
            };

            var creditEntry = new Entry()
            {
                AccountId = undepositedAccount!.Id,
                Amount = depositDto.Amount,
                CreatedAt = nowUtc,
                TransactionId = transaction.Id,
                Description = "",
                Type = "C"
            };

            _context.Entries!.Add(debitEntry);
            _context.Entries!.Add(creditEntry);

            await _context.SaveChangesAsync();


            // ------------------------------------------------------------
            // Record the deposit.
            // ------------------------------------------------------------

            var deposit = new Deposit
            {
                EnteredOn = depositDto.EnteredOn,
                CreatedAt = nowUtc,
                ReferenceNumber = depositDto.ReferenceNumber,
                Amount = depositDto.Amount,
                TransactionId = transaction.Id,
                BankAccountId = depositDto.BankAccountId
            };

            _context.Deposits!.Add(deposit);

            await _context.SaveChangesAsync();


            // ------------------------------------------------------------
            // Commit the transaction.
            // ------------------------------------------------------------

            await databaseTransaction.CommitAsync();

            return CreatedAtAction(
                nameof(GetDeposit),
                new { id = deposit.Id },
                deposit);
        }
        catch (Exception ex)
        {
            await databaseTransaction.RollbackAsync();

            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    // DELETE api/Accounting/Deposits/5
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteDeposit(long id)
    {
        var deposit = await _context.Deposits!.FindAsync(id);

        if (deposit == null)
        {
            return NotFound();
        }

        try
        {
            // ------------------------------------------------------------
            // Delete the deposit.
            // ------------------------------------------------------------

            _context.Deposits.Remove(deposit);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
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
