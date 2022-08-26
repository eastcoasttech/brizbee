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

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using Dapper;
using Brizbee.Core.Models.Accounting;

namespace Brizbee.Api.Tests
{
    [TestClass]
    public class PaychecksControllerTest
    {
        public IConfiguration _configuration { get; set; }

        public SqlContext _context { get; set; }

        private Helper helper = new Helper();

        JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public PaychecksControllerTest()
        {
            // Setup configuration
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonFile("appsettings.json");
            _configuration = configurationBuilder.Build();

            // Setup database context
            var options = new DbContextOptionsBuilder<SqlContext>()
                .UseSqlServer(_configuration["ConnectionStrings:SqlContext"])
                .Options;
            _context = new SqlContext(options);
        }
        
        [TestInitialize]
        public void PrepareForTest()
        {
            helper.Prepare();
        }

        [TestCleanup]
        public void CleanupAfterTest()
        {
            helper.Cleanup();
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
                    builder.ConfigureAppConfiguration((hostingContext, configurationBuilder) =>
                    {
                        configurationBuilder.AddJsonFile("appsettings.json");
                    });
                });

            var client = application.CreateClient();

            // User will be authenticated
            var currentUser = _context.Users!
                .Where(u => u.EmailAddress == "test.user.a@brizbee.com")
                .FirstOrDefault();

            var token = GenerateJSONWebToken(currentUser!.Id, currentUser!.EmailAddress!);

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






            var arAccount = await _context.Accounts!.FirstOrDefaultAsync(x => x.Name == "Accounts Receivable");
            var undepositedAccount = await _context.Accounts!.FirstOrDefaultAsync(x => x.Name == "Undeposited Funds");

            var contentInvoice = new
            {
                EnteredOn = new DateTime(2022, 8, 1),
                ReferenceNumber = "EC100",
                LineItems = new LineItem[]
                {
                    new LineItem()
                    {
                        UnitAmount = 12.53M,
                        Quantity = 2M,
                        Description = "Some Item"
                    },
                    new LineItem()
                    {
                        UnitAmount = 356.99M,
                        Quantity = 1M,
                        Description = "Some Item"
                    }
                }
            };
            var json = JsonSerializer.Serialize(contentInvoice, options);
            var buffer = Encoding.UTF8.GetBytes(json);
            var byteContent = new ByteArrayContent(buffer);
            byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await client.PostAsync($"api/Invoices", byteContent);
            

            // ----------------------------------------------------------------
            // Act
            // ----------------------------------------------------------------

            var contentPayment = new
            {
                EnteredOn = new DateTime(2022, 8, 1),
                ReferenceNumber = "1000",
                Amount = 382.05M
            };
            json = JsonSerializer.Serialize(contentPayment, options);
            buffer = Encoding.UTF8.GetBytes(json);
            byteContent = new ByteArrayContent(buffer);
            byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            response = await client.PostAsync($"api/Payments", byteContent);


            // ----------------------------------------------------------------
            // Assert
            // ----------------------------------------------------------------

            Assert.IsTrue(response.IsSuccessStatusCode);

            var balanceOfAccountSql = "SELECT [dbo].[udf_AccountBalance] (@MinDate, @MaxDate, @AccountId);";

            var balanceOfAccountsReceivable = await _context.Database.GetDbConnection().QueryFirstOrDefaultAsync<decimal>(
                balanceOfAccountSql,
                param: new
                {
                    MinDate = new DateTime(2022, 8, 1),
                    MaxDate = new DateTime(2022, 8, 1),
                    AccountId = arAccount!.Id
                });

            Assert.AreEqual(0.00M, balanceOfAccountsReceivable);
            
            var balanceOfUndepositedFunds = await _context.Database.GetDbConnection().QueryFirstOrDefaultAsync<decimal>(
                balanceOfAccountSql,
                param: new
                {
                    MinDate = new DateTime(2022, 8, 1),
                    MaxDate = new DateTime(2022, 8, 1),
                    AccountId = undepositedAccount!.Id
                });

            Assert.AreEqual(382.05M, balanceOfUndepositedFunds);
        }
        
        private string GenerateJSONWebToken(int userId, string emailAddress)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[] {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, emailAddress),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(_configuration["Jwt:Issuer"],
                _configuration["Jwt:Issuer"],
                claims,
                expires: DateTime.Now.AddMinutes(120),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
