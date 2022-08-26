//
//  PaychecksController.cs
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

namespace Brizbee.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaychecksController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly SqlContext _context;

        public PaychecksController(IConfiguration configuration, SqlContext context)
        {
            _configuration = configuration;
            _context = context;
        }
        
        // GET: api/Paychecks
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Paycheck>>> GetPaychecks()
        {
            return await _context.Paychecks!
                .ToListAsync();
        }

        // GET api/Paychecks/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Paycheck>> GetPaycheck(long id)
        {
            var paycheck = await _context.Paychecks!.FindAsync(id);

            if (paycheck == null)
            {
                return NotFound();
            }

            return paycheck;
        }

        // POST api/Paychecks
        [HttpPost]
        public async Task<ActionResult<Paycheck>> CreatePaycheck([FromBody] Paycheck paycheckDto)
        {
            var currentUser = CurrentUser();
            var nowUtc = DateTime.UtcNow;
            var grossAmount = paycheckDto.GrossAmount;
            var netAmount = paycheckDto.GrossAmount;
            var preTaxDeductions = 0.00M;
            var postTaxDeductions = 0.00M;
            var employeeTaxes = 0.00M;
            var employerTaxes = 0.00M;
            var levelWithholdings = new Dictionary<string, decimal>(0);

            using var databaseTransaction = _context.Database.BeginTransaction();
            
            try
            {
                // ------------------------------------------------------------
                // Calculate each pre-tax deduction configuration for this user.
                // ------------------------------------------------------------

                foreach (var calculatedDeduction in paycheckDto.CalculatedDeductions!.Where(x => x.AvailableDeduction!.RelationToTaxation == "PRE"))
                {
                    preTaxDeductions += calculatedDeduction.Amount;
                }


                // ------------------------------------------------------------
                // Calculate each post-tax deduction configuration for this user.
                // ------------------------------------------------------------

                foreach (var calculatedDeduction in paycheckDto.CalculatedDeductions!.Where(x => x.AvailableDeduction!.RelationToTaxation == "POST"))
                {
                    postTaxDeductions += calculatedDeduction.Amount;
                }
                

                // ------------------------------------------------------------
                // Calculate each employee taxation configuration for this user.
                // ------------------------------------------------------------

                foreach (var calculatedTaxation in paycheckDto.CalculatedTaxations!.Where(x => x.AvailableTaxation!.Entity == "EMPLOYEE"))
                {
                    employeeTaxes += -calculatedTaxation.Amount;
                }
                

                // ------------------------------------------------------------
                // Calculate each employer taxation configuration for this user.
                // ------------------------------------------------------------

                foreach (var calculatedTaxation in paycheckDto.CalculatedTaxations!.Where(x => x.AvailableTaxation!.Entity == "EMPLOYER"))
                {
                    employerTaxes += -calculatedTaxation.Amount;
                }


                // ------------------------------------------------------------
                // Calculate each withholding configuration for this user.
                // ------------------------------------------------------------

                var levels = paycheckDto.CalculatedWithholdings!
                    .GroupBy(x => x.AvailableWithholding!.Level)
                    .Select(x => x.Key);

                foreach (var level in levels)
                {
                    foreach (var calculatedWithholding in paycheckDto.CalculatedWithholdings!.Where(x => x.AvailableWithholding!.Level == level))
                    {
                        if (!levelWithholdings.ContainsKey(level))
                            levelWithholdings[level] = 0.00M;

                        levelWithholdings[level] += calculatedWithholding.Amount;
                    }
                }




                // ------------------------------------------------------------
                // Record the transaction and entries for this paycheck.
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
                // Record the paycheck.
                // ------------------------------------------------------------

                var paycheck = new Paycheck()
                {
                    EnteredOn = paycheckDto.EnteredOn,
                    CreatedAt = nowUtc,
                    GrossAmount = paycheckDto.GrossAmount,
                    NetAmount = netAmount,
                    Number = paycheckDto.Number,
                    OrganizationId = currentUser.OrganizationId,
                    UserId = paycheckDto.UserId
                };

                _context.Paychecks!.Add(paycheck);

                await _context.SaveChangesAsync();
                

                // ------------------------------------------------------------
                // Commit the transaction.
                // ------------------------------------------------------------

                await databaseTransaction.CommitAsync();

                return CreatedAtAction(
                    nameof(GetPaycheck),
                    new { id = paycheck.Id },
                    paycheck);
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
