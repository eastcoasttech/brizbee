//
//  KioskControllerTest.cs
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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Brizbee.Api.Tests
{
    [TestClass]
    public class KioskControllerTest
    {
        public IConfiguration _configuration { get; set; }

        public SqlContext _context { get; set; }

        Helper helper = new Helper();

        JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public KioskControllerTest()
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
        public async System.Threading.Tasks.Task Timecard_Should_ReturnSuccessfully()
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
            var enteredAt = DateTime.Today;
            var minutes = 60;
            var notes = "";

            var response = await client.PostAsync($"api/Kiosk/TimeCard?taskId={taskId}&enteredAt={enteredAt}&minutes={minutes}&notes={notes}", new StringContent(""));


            // ----------------------------------------------------------------
            // Assert
            // ----------------------------------------------------------------

            Assert.IsTrue(response.IsSuccessStatusCode);
        }

        [TestMethod]
        public async System.Threading.Tasks.Task PunchIn_Should_ReturnSuccessfully()
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
            var timeZone = "America/New_York";
            var latitude = "";
            var longitude = "";
            var sourceHardware = "";
            var sourceOperatingSystem = "";
            var sourceOperatingSystemVersion = "";
            var sourceBrowser = "";
            var sourceBrowserVersion = "";

            var response = await client.PostAsync($"api/Kiosk/PunchIn?taskId={taskId}&timeZone={timeZone}&latitude={latitude}&longitude={longitude}&sourceHardware={sourceHardware}&sourceOperatingSystem={sourceOperatingSystem}&sourceOperatingSystemVersion={sourceOperatingSystemVersion}&sourceBrowser={sourceBrowser}&sourceBrowserVersion={sourceBrowserVersion}", new StringContent(""));


            // ----------------------------------------------------------------
            // Assert
            // ----------------------------------------------------------------

            Assert.IsTrue(response.IsSuccessStatusCode);
        }

        [TestMethod]
        public async System.Threading.Tasks.Task PunchOut_Should_ReturnSuccessfully()
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
            // Act - punch in
            // ----------------------------------------------------------------

            var taskId = _context.Tasks
                .Where(t => t.Number == "1000")
                .Select(t => t.Id)
                .FirstOrDefault();
            var timeZone = "America/New_York";
            var latitude = "";
            var longitude = "";
            var sourceHardware = "";
            var sourceOperatingSystem = "";
            var sourceOperatingSystemVersion = "";
            var sourceBrowser = "";
            var sourceBrowserVersion = "";

            var responseToPunchIn = await client.PostAsync($"api/Kiosk/PunchIn?taskId={taskId}&timeZone={timeZone}&latitude={latitude}&longitude={longitude}&sourceHardware={sourceHardware}&sourceOperatingSystem={sourceOperatingSystem}&sourceOperatingSystemVersion={sourceOperatingSystemVersion}&sourceBrowser={sourceBrowser}&sourceBrowserVersion={sourceBrowserVersion}", new StringContent(""));


            // ----------------------------------------------------------------
            // Assert
            // ----------------------------------------------------------------

            Assert.IsTrue(responseToPunchIn.IsSuccessStatusCode);


            // ----------------------------------------------------------------
            // Act again - punch out
            // ----------------------------------------------------------------

            var responseToPunchOut = await client.PostAsync($"api/Kiosk/PunchOut?timeZone={timeZone}&latitude={latitude}&longitude={longitude}&sourceHardware={sourceHardware}&sourceOperatingSystem={sourceOperatingSystem}&sourceOperatingSystemVersion={sourceOperatingSystemVersion}&sourceBrowser={sourceBrowser}&sourceBrowserVersion={sourceBrowserVersion}", new StringContent(""));


            // ----------------------------------------------------------------
            // Assert again
            // ----------------------------------------------------------------

            Assert.IsTrue(responseToPunchOut.IsSuccessStatusCode);
        }

        [TestMethod]
        public async System.Threading.Tasks.Task CurrentPunch_Should_ReturnSuccessfully()
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
            // Act - punch in
            // ----------------------------------------------------------------

            var taskId = _context.Tasks
                .Where(t => t.Number == "1000")
                .Select(t => t.Id)
                .FirstOrDefault();
            var timeZone = "America/New_York";
            var latitude = "";
            var longitude = "";
            var sourceHardware = "";
            var sourceOperatingSystem = "";
            var sourceOperatingSystemVersion = "";
            var sourceBrowser = "";
            var sourceBrowserVersion = "";

            var responseToPunchIn = await client.PostAsync($"api/Kiosk/PunchIn?taskId={taskId}&timeZone={timeZone}&latitude={latitude}&longitude={longitude}&sourceHardware={sourceHardware}&sourceOperatingSystem={sourceOperatingSystem}&sourceOperatingSystemVersion={sourceOperatingSystemVersion}&sourceBrowser={sourceBrowser}&sourceBrowserVersion={sourceBrowserVersion}", new StringContent(""));


            // ----------------------------------------------------------------
            // Assert
            // ----------------------------------------------------------------

            Assert.IsTrue(responseToPunchIn.IsSuccessStatusCode);


            // ----------------------------------------------------------------
            // Act again - check current punch
            // ----------------------------------------------------------------

            var responseToCurrent = await client.GetAsync($"api/Kiosk/Punches/Current");

            var currentPunch = JsonSerializer.Deserialize<Punch>(responseToCurrent.Content.ReadAsStream(), options);


            // ----------------------------------------------------------------
            // Assert again
            // ----------------------------------------------------------------

            Assert.IsNotNull(currentPunch);
            Assert.IsNotNull(currentPunch.Id);
            Assert.AreEqual(currentUser.Id, currentPunch.UserId);
            Assert.AreEqual(taskId, currentPunch.TaskId);
            Assert.IsNotNull(currentPunch.InAt);
            Assert.IsNull(currentPunch.OutAt);
        }

        [TestMethod]
        public async System.Threading.Tasks.Task SearchTasks_Should_ReturnSuccessfully()
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
            var taskNumber = "1000";

            var response = await client.GetAsync($"api/Kiosk/SearchTasks?taskNumber={taskNumber}");

            var task = JsonSerializer.Deserialize<Task>(response.Content.ReadAsStream(), options);


            // ----------------------------------------------------------------
            // Assert
            // ----------------------------------------------------------------

            Assert.IsTrue(response.IsSuccessStatusCode);
            Assert.IsNotNull(task);
            Assert.AreEqual(taskNumber, task!.Number);
            Assert.AreEqual(taskId, task!.Id);
        }

        [TestMethod]
        public async System.Threading.Tasks.Task SearchTasks_Should_Fail()
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

            var taskNumber = "999999";

            var response = await client.GetAsync($"api/Kiosk/SearchTasks?taskNumber={taskNumber}");


            // ----------------------------------------------------------------
            // Assert
            // ----------------------------------------------------------------

            Assert.IsFalse(response.IsSuccessStatusCode);
            Assert.AreEqual(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }

        [TestMethod]
        public async System.Threading.Tasks.Task Customers_Should_ReturnSuccessfully()
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

            var response = await client.GetAsync("api/Kiosk/Customers");

            var customers = JsonSerializer.Deserialize<List<Customer>>(response.Content.ReadAsStream(), options);


            // ----------------------------------------------------------------
            // Assert
            // ----------------------------------------------------------------

            Assert.IsTrue(response.IsSuccessStatusCode);
            Assert.IsNotNull(customers);
            Assert.AreEqual(1, customers.Count);
        }

        [TestMethod]
        public async System.Threading.Tasks.Task Projects_Should_ReturnSuccessfully()
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

            var customerId = _context.Customers
                .Where(c => c.Number == "1000")
                .Select(c => c.Id)
                .FirstOrDefault();

            var response = await client.GetAsync($"api/Kiosk/Projects?customerId={customerId}");

            var projects = JsonSerializer.Deserialize<List<Customer>>(response.Content.ReadAsStream(), options);


            // ----------------------------------------------------------------
            // Assert
            // ----------------------------------------------------------------

            Assert.IsTrue(response.IsSuccessStatusCode);
            Assert.IsNotNull(projects);
            Assert.AreEqual(1, projects.Count);
        }

        [TestMethod]
        public async System.Threading.Tasks.Task Tasks_Should_ReturnSuccessfully()
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

            var projectId = _context.Jobs
                .Where(j => j.Number == "1000")
                .Select(j => j.Id)
                .FirstOrDefault();

            var response = await client.GetAsync($"api/Kiosk/Tasks?projectId={projectId}");

            var tasks = JsonSerializer.Deserialize<List<Customer>>(response.Content.ReadAsStream(), options);


            // ----------------------------------------------------------------
            // Assert
            // ----------------------------------------------------------------

            Assert.IsTrue(response.IsSuccessStatusCode);
            Assert.IsNotNull(tasks);
            Assert.AreEqual(1, tasks.Count);
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TimeZones_Should_ReturnSuccessfully()
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

            var response = await client.GetAsync("api/Kiosk/TimeZones");

            var timeZones = JsonSerializer.Deserialize<List<IanaTimeZone>>(response.Content.ReadAsStream(), options);


            // ----------------------------------------------------------------
            // Assert
            // ----------------------------------------------------------------

            Assert.IsTrue(response.IsSuccessStatusCode);
            Assert.IsNotNull(timeZones);

            var ameriaNewYork = timeZones.Where(t => t.Id == "America/New_York").FirstOrDefault();

            Assert.IsNotNull(ameriaNewYork);
        }

        [TestMethod]
        public async System.Threading.Tasks.Task SearchInventoryItems_Should_ReturnSuccessfully()
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

            var invalidBarCodeValue = "999999999";

            var responseInvalid = await client.GetAsync($"api/Kiosk/InventoryItems/Search?barCodeValue={invalidBarCodeValue}");


            // ----------------------------------------------------------------
            // Assert
            // ----------------------------------------------------------------

            Assert.IsFalse(responseInvalid.IsSuccessStatusCode);


            // ----------------------------------------------------------------
            // Act again
            // ----------------------------------------------------------------

            var validBarCodeValue = "70012";

            var responseValid = await client.GetAsync($"api/Kiosk/InventoryItems/Search?barCodeValue={validBarCodeValue}");


            // ----------------------------------------------------------------
            // Assert again
            // ----------------------------------------------------------------

            Assert.IsTrue(responseValid.IsSuccessStatusCode);
        }

        [TestMethod]
        public async System.Threading.Tasks.Task ConsumeInventoryItems_Should_ReturnSuccessfully()
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
            var timeZone = "America/New_York";
            var latitude = "";
            var longitude = "";
            var sourceHardware = "";
            var sourceOperatingSystem = "";
            var sourceOperatingSystemVersion = "";
            var sourceBrowser = "";
            var sourceBrowserVersion = "";

            var responsePunchIn = await client.PostAsync($"api/Kiosk/PunchIn?taskId={taskId}&timeZone={timeZone}&latitude={latitude}&longitude={longitude}&sourceHardware={sourceHardware}&sourceOperatingSystem={sourceOperatingSystem}&sourceOperatingSystemVersion={sourceOperatingSystemVersion}&sourceBrowser={sourceBrowser}&sourceBrowserVersion={sourceBrowserVersion}", new StringContent(""));

            Assert.IsTrue(responsePunchIn.IsSuccessStatusCode);

            var qbdInventoryItemId = _context.QBDInventoryItems
                .Where(i => i.CustomBarCodeValue == "70012")
                .Select(i => i.Id)
                .FirstOrDefault();

            var quantity = 1;
            var hostname = "HOSTNAME-01";
            var unitOfMeasure = "each";

            var responseConsume = await client.PostAsync($"api/Kiosk/InventoryItems/Consume?qbdInventoryItemId={qbdInventoryItemId}&quantity={quantity}&hostname={hostname}&unitOfMeasure={unitOfMeasure}", new StringContent(""));


            // ----------------------------------------------------------------
            // Assert
            // ----------------------------------------------------------------

            Assert.IsTrue(responseConsume.IsSuccessStatusCode);
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
