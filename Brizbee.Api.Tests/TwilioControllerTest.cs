//
//  TwilioControllerTest.cs
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
    public class TwilioControllerTest
    {
        public IConfiguration _configuration { get; set; }

        public SqlContext _context { get; set; }

        private readonly string _brizbeeAuth;

        Helper helper = new Helper();

        JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public TwilioControllerTest()
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

            _brizbeeAuth = _configuration["TwilioAuthKeyForBrizbeeApi"];
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
        public async System.Threading.Tasks.Task GetCode_Should_ReturnSuccessfully()
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


            // ----------------------------------------------------------------
            // Act
            // ----------------------------------------------------------------

            var digits = "";

            var response = await client.GetAsync($"api/Twilio/Code?brizbeeAuth={_brizbeeAuth}&digits={digits}");


            // ----------------------------------------------------------------
            // Assert
            // ----------------------------------------------------------------

            Assert.IsTrue(response.IsSuccessStatusCode);

            var content = await response.Content.ReadAsStringAsync();

            var expected = "Thank you for calling brizbee! Please enter your organization code and press the pound key.";

            Assert.IsTrue(content.Contains(expected));
        }

        [TestMethod]
        public async System.Threading.Tasks.Task GetPin_Should_ReturnSuccessfully()
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


            // ----------------------------------------------------------------
            // Act
            // ----------------------------------------------------------------

            var digits = "";

            var response = await client.GetAsync($"api/Twilio/Pin?brizbeeAuth={_brizbeeAuth}&digits={digits}");


            // ----------------------------------------------------------------
            // Assert
            // ----------------------------------------------------------------

            Assert.IsTrue(response.IsSuccessStatusCode);

            var content = await response.Content.ReadAsStringAsync();

            var expected = "Okay, please enter your PIN number and press the pound key.";

            Assert.IsTrue(content.Contains(expected));
        }

        [TestMethod]
        public async System.Threading.Tasks.Task GetStatus_Should_ReturnSuccessfullyWhenUserDoesNotHavePermission()
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
                .Include(u => u.Organization)
                .Where(u => u.EmailAddress == "test.user.a@brizbee.com")
                .FirstOrDefault();

            currentUser!.UsesTouchToneClock = false;
            _context.SaveChanges();


            // ----------------------------------------------------------------
            // Act
            // ----------------------------------------------------------------

            var digits = currentUser!.Pin;
            var organizationCode = currentUser!.Organization!.Code;
            var from = "+10001112233";

            var response = await client.GetAsync($"api/Twilio/Status?brizbeeAuth={_brizbeeAuth}&digits={digits}&organizationCode={organizationCode}&from={from}");


            // ----------------------------------------------------------------
            // Assert
            // ----------------------------------------------------------------

            Assert.IsTrue(response.IsSuccessStatusCode);

            var content = await response.Content.ReadAsStringAsync();

            var expected = "I'm sorry, but you do not have permission to use the touch-tone clock.";

            Assert.IsTrue(content.Contains(expected));
        }

        [TestMethod]
        public async System.Threading.Tasks.Task GetStatus_Should_ReturnSuccessfullyWhenPunchedOut()
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
                .Include(u => u.Organization)
                .Where(u => u.EmailAddress == "test.user.a@brizbee.com")
                .FirstOrDefault();


            // ----------------------------------------------------------------
            // Act
            // ----------------------------------------------------------------

            var digits = currentUser!.Pin;
            var organizationCode = currentUser!.Organization!.Code;
            var from = "+10001112233";

            var response = await client.GetAsync($"api/Twilio/Status?brizbeeAuth={_brizbeeAuth}&digits={digits}&organizationCode={organizationCode}&from={from}");


            // ----------------------------------------------------------------
            // Assert
            // ----------------------------------------------------------------

            Assert.IsTrue(response.IsSuccessStatusCode);

            var content = await response.Content.ReadAsStringAsync();

            var expected = $"Hello {currentUser.Name}! You are not punched <emphasis>in</emphasis> right now. Please press 1 to punch in.";

            Assert.IsTrue(content.Contains(expected));
        }
        [TestMethod]
        public async System.Threading.Tasks.Task GetStatus_Should_ReturnSuccessfullyWhenPunchedIn()
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

            var currentUser = _context.Users
                .Include(u => u.Organization)
                .Where(u => u.EmailAddress == "test.user.a@brizbee.com")
                .FirstOrDefault();

            var task = _context.Tasks
                .Include(t => t.Job)
                .Include(t => t.Job!.Customer)
                .Where(t => t.Number == "1000")
                .FirstOrDefault();
            var timeZone = "America/New_York";
            var latitude = "";
            var longitude = "";
            var sourceHardware = "";
            var sourceOperatingSystem = "";
            var sourceOperatingSystemVersion = "";
            var sourceBrowser = "";
            var sourceBrowserVersion = "";

            // Authenticate the user
            var token = GenerateJSONWebToken(currentUser!.Id, currentUser!.EmailAddress!);

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            // Punch in the user
            await client.PostAsync($"api/Kiosk/PunchIn?taskId={task!.Id}&timeZone={timeZone}&latitude={latitude}&longitude={longitude}&sourceHardware={sourceHardware}&sourceOperatingSystem={sourceOperatingSystem}&sourceOperatingSystemVersion={sourceOperatingSystemVersion}&sourceBrowser={sourceBrowser}&sourceBrowserVersion={sourceBrowserVersion}", new StringContent(""));

            // Clear the authentication
            client.DefaultRequestHeaders.Remove("Authorization");


            // ----------------------------------------------------------------
            // Act
            // ----------------------------------------------------------------

            var digits = currentUser!.Pin;
            var organizationCode = currentUser!.Organization!.Code;
            var from = "+10001112233";

            var response = await client.GetAsync($"api/Twilio/Status?brizbeeAuth={_brizbeeAuth}&digits={digits}&organizationCode={organizationCode}&from={from}");


            // ----------------------------------------------------------------
            // Assert
            // ----------------------------------------------------------------

            Assert.IsTrue(response.IsSuccessStatusCode);

            var content = await response.Content.ReadAsStringAsync();

            var taskNumberBrokenOut = task.Number!.ToString().Aggregate(string.Empty, (c, i) => c + i + ' ');
            var jobNumberBrokenOut = task.Job!.Number!.ToString().Aggregate(string.Empty, (c, i) => c + i + ' ');
            var customerNumberBrokenOut = task.Job!.Customer!.Number!.ToString().Aggregate(string.Empty, (c, i) => c + i + ' ');

            var expected = $"Hello {currentUser.Name}! You are currently punched <emphasis>in</emphasis> to task, {taskNumberBrokenOut} - {task.Name}, for the job, {jobNumberBrokenOut} - {task.Job!.Name}, and customer, {customerNumberBrokenOut} - {task.Job!.Customer!.Name}. Please press 1 to punch in on another task or job. Or, press 2 to punch out.";

            Assert.IsTrue(content.Contains(expected));
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
                expires: DateTime.UtcNow.AddMinutes(120),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
