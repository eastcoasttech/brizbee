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
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Brizbee.Api.Controllers
{
    [Route("api/[controller]")]
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

        // GET: api/Accounts
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Account>>> GetAccounts()
        {
            return await _context.Accounts!
                .ToListAsync();
        }

        // GET api/Accounts/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Account>> GetAccount(long id)
        {
            var account = await _context.Accounts!.FindAsync(id);

            if (account == null)
            {
                return NotFound();
            }

            return account;
        }

        // PUT api/Accounts/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAccount(long id, [FromBody] Account accountDTO)
        {
            if (id != accountDTO.Id)
            {
                return BadRequest();
            }
            
            var account = await _context.Accounts!.FindAsync(id);
            if (account == null)
            {
                return NotFound();
            }

            account.Name = accountDTO.Name;
            account.Number = accountDTO.Number;
            account.Description = accountDTO.Description;

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

        // POST api/Accounts
        [HttpPost]
        public async Task<ActionResult<Account>> CreateAccount([FromBody] Account accountDTO)
        {
            var currentUser = CurrentUser();

            var validTypes = new string[]
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

            if (!validTypes.Contains(accountDTO.Type))
                return BadRequest();

            if (DuplicateNameExists(accountDTO.Name))
                return BadRequest();
            
            if (DuplicateNumberExists(accountDTO.Number))
                return BadRequest();

            var account = new Account
            {
                Name = accountDTO.Name,
                Description = accountDTO.Description,
                CreatedAt = DateTime.UtcNow,
                Number = accountDTO.Number,
                OrganizationId = currentUser.OrganizationId,
                Type = accountDTO.Type
            };

            _context.Accounts!.Add(account);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetAccount),
                new { id = account.Id },
                account);
        }

        // DELETE api/Accounts/5
        [HttpDelete("{id}")]
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
            var type = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";
            var claim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == type)!.Value;
            var currentUserId = int.Parse(claim);
            return _context.Users!
                .FirstOrDefault(u => u.Id == currentUserId)!;
        }
    }
}
