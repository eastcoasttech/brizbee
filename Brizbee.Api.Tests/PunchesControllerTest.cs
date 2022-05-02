//
//  PunchesControllerTest.cs
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
using System.Diagnostics;
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
    public class PunchesControllerTest
    {
        public IConfiguration _configuration { get; set; }

        public SqlContext _context { get; set; }

        Helper helper = new Helper();

        JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public PunchesControllerTest()
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
        public async System.Threading.Tasks.Task GetAllPunches_Should_ReturnSuccessfully()
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
            var currentUser = _context.Users
                .Where(u => u.EmailAddress == "test.user.a@brizbee.com")
                .FirstOrDefault();

            var token = GenerateJSONWebToken(currentUser!.Id, currentUser!.EmailAddress!);

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");


            // ----------------------------------------------------------------
            // Act
            // ----------------------------------------------------------------

            var response = await client.GetAsync($"odata/Punches");


            // ----------------------------------------------------------------
            // Assert
            // ----------------------------------------------------------------

            Assert.IsTrue(response.IsSuccessStatusCode);
        }

        [TestMethod]
        public async System.Threading.Tasks.Task GetPunch_Should_ReturnSuccessfully()
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
            var currentUser = _context.Users
                .Where(u => u.EmailAddress == "test.user.a@brizbee.com")
                .FirstOrDefault();

            var token = GenerateJSONWebToken(currentUser!.Id, currentUser!.EmailAddress!);

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");


            // ----------------------------------------------------------------
            // Act
            // ----------------------------------------------------------------

            var punchId = _context.Punches
                .Select(r => r.Id)
                .FirstOrDefault();

            var response = await client.GetAsync($"odata/Punches({punchId})");


            // ----------------------------------------------------------------
            // Assert
            // ----------------------------------------------------------------

            Assert.IsTrue(response.IsSuccessStatusCode);
        }

        [TestMethod]
        public async System.Threading.Tasks.Task CreatePunch_Should_ReturnSuccessfully()
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
            var currentUser = _context.Users
                .Where(u => u.EmailAddress == "test.user.a@brizbee.com")
                .FirstOrDefault();

            var token = GenerateJSONWebToken(currentUser!.Id, currentUser!.EmailAddress!);

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");


            // ----------------------------------------------------------------
            // Act
            // ----------------------------------------------------------------

            var taskId = _context.Tasks
                .Where(t => t.Number == "1000")
                .Select(t => t.Id)
                .FirstOrDefault();

            var content = new
            {
                TaskId = taskId,
                InAt = new DateTime(2022, 1, 2, 8, 0, 0).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                OutAt = new DateTime(2022, 1, 2, 17, 0, 0).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                UserId = currentUser.Id
            };
            var json = JsonSerializer.Serialize(content, options);
            var buffer = Encoding.UTF8.GetBytes(json);
            var byteContent = new ByteArrayContent(buffer);
            byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await client.PostAsync($"odata/Punches", byteContent);


            // ----------------------------------------------------------------
            // Assert
            // ----------------------------------------------------------------

            Assert.IsTrue(response.IsSuccessStatusCode);
        }

        [TestMethod]
        public async System.Threading.Tasks.Task CreatePunch_Should_FailForOverlap()
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
            var currentUser = _context.Users
                .Where(u => u.EmailAddress == "test.user.a@brizbee.com")
                .FirstOrDefault();

            var token = GenerateJSONWebToken(currentUser!.Id, currentUser!.EmailAddress!);

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");


            // ----------------------------------------------------------------
            // Create a punch
            // ----------------------------------------------------------------

            var taskId = _context.Tasks
                .Where(t => t.Number == "1000")
                .Select(t => t.Id)
                .FirstOrDefault();

            var contentForFirstPunch = new
            {
                TaskId = taskId,
                InAt = new DateTime(2022, 1, 2, 8, 0, 0).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                OutAt = new DateTime(2022, 1, 2, 17, 0, 0).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                UserId = currentUser.Id
            };
            var jsonForFirstPunch = JsonSerializer.Serialize(contentForFirstPunch, options);
            var bufferForFirstPunch = Encoding.UTF8.GetBytes(jsonForFirstPunch);
            var byteContentForFirstPunch = new ByteArrayContent(bufferForFirstPunch);
            byteContentForFirstPunch.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var responseForFirstPunch = await client.PostAsync($"odata/Punches", byteContentForFirstPunch);


            // ----------------------------------------------------------------
            // Act
            // ----------------------------------------------------------------

            var contentForContainedOverlappingPunch = new
            {
                TaskId = taskId,
                InAt = new DateTime(2022, 1, 2, 9, 0, 0).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                OutAt = new DateTime(2022, 1, 2, 10, 0, 0).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                UserId = currentUser.Id
            };
            var jsonForContainedOverlappingPunch = JsonSerializer.Serialize(contentForContainedOverlappingPunch, options);
            var bufferForContainedOverlappingPunch = Encoding.UTF8.GetBytes(jsonForContainedOverlappingPunch);
            var byteContentForContainedOverlappingPunch = new ByteArrayContent(bufferForContainedOverlappingPunch);
            byteContentForContainedOverlappingPunch.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var responseForContainedOverlappingPunch = await client.PostAsync($"odata/Punches", byteContentForContainedOverlappingPunch);


            // ----------------------------------------------------------------
            // Act again
            // ----------------------------------------------------------------

            var contentForInAtOverlappingPunch = new
            {
                TaskId = taskId,
                InAt = new DateTime(2022, 1, 2, 9, 0, 0).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                OutAt = new DateTime(2022, 1, 3, 9, 0, 0).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                UserId = currentUser.Id
            };
            var jsonForInAtOverlappingPunch = JsonSerializer.Serialize(contentForInAtOverlappingPunch, options);
            var bufferForInAtOverlappingPunch = Encoding.UTF8.GetBytes(jsonForInAtOverlappingPunch);
            var byteContentForInAtOverlappingPunch = new ByteArrayContent(bufferForInAtOverlappingPunch);
            byteContentForInAtOverlappingPunch.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var responseForInAtOverlappingPunch = await client.PostAsync($"odata/Punches", byteContentForInAtOverlappingPunch);


            // ----------------------------------------------------------------
            // Act again
            // ----------------------------------------------------------------

            var contentForOutAtOverlappingPunch = new
            {
                TaskId = taskId,
                InAt = new DateTime(2022, 1, 1, 9, 0, 0).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                OutAt = new DateTime(2022, 1, 2, 9, 0, 0).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                UserId = currentUser.Id
            };
            var jsonForOutAtOverlappingPunch = JsonSerializer.Serialize(contentForOutAtOverlappingPunch, options);
            var bufferForOutAtOverlappingPunch = Encoding.UTF8.GetBytes(jsonForOutAtOverlappingPunch);
            var byteContentForOutAtOverlappingPunch = new ByteArrayContent(bufferForOutAtOverlappingPunch);
            byteContentForOutAtOverlappingPunch.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var responseForOutAtOverlappingPunch = await client.PostAsync($"odata/Punches", byteContentForOutAtOverlappingPunch);


            // ----------------------------------------------------------------
            // Assert
            // ----------------------------------------------------------------

            Assert.IsFalse(responseForContainedOverlappingPunch.IsSuccessStatusCode);
            Assert.IsFalse(responseForInAtOverlappingPunch.IsSuccessStatusCode);
            Assert.IsFalse(responseForOutAtOverlappingPunch.IsSuccessStatusCode);
        }

        [TestMethod]
        public async System.Threading.Tasks.Task CreatePunch_Should_SucceedForBackToBackPunches()
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
            var currentUser = _context.Users
                .Where(u => u.EmailAddress == "test.user.a@brizbee.com")
                .FirstOrDefault();

            var token = GenerateJSONWebToken(currentUser!.Id, currentUser!.EmailAddress!);

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");


            // ----------------------------------------------------------------
            // Create a punch
            // ----------------------------------------------------------------

            var taskId = _context.Tasks
                .Where(t => t.Number == "1000")
                .Select(t => t.Id)
                .FirstOrDefault();

            var contentForFirstPunch = new
            {
                TaskId = taskId,
                InAt = new DateTime(2022, 1, 2, 8, 0, 0).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                OutAt = new DateTime(2022, 1, 2, 17, 0, 0).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                UserId = currentUser.Id
            };
            var jsonForFirstPunch = JsonSerializer.Serialize(contentForFirstPunch, options);
            var bufferForFirstPunch = Encoding.UTF8.GetBytes(jsonForFirstPunch);
            var byteContentForFirstPunch = new ByteArrayContent(bufferForFirstPunch);
            byteContentForFirstPunch.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var responseForFirstPunch = await client.PostAsync($"odata/Punches", byteContentForFirstPunch);


            // ----------------------------------------------------------------
            // Act
            // ----------------------------------------------------------------

            var contentForContainedOverlappingPunch = new
            {
                TaskId = taskId,
                InAt = new DateTime(2022, 1, 2, 17, 0, 0).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                OutAt = new DateTime(2022, 1, 2, 20, 0, 0).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                UserId = currentUser.Id
            };
            var jsonForContainedOverlappingPunch = JsonSerializer.Serialize(contentForContainedOverlappingPunch, options);
            var bufferForContainedOverlappingPunch = Encoding.UTF8.GetBytes(jsonForContainedOverlappingPunch);
            var byteContentForContainedOverlappingPunch = new ByteArrayContent(bufferForContainedOverlappingPunch);
            byteContentForContainedOverlappingPunch.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var responseForContainedOverlappingPunch = await client.PostAsync($"odata/Punches", byteContentForContainedOverlappingPunch);


            // ----------------------------------------------------------------
            // Assert
            // ----------------------------------------------------------------

            Assert.IsTrue(responseForContainedOverlappingPunch.IsSuccessStatusCode);
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
