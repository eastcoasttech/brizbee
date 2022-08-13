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
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Brizbee.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DepositsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly SqlContext _context;

        public DepositsController(IConfiguration configuration, SqlContext context)
        {
            _configuration = configuration;
            _context = context;
        }
        
        // GET: api/Deposits
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Deposit>>> GetDeposits()
        {
            return await _context.Deposits!
                .ToListAsync();
        }

        // GET api/Deposits/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Deposit>> GetDeposit(long id)
        {
            var deposit = await _context.Deposits!.FindAsync(id);

            if (deposit == null)
            {
                return NotFound();
            }

            return deposit;
        }

        // POST api/Deposits
        [HttpPost]
        public async Task<ActionResult<Payment>> CreateDeposit([FromBody] Deposit depositDTO)
        {
            var currentUser = CurrentUser();
            var nowUtc = DateTime.UtcNow;

            using var databaseTransaction = _context.Database.BeginTransaction();

            try
            {
                // ------------------------------------------------------------
                // Record the transaction and entries for this deposit.
                // ------------------------------------------------------------

                var undepositedAccount = _context.Accounts!.FirstOrDefault(x => x.Name == "Undeposited Funds");
                var bankAccount = _context.Accounts!.FirstOrDefault(x => x.Id == depositDTO.BankAccountId);

                var transaction = new Transaction()
                {
                    EnteredOn = depositDTO.EnteredOn,
                    CreatedAt = nowUtc,
                    Description = "",
                    OrganizationId = currentUser.OrganizationId,
                    ReferenceNumber = ""
                };
                
                _context.Transactions!.Add(transaction);
                
                await _context.SaveChangesAsync();

                var debitEntry = new Entry()
                {
                    AccountId = bankAccount!.Id,
                    Amount = depositDTO.Amount,
                    CreatedAt = nowUtc,
                    TransactionId = transaction.Id,
                    Description = "",
                    Type = "D"
                };
                
                var creditEntry = new Entry()
                {
                    AccountId = undepositedAccount!.Id,
                    Amount = depositDTO.Amount,
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
                    EnteredOn = depositDTO.EnteredOn,
                    CreatedAt = nowUtc,
                    ReferenceNumber = depositDTO.ReferenceNumber,
                    Amount = depositDTO.Amount,
                    TransactionId = transaction.Id,
                    BankAccountId = depositDTO.BankAccountId
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

                return  StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
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
