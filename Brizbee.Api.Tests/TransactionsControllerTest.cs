//
//  TransactionsControllerTest.cs
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

namespace Brizbee.Api.Tests
{
    [TestClass]
    public class TransactionsControllerTest
    {
        public IConfiguration _configuration { get; set; }

        public SqlContext _context { get; set; }

        private Helper helper = new Helper();

        JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public TransactionsControllerTest()
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
        public async System.Threading.Tasks.Task GetTransactions_Valid_Succeeds()
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
            // Act
            // ----------------------------------------------------------------

            var response = await client.GetAsync($"api/Transactions");


            // ----------------------------------------------------------------
            // Assert
            // ----------------------------------------------------------------

            Assert.IsTrue(response.IsSuccessStatusCode);
        }
        
        [TestMethod]
        public async System.Threading.Tasks.Task CreateTransaction_ValidPhoneExpense_Succeeds()
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
            // Act
            // ----------------------------------------------------------------

            var phoneExpenseAccount = await _context.Accounts!.FirstOrDefaultAsync(x => x.Name == "Phone Expense");
            var apAccount = await _context.Accounts!.FirstOrDefaultAsync(x => x.Name == "Accounts Payable");

            var content = new
            {
                EnteredOn = new DateTime(2022, 8, 1),
                Description = "Phone Bill",
                ReferenceNumber = "DEBIT",
                Entries = new Entry[]
                {
                    new Entry()
                    {
                        AccountId = phoneExpenseAccount!.Id,
                        Amount = 109.57M,
                        Description = "",
                        Type = "D"
                    },
                    new Entry()
                    {
                        AccountId = apAccount!.Id,
                        Amount = 109.57M,
                        Description = "",
                        Type = "C"
                    }
                }
            };
            var json = JsonSerializer.Serialize(content, options);
            var buffer = Encoding.UTF8.GetBytes(json);
            var byteContent = new ByteArrayContent(buffer);
            byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await client.PostAsync($"api/Transactions", byteContent);


            // ----------------------------------------------------------------
            // Assert
            // ----------------------------------------------------------------

            Assert.IsTrue(response.IsSuccessStatusCode);
        }
        
        [TestMethod]
        public async System.Threading.Tasks.Task CreateTransaction_ZeroDebitsAndCredits_Fails()
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
            // Act
            // ----------------------------------------------------------------

            var phoneExpenseAccount = await _context.Accounts!.FirstOrDefaultAsync(x => x.Name == "Phone Expense");
            var apAccount = await _context.Accounts!.FirstOrDefaultAsync(x => x.Name == "Accounts Payable");

            var content = new
            {
                EnteredOn = new DateTime(2022, 8, 1),
                Description = "Phone Bill",
                Entries = new Entry[]
                {
                    new Entry()
                    {
                        AccountId = phoneExpenseAccount!.Id,
                        Amount = 0.00M
                    },
                    new Entry()
                    {
                        AccountId = apAccount!.Id,
                        Amount = 0.00M
                    }
                }
            };
            var json = JsonSerializer.Serialize(content, options);
            var buffer = Encoding.UTF8.GetBytes(json);
            var byteContent = new ByteArrayContent(buffer);
            byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await client.PostAsync($"api/Transactions", byteContent);


            // ----------------------------------------------------------------
            // Assert
            // ----------------------------------------------------------------

            Assert.IsFalse(response.IsSuccessStatusCode);
        }
        
        [TestMethod]
        public async System.Threading.Tasks.Task CreateTransaction_MismatchingDebitAndCredits_Fails()
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
            // Act
            // ----------------------------------------------------------------

            var phoneExpenseAccount = await _context.Accounts!.FirstOrDefaultAsync(x => x.Name == "Phone Expense");
            var apAccount = await _context.Accounts!.FirstOrDefaultAsync(x => x.Name == "Accounts Payable");

            var content = new
            {
                EnteredOn = new DateTime(2022, 8, 1),
                Description = "Phone Bill",
                Entries = new Entry[]
                {
                    new Entry()
                    {
                        AccountId = phoneExpenseAccount!.Id,
                        Amount = 10.00M
                    },
                    new Entry()
                    {
                        AccountId = apAccount!.Id,
                        Amount = 20.00M
                    }
                }
            };
            var json = JsonSerializer.Serialize(content, options);
            var buffer = Encoding.UTF8.GetBytes(json);
            var byteContent = new ByteArrayContent(buffer);
            byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await client.PostAsync($"api/Transactions", byteContent);


            // ----------------------------------------------------------------
            // Assert
            // ----------------------------------------------------------------

            Assert.IsFalse(response.IsSuccessStatusCode);
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
