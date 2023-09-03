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
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Globalization;
using System.Net;

namespace Brizbee.Api.Controllers.Accounting;

[Route("api/Accounting/[controller]")]
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

    // GET: api/Accounting/Paychecks
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Paycheck>>> GetPaychecks(
        [FromQuery] int skip = 0, [FromQuery] int pageSize = 1000,
        [FromQuery] string orderBy = "PAYCHECKS/NAME", [FromQuery] string orderByDirection = "ASC")
    {
        if (pageSize > 1000)
        {
            BadRequest();
        }

        var currentUser = CurrentUser();

        var paychecks = new List<Paycheck>();
        await using var connection = new SqlConnection(_configuration.GetConnectionString("SqlContext"));

        connection.Open();

        // Determine the order by columns.
        var orderByFormatted = orderBy.ToUpperInvariant() switch
        {
            "PAYCHECKS/NUMBER" => "[Number]",
            _ => "[Number]"
        };

        // Determine the order direction.
        var orderByDirectionFormatted = orderByDirection.ToUpperInvariant() switch
        {
            "ASC" => "ASC",
            "DESC" => "DESC",
            _ => "ASC"
        };

        var parameters = new DynamicParameters();

        // Common clause.
        parameters.Add("@OrganizationId", currentUser.OrganizationId);

        // Get the count.
        const string countSql = @"
            SELECT
                COUNT(*)
            FROM [dbo].[Paychecks] AS [P]
            WHERE
                [P].[OrganizationId] = @OrganizationId;";

        var total = await connection.QuerySingleAsync<int>(countSql, parameters);

        // Paging parameters.
        parameters.Add("@Skip", skip);
        parameters.Add("@PageSize", pageSize);

        // Get the records.
        var recordsSql = $@"
            SELECT
                [P].[CreatedAt],
                [P].[Id],
                [P].[Number],
                [P].[OrganizationId]
            FROM [dbo].[Paychecks] AS [P]
            WHERE
                [P].[OrganizationId] = @OrganizationId
            ORDER BY
                [P].{orderByFormatted} {orderByDirectionFormatted}
            OFFSET @Skip ROWS
            FETCH NEXT @PageSize ROWS ONLY;";

        var results = await connection.QueryAsync<Paycheck>(recordsSql, parameters);

        paychecks.AddRange(results);

        // Determine page count.
        var pageCount = total > 0
            ? (int)Math.Ceiling(total / (double)pageSize)
            : 0;

        // Set headers for paging.
        HttpContext.Response.Headers.Add("X-Paging-PageSize", pageSize.ToString(CultureInfo.InvariantCulture));
        HttpContext.Response.Headers.Add("X-Paging-PageCount", pageCount.ToString(CultureInfo.InvariantCulture));
        HttpContext.Response.Headers.Add("X-Paging-TotalRecordCount", total.ToString(CultureInfo.InvariantCulture));

        return new JsonResult(paychecks)
        {
            StatusCode = (int)HttpStatusCode.OK
        };
    }

    // GET api/Accounting/Paychecks/5
    [HttpGet("{id:long}")]
    public async Task<ActionResult<Paycheck>> GetPaycheck(long id)
    {
        var paycheck = await _context.Paychecks!.FindAsync(id);

        if (paycheck == null)
        {
            return NotFound();
        }

        return paycheck;
    }

    // POST api/Accounting/Paychecks
    [HttpPost]
    public async Task<ActionResult<Paycheck>> CreatePaycheck([FromBody] Paycheck paycheckDto, [FromQuery] long bankAccountId)
    {
        var currentUser = CurrentUser();
        var nowUtc = DateTime.UtcNow;
        var grossAmount = paycheckDto.GrossAmount;
        var preTaxDeductions = 0.00M;
        var postTaxDeductions = 0.00M;
        var employeeTaxes = 0.00M;
        var employerTaxes = 0.00M;
        var levelWithholding = new Dictionary<string, decimal>(0);

        await using var databaseTransaction = await _context.Database.BeginTransactionAsync();

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
                employeeTaxes += calculatedTaxation.Amount;
            }


            // ------------------------------------------------------------
            // Calculate each employer taxation configuration for this user.
            // ------------------------------------------------------------

            foreach (var calculatedTaxation in paycheckDto.CalculatedTaxations!.Where(x => x.AvailableTaxation!.Entity == "EMPLOYER"))
            {
                employerTaxes += calculatedTaxation.Amount;
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
                    levelWithholding.TryAdd(level, 0.00M);

                    levelWithholding[level] += calculatedWithholding.Amount;
                }
            }
            

            // ------------------------------------------------------------
            // Determine the amounts for net, liabilities, and employer taxes.
            // ------------------------------------------------------------

            var totalWithholding = 0.00M;

            foreach (var withholding in levelWithholding)
            {
                totalWithholding += withholding.Value;
            }

            // 4,000 GROSS, 100 EMPLOYEE, 200 PRETAX, 200 POST TAX, 500 WITH
            // = NET 3,000
            var netAmount = grossAmount - employeeTaxes - preTaxDeductions - postTaxDeductions - totalWithholding;
            
            // 100 EMPLOYEE + 200 PRETAX, 200 POST TAX, 500 WITH
            // = LIABILITIES 1,000
            var liabilitiesAmount = employeeTaxes + preTaxDeductions + postTaxDeductions + totalWithholding;

            // 100 EMPLOYER
            var employerAmount = employerTaxes;


            // ------------------------------------------------------------
            // Record the transaction and entries for this paycheck.
            // ------------------------------------------------------------

            var payrollExpensesAccount = _context.Accounts!.FirstOrDefault(x => x.Name == "Payroll Expenses"); // Gross Pay and Employer Taxes
            
            if (payrollExpensesAccount == null)
            {
                return BadRequest();
            }

            var payrollLiabilitiesAccount = _context.Accounts!.FirstOrDefault(x => x.Name == "Payroll Liabilities"); // Employee Taxes, Withholding, and Deductions
            
            if (payrollLiabilitiesAccount == null)
            {
                return BadRequest();
            }

            var bankAccount = await _context.Accounts!.FindAsync(bankAccountId);

            if (bankAccount == null)
            {
                return BadRequest();
            }

            var transaction = new Transaction()
            {
                EnteredOn = paycheckDto.EnteredOn,
                CreatedAt = nowUtc,
                Description = "",
                OrganizationId = currentUser.OrganizationId,
                ReferenceNumber = "",
                VoucherType = "PAY"
            };

            _context.Transactions!.Add(transaction);

            await _context.SaveChangesAsync();

            
            var creditBankAccountEntry = new Entry()
            {
                AccountId = bankAccount.Id,
                Amount = netAmount, // 3,000
                CreatedAt = nowUtc,
                TransactionId = transaction.Id,
                Description = "",
                Type = "C"
            };

            var debitPayrollExpensesEntry = new Entry()
            {
                AccountId = payrollExpensesAccount!.Id,
                Amount = grossAmount + employerAmount, // 3,000 + 1,000 + 100
                CreatedAt = nowUtc,
                TransactionId = transaction.Id,
                Description = "",
                Type = "D"
            };

            var creditPayrollLiabilitiesEntry = new Entry()
            {
                AccountId = payrollLiabilitiesAccount!.Id,
                Amount = totalWithholding + preTaxDeductions + postTaxDeductions + employeeTaxes + employerAmount, // 1,000 + 100
                CreatedAt = nowUtc,
                TransactionId = transaction.Id,
                Description = "",
                Type = "C"
            };
            
            _context.Entries!.Add(creditBankAccountEntry);
            _context.Entries!.Add(debitPayrollExpensesEntry);
            _context.Entries!.Add(creditPayrollLiabilitiesEntry);

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
