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
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
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
