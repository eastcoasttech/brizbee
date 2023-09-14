using Brizbee.Core.Models;
using Brizbee.Core.Models.Accounting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Brizbee.Api.Controllers.Accounting;

[Route("api/Accounting/[controller]")]
[ApiController]
public class ChecksController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly SqlContext _context;

    public ChecksController(IConfiguration configuration, SqlContext context)
    {
        _configuration = configuration;
        _context = context;
    }
    
    // GET api/Accounting/Checks/5
    [HttpGet("{id:long}")]
    public async Task<ActionResult<Check>> GetCheck(long id)
    {
        var check = await _context.Checks!
            .Include(x => x.Vendor!)
            .Include(x => x.CheckExpenseLines!)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (check == null)
        {
            return NotFound();
        }

        return check;
    }

    // POST api/Accounting/Checks
    [HttpPost]
    public async Task<ActionResult<Check>> CreateCheck([FromBody] Check checkDto)
    {
        var currentUser = CurrentUser();
        var nowUtc = DateTime.UtcNow;

        var totalAmount = checkDto.CheckExpenseLines!.Sum(x => x.Amount);

        await using var databaseTransaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // ------------------------------------------------------------
            // Record the transaction for this check.
            // ------------------------------------------------------------

            var bankAccount = _context.Accounts!.FirstOrDefault(x => x.Id == checkDto.BankAccountId);

            var transaction = new Transaction()
            {
                EnteredOn = checkDto.EnteredOn,
                CreatedAt = nowUtc,
                Description = string.Empty,
                OrganizationId = currentUser.OrganizationId,
                ReferenceNumber = string.Empty,
                VoucherType = "CHK"
            };

            _context.Transactions!.Add(transaction);

            await _context.SaveChangesAsync();


            // ------------------------------------------------------------
            // Record the check.
            // ------------------------------------------------------------

            var check = new Check
            {
                EnteredOn = checkDto.EnteredOn,
                CreatedAt = nowUtc,
                OrganizationId = currentUser.OrganizationId,
                Number = checkDto.Number,
                VendorId = checkDto.VendorId,
                TotalAmount = totalAmount,
                TransactionId = transaction.Id,
                Memo = checkDto.Memo
            };

            _context.Checks!.Add(check);

            await _context.SaveChangesAsync();

            
            // ------------------------------------------------------------
            // Record the expense lines and their entries.
            // ------------------------------------------------------------

            foreach (var checkExpenseLineDto in checkDto.CheckExpenseLines!)
            {
                var expenseAccount = _context.Accounts!.FirstOrDefault(x => x.Id == checkExpenseLineDto.AccountId);

                var debitEntry = new Entry()
                {
                    Amount = checkExpenseLineDto.Amount,
                    AccountId = expenseAccount!.Id,
                    CreatedAt = nowUtc,
                    Description = string.Empty,
                    TransactionId = transaction.Id,
                    Type = "D"
                };

                _context.Entries!.Add(debitEntry);
                
                var checkExpenseLine = new CheckExpenseLine()
                {
                    Amount = checkExpenseLineDto.Amount,
                    AccountId = expenseAccount!.Id,
                    CheckId = check.Id,
                    CreatedAt = nowUtc,
                    EntryId = debitEntry.Id
                };

                _context.CheckExpenseLines!.Add(checkExpenseLine);
            }

            var creditEntry = new Entry()
            {
                AccountId = bankAccount!.Id,
                Amount = totalAmount,
                CreatedAt = nowUtc,
                TransactionId = transaction.Id,
                Description = string.Empty,
                Type = "C"
            };

            _context.Entries!.Add(creditEntry);

            await _context.SaveChangesAsync();


            // ------------------------------------------------------------
            // Commit the transaction.
            // ------------------------------------------------------------

            await databaseTransaction.CommitAsync();

            return CreatedAtAction(
                nameof(GetCheck),
                new { id = check.Id },
                check);
        }
        catch (Exception ex)
        {
            await databaseTransaction.RollbackAsync();

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
