//
//  TasksControllerTest.cs
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
using Brizbee.Core.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
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
    public class TasksControllerTest
    {
        public IConfiguration _configuration { get; set; }

        public SqlContext _context { get; set; }

        Helper helper = new Helper();

        JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public TasksControllerTest()
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
        public async System.Threading.Tasks.Task GetAllTasks_Should_ReturnSuccessfully()
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

            var response = await client.GetAsync($"odata/Tasks");


            // ----------------------------------------------------------------
            // Assert
            // ----------------------------------------------------------------

            Assert.IsTrue(response.IsSuccessStatusCode);
        }

        [TestMethod]
        public async System.Threading.Tasks.Task GetTask_Should_ReturnSuccessfully()
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

            var response = await client.GetAsync($"odata/Tasks({taskId})");


            // ----------------------------------------------------------------
            // Assert
            // ----------------------------------------------------------------

            Assert.IsTrue(response.IsSuccessStatusCode);
        }

        [TestMethod]
        public async System.Threading.Tasks.Task CreateTask_Should_ReturnSuccessfully()
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

            var jobId = _context.Jobs
                .Where(j => j.Number == "1000")
                .Select(j => j.Id)
                .FirstOrDefault();

            var content = new
            {
                Number = "1001",
                Name = "Post-Installation",
                JobId = jobId
            };
            var json = JsonSerializer.Serialize(content, options);
            var buffer = Encoding.UTF8.GetBytes(json);
            var byteContent = new ByteArrayContent(buffer);
            byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await client.PostAsync($"odata/Tasks", byteContent);


            // ----------------------------------------------------------------
            // Assert
            // ----------------------------------------------------------------

            Assert.IsTrue(response.IsSuccessStatusCode);
        }

        [TestMethod]
        public async System.Threading.Tasks.Task UpdateTask_Should_ReturnSuccessfully()
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

            var jobId = _context.Jobs
                .Where(j => j.Number == "1000")
                .Select(j => j.Id)
                .FirstOrDefault();

            var content = new
            {
                Number = "1001",
                Name = "Post-Installation",
                JobId = jobId
            };
            var jsonCreate = JsonSerializer.Serialize(content, options);
            var bufferCreate = Encoding.UTF8.GetBytes(jsonCreate);
            var byteContentCreate = new ByteArrayContent(bufferCreate);
            byteContentCreate.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var responseCreate = await client.PostAsync($"odata/Tasks", byteContentCreate);


            // ----------------------------------------------------------------
            // Assert
            // ----------------------------------------------------------------

            Assert.IsTrue(responseCreate.IsSuccessStatusCode);


            // ----------------------------------------------------------------
            // Act again
            // ----------------------------------------------------------------

            var taskId = _context.Tasks
                .Where(t => t.Number == "1001")
                .Select(t => t.Id)
                .FirstOrDefault();

            var changes = new
            {
                Name = "Callback",
            };
            var jsonChanges = JsonSerializer.Serialize(changes, options);
            var bufferChanges = Encoding.UTF8.GetBytes(jsonChanges);
            var byteContentChanges = new ByteArrayContent(bufferChanges);
            byteContentChanges.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var responseUpdate = await client.PatchAsync($"odata/Tasks({taskId})", byteContentChanges);


            // ----------------------------------------------------------------
            // Assert again
            // ----------------------------------------------------------------

            Assert.IsTrue(responseUpdate.IsSuccessStatusCode);
        }

        [TestMethod]
        public async System.Threading.Tasks.Task DeleteTask_Should_ReturnSuccessfully()
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

            var jobId = _context.Jobs
                .Where(j => j.Number == "1000")
                .Select(j => j.Id)
                .FirstOrDefault();

            var content = new
            {
                Number = "1001",
                Name = "Post-Installation",
                JobId = jobId
            };
            var json = JsonSerializer.Serialize(content, options);
            var buffer = Encoding.UTF8.GetBytes(json);
            var byteContent = new ByteArrayContent(buffer);
            byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var responseCreate = await client.PostAsync($"odata/Tasks", byteContent);


            // ----------------------------------------------------------------
            // Assert
            // ----------------------------------------------------------------

            Assert.IsTrue(responseCreate.IsSuccessStatusCode);


            // ----------------------------------------------------------------
            // Act again
            // ----------------------------------------------------------------

            var taskId = _context.Tasks
                .Where(t => t.Number == "1001")
                .Select(t => t.Id)
                .FirstOrDefault();

            var responseDelete = await client.DeleteAsync($"odata/Tasks({taskId})");


            // ----------------------------------------------------------------
            // Assert again
            // ----------------------------------------------------------------

            Assert.IsTrue(responseDelete.IsSuccessStatusCode);
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
