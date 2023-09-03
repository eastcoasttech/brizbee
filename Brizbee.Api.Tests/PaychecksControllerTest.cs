//
//  PaychecksControllerTest.cs
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

using Brizbee.Core.Models.Accounting;
using Dapper;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Brizbee.Api.Tests;

[TestClass]
public class PaychecksControllerTest
{
    public IConfiguration Configuration { get; set; }

    public SqlContext Context { get; set; }

    private readonly Helper _helper = new ();

    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PaychecksControllerTest()
    {
        // Setup configuration
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddJsonFile("appsettings.json");
        Configuration = configurationBuilder.Build();

        // Setup database context
        var options = new DbContextOptionsBuilder<SqlContext>()
            .UseSqlServer(Configuration["ConnectionStrings:SqlContext"])
            .Options;
        Context = new SqlContext(options);
    }
    
    [TestInitialize]
    public void PrepareForTest()
    {
        _helper.Prepare();
    }

    [TestCleanup]
    public void CleanupAfterTest()
    {
        _helper.Cleanup();
    }
    
    [TestMethod]
    public async System.Threading.Tasks.Task CreatePaycheck_Valid_Succeeds()
    {
        // ----------------------------------------------------------------
        // Arrange
        // ----------------------------------------------------------------

        var application = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configurationBuilder) =>
                {
                    configurationBuilder.AddJsonFile("appsettings.json");
                });
            });

        var client = application.CreateClient();

        // User will be authenticated
        var currentUser = Context.Users!
            .First(u => u.EmailAddress == "test.user.a@brizbee.com");

        var token = GenerateJsonWebToken(currentUser.Id, currentUser.EmailAddress!);

        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");


        // ----------------------------------------------------------------
        // Prepare
        // ----------------------------------------------------------------

        var hsaDeduction = new AvailableDeduction()
        {
            CreatedAt = DateTime.UtcNow,
            OrganizationId = currentUser.OrganizationId,
            Name = "Health Savings Account Deduction",
            RateType = "FLAT",
            RateAmount = 100.00M,
            RelationToTaxation = "EMPLOYEE"
        };

        var employeeSsaTaxation = new AvailableTaxation()
        {
            CreatedAt = DateTime.UtcNow,
            OrganizationId = currentUser.OrganizationId,
            Name = "Social Security Employee",
            RateType = "PERCENT",
            RateAmount = 6.2M,
            Entity = "EMPLOYEE",
            LimitAmount = 147000.00M
        };
        
        var employerSsaTaxation = new AvailableTaxation()
        {
            CreatedAt = DateTime.UtcNow,
            OrganizationId = currentUser.OrganizationId,
            Name = "Social Security Employer",
            RateType = "PERCENT",
            RateAmount = 6.2M,
            Entity = "EMPLOYER",
            LimitAmount = 147000.00M
        };
        
        var employeeMedicareTaxation = new AvailableTaxation()
        {
            CreatedAt = DateTime.UtcNow,
            OrganizationId = currentUser.OrganizationId,
            Name = "Medicare Employee",
            RateType = "PERCENT",
            RateAmount = 1.45M,
            Entity = "EMPLOYEE",
            LimitAmount = 147000.00M
        };
        
        var employerMedicareTaxation = new AvailableTaxation()
        {
            CreatedAt = DateTime.UtcNow,
            OrganizationId = currentUser.OrganizationId,
            Name = "Medicare Employer",
            RateType = "PERCENT",
            RateAmount = 1.45M,
            Entity = "EMPLOYER",
            LimitAmount = 147000.00M
        };

        var federalWithholding = new AvailableWithholding()
        {
            CreatedAt = DateTime.UtcNow,
            Level = "FEDERAL",
            OrganizationId = currentUser.OrganizationId,
            Name = "Federal Withholding"
        };
        
        var stateWithholding = new AvailableWithholding()
        {
            CreatedAt = DateTime.UtcNow,
            Level = "STATE",
            OrganizationId = currentUser.OrganizationId,
            Name = "State Withholding"
        };





        
        var bankAccount = await Context.Accounts!.FirstAsync(x => x.Name == "Capital One Spark Checking");
        var payrollExpenses = await Context.Accounts!.FirstAsync(x => x.Name == "Payroll Expenses");
        var payrollLiabilities = await Context.Accounts!.FirstAsync(x => x.Name == "Payroll Liabilities");

        

        // ----------------------------------------------------------------
        // Act
        // ----------------------------------------------------------------

        var contentPaycheck = new
        {
            GrossAmount = 4000.00M,
            EnteredOn = new DateTime(2022, 8, 1),
            Number = "1000",
            UserId = currentUser.Id,
            CalculatedDeductions = new[]
            {
                new
                {
                    AvailableDeduction = new
                    {
                        RelationToTaxation = "PRE"
                    },
                    Amount = 200.00M
                },
                new
                {
                    AvailableDeduction = new
                    {
                        RelationToTaxation = "POST"
                    },
                    Amount = 200.00M
                }
            },
            CalculatedTaxations = new[]
            {
                new
                {
                    AvailableTaxation = new
                    {
                        Entity = "EMPLOYEE"
                    },
                    Amount = 100.00M
                },
                new
                {
                    AvailableTaxation = new
                    {
                        Entity = "EMPLOYER"
                    },
                    Amount = 100.00M
                }
            },
            CalculatedWithholdings = new[]
            {
                new
                {
                    AvailableWithholding = new
                    {
                        Level = "FEDERAL"
                    },
                    Amount = 500.00M
                }
            }
        };
        var json = JsonSerializer.Serialize(contentPaycheck, _options);
        var buffer = Encoding.UTF8.GetBytes(json);
        var byteContent = new ByteArrayContent(buffer);
        byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var response = await client.PostAsync($"api/Accounting/Paychecks?bankAccountId={bankAccount.Id}", byteContent);


        // ----------------------------------------------------------------
        // Assert
        // ----------------------------------------------------------------

        Assert.IsTrue(response.IsSuccessStatusCode);

        const string balanceOfAccountSql = "SELECT [dbo].[udf_AccountBalance] (@MinDate, @MaxDate, @AccountId);";

        var balanceOfBankAccount = await Context.Database.GetDbConnection().QueryFirstOrDefaultAsync<decimal>(
            balanceOfAccountSql,
            param: new
            {
                MinDate = new DateTime(2022, 8, 1),
                MaxDate = new DateTime(2022, 8, 1),
                AccountId = bankAccount!.Id
            });

        Assert.AreEqual(-3000.00M, balanceOfBankAccount);
        
        var balanceOfPayrollExpenses = await Context.Database.GetDbConnection().QueryFirstOrDefaultAsync<decimal>(
            balanceOfAccountSql,
            param: new
            {
                MinDate = new DateTime(2022, 8, 1),
                MaxDate = new DateTime(2022, 8, 1),
                AccountId = payrollExpenses!.Id
            });

        Assert.AreEqual(4100.00M, balanceOfPayrollExpenses);
        
        var balanceOfPayrollLiabilities = await Context.Database.GetDbConnection().QueryFirstOrDefaultAsync<decimal>(
            balanceOfAccountSql,
            param: new
            {
                MinDate = new DateTime(2022, 8, 1),
                MaxDate = new DateTime(2022, 8, 1),
                AccountId = payrollLiabilities!.Id
            });

        Assert.AreEqual(1100.00M, balanceOfPayrollLiabilities);
    }
    
    private string GenerateJsonWebToken(int userId, string emailAddress)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Jwt:Key"]));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[] {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, emailAddress),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(Configuration["Jwt:Issuer"],
            Configuration["Jwt:Issuer"],
            claims,
            expires: DateTime.Now.AddMinutes(120),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
