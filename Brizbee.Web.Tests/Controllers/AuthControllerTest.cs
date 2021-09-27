//
//  AuthControllerTest.cs
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
using Brizbee.Common.Security;
using Brizbee.Web.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Results;

namespace Brizbee.Web.Tests.Controllers
{
    [TestClass]
    public class AuthControllerTest
    {
        Helper helper = new Helper();

        [TestInitialize]
        public void PrepareForTest()
        {
            helper.Cleanup(); // Fresh start.
        }

        [TestCleanup]
        public void CleanupAfterTest()
        {
            helper.Cleanup();
        }

        [TestMethod]
        public void Register_Should_ReturnSuccessfully()
        {
            // ----------------------------------------------------------------
            // Arrange
            // ----------------------------------------------------------------

            var controller = new AuthController();
            var controllerContext = new HttpControllerContext();

            var configuration = new HttpConfiguration();
            var request = new HttpRequestMessage();
            request.Properties[System.Web.Http.Hosting.HttpPropertyKeys.HttpConfigurationKey] = configuration;
            request.Headers.Add("Accept", "application/json");

            controllerContext.Request = request;
            controller.ControllerContext = controllerContext;


            // ----------------------------------------------------------------
            // Act
            // ----------------------------------------------------------------
            var registration = new Registration()
            {
                Organization = new Organization()
                {
                    Name = "Test Corporation",
                    PlanId = 1
                },
                User = new User()
                {
                    Name = "Test User",
                    EmailAddress = "test.user@corporation.com",
                    Password = "password"
                }
            };

            var actionResult = controller.Register(registration);

            var content = actionResult as CreatedNegotiatedContentResult<User>;


            // ----------------------------------------------------------------
            // Assert
            // ----------------------------------------------------------------

            Assert.IsNotNull(content);
            Assert.IsNotNull(content.Content);
        }
    }
}
