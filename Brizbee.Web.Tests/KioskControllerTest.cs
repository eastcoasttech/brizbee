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

using Brizbee.Common.Models;
using Brizbee.Web.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Net.Http;
using System.Security.Principal;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Results;

namespace Brizbee.Web.Tests
{
    [TestClass]
    public class KioskControllerTest
    {
        SqlContext _context = new SqlContext();
        Helper helper = new Helper();

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
        public void Timecard_Should_ReturnSuccessfully()
        {
            // ----------------------------------------------------------------
            // Arrange
            // ----------------------------------------------------------------

            var controller = new KioskController();
            var controllerContext = new HttpControllerContext();

            // User will be authenticated
            var userId = _context.Users
                .Where(u => u.EmailAddress == "test.user.a@brizbee.com")
                .FirstOrDefault()
                .Id
                .ToString();
            controllerContext.RequestContext.Principal = new GenericPrincipal(new GenericIdentity(userId), new string[] { });

            var configuration = new HttpConfiguration();
            var request = new HttpRequestMessage();
            request.Properties[System.Web.Http.Hosting.HttpPropertyKeys.HttpConfigurationKey] = configuration;
            request.Headers.Add("Accept", "application/json");

            controllerContext.Request = request;
            controller.ControllerContext = controllerContext;


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

            var actionResult = controller.Timecard(taskId, enteredAt, minutes, notes);

            var content = actionResult as OkNegotiatedContentResult<TimesheetEntry>;


            // ----------------------------------------------------------------
            // Assert
            // ----------------------------------------------------------------

            Assert.IsNotNull(content);
            Assert.IsNotNull(content.Content);
            Assert.AreEqual(DateTime.Today.Date, content.Content.EnteredAt.Date);
            Assert.AreEqual(minutes, content.Content.Minutes);
            Assert.AreEqual(int.Parse(userId), content.Content.UserId);
        }

        [TestMethod]
        public void PunchIn_Should_ReturnSuccessfully()
        {
            // ----------------------------------------------------------------
            // Arrange
            // ----------------------------------------------------------------

            var controller = new KioskController();
            var controllerContext = new HttpControllerContext();

            // User will be authenticated
            var userId = _context.Users
                .Where(u => u.EmailAddress == "test.user.a@brizbee.com")
                .FirstOrDefault()
                .Id
                .ToString();
            controllerContext.RequestContext.Principal = new GenericPrincipal(new GenericIdentity(userId), new string[] { });

            var configuration = new HttpConfiguration();
            var request = new HttpRequestMessage();
            request.Properties[System.Web.Http.Hosting.HttpPropertyKeys.HttpConfigurationKey] = configuration;
            request.Headers.Add("Accept", "application/json");

            controllerContext.Request = request;
            controller.ControllerContext = controllerContext;


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

            var actionResult = controller.PunchIn(taskId, timeZone, latitude, longitude, sourceHardware, sourceOperatingSystem, sourceOperatingSystemVersion, sourceBrowser, sourceBrowserVersion);

            var content = actionResult as OkNegotiatedContentResult<Punch>;


            // ----------------------------------------------------------------
            // Assert
            // ----------------------------------------------------------------

            Assert.IsNotNull(content);
            Assert.IsNotNull(content.Content);
            Assert.AreEqual(int.Parse(userId), content.Content.UserId);
            Assert.IsNull(content.Content.OutAt);
        }

        [TestMethod]
        public void PunchOut_Should_ReturnSuccessfully()
        {
            // ----------------------------------------------------------------
            // Arrange
            // ----------------------------------------------------------------

            var controller = new KioskController();
            var controllerContext = new HttpControllerContext();

            // User will be authenticated
            var userId = _context.Users
                .Where(u => u.EmailAddress == "test.user.a@brizbee.com")
                .FirstOrDefault()
                .Id
                .ToString();
            controllerContext.RequestContext.Principal = new GenericPrincipal(new GenericIdentity(userId), new string[] { });

            var configuration = new HttpConfiguration();
            var request = new HttpRequestMessage();
            request.Properties[System.Web.Http.Hosting.HttpPropertyKeys.HttpConfigurationKey] = configuration;
            request.Headers.Add("Accept", "application/json");

            controllerContext.Request = request;
            controller.ControllerContext = controllerContext;


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

            var inActionResult = controller.PunchIn(taskId, timeZone, latitude, longitude, sourceHardware, sourceOperatingSystem, sourceOperatingSystemVersion, sourceBrowser, sourceBrowserVersion);

            var inContent = inActionResult as OkNegotiatedContentResult<Punch>;


            // ----------------------------------------------------------------
            // Assert
            // ----------------------------------------------------------------

            Assert.IsNotNull(inContent);
            Assert.IsNotNull(inContent.Content);
            Assert.AreEqual(int.Parse(userId), inContent.Content.UserId);
            Assert.IsNull(inContent.Content.OutAt);


            // ----------------------------------------------------------------
            // Act again - punch out
            // ----------------------------------------------------------------

            var outActionResult = controller.PunchOut(timeZone, latitude, longitude, sourceHardware, sourceOperatingSystem, sourceOperatingSystemVersion, sourceBrowser, sourceBrowserVersion);

            var outContent = outActionResult as OkNegotiatedContentResult<Punch>;


            // ----------------------------------------------------------------
            // Assert
            // ----------------------------------------------------------------

            Assert.IsNotNull(outContent);
            Assert.IsNotNull(outContent.Content);
            Assert.AreEqual(int.Parse(userId), outContent.Content.UserId);
            Assert.IsNotNull(outContent.Content.OutAt);
        }

        [TestMethod]
        public void CurrentPunch_Should_ReturnSuccessfully()
        {
            // ----------------------------------------------------------------
            // Arrange
            // ----------------------------------------------------------------

            var controller = new KioskController();
            var controllerContext = new HttpControllerContext();

            // User will be authenticated
            var userId = _context.Users
                .Where(u => u.EmailAddress == "test.user.a@brizbee.com")
                .FirstOrDefault()
                .Id
                .ToString();
            controllerContext.RequestContext.Principal = new GenericPrincipal(new GenericIdentity(userId), new string[] { });

            var configuration = new HttpConfiguration();
            var request = new HttpRequestMessage();
            request.Properties[System.Web.Http.Hosting.HttpPropertyKeys.HttpConfigurationKey] = configuration;
            request.Headers.Add("Accept", "application/json");

            controllerContext.Request = request;
            controller.ControllerContext = controllerContext;


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

            var inActionResult = controller.PunchIn(taskId, timeZone, latitude, longitude, sourceHardware, sourceOperatingSystem, sourceOperatingSystemVersion, sourceBrowser, sourceBrowserVersion);

            var inContent = inActionResult as OkNegotiatedContentResult<Punch>;


            // ----------------------------------------------------------------
            // Assert
            // ----------------------------------------------------------------

            Assert.IsNotNull(inContent);
            Assert.IsNotNull(inContent.Content);
            Assert.AreEqual(int.Parse(userId), inContent.Content.UserId);
            Assert.IsNull(inContent.Content.OutAt);


            // ----------------------------------------------------------------
            // Act again - check current punch
            // ----------------------------------------------------------------

            var currentActionResult = controller.CurrentPunch();

            var currentContent = currentActionResult as OkNegotiatedContentResult<Punch>;


            // ----------------------------------------------------------------
            // Assert
            // ----------------------------------------------------------------

            Assert.IsNotNull(currentContent);
            Assert.IsNotNull(currentContent.Content);
            Assert.AreEqual(int.Parse(userId), currentContent.Content.UserId);
            Assert.IsNull(currentContent.Content.OutAt);
        }
    }
}
