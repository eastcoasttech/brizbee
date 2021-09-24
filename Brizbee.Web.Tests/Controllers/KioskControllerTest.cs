using Brizbee.Common.Models;
using Brizbee.Web.Controllers;
using Dapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Security.Principal;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Results;

namespace Brizbee.Web.Tests.Controllers
{
    [TestClass]
    public class KioskControllerTest
    {
        private readonly SqlContext db = new SqlContext();

        [TestInitialize]
        public void PrepareForTest()
        {
            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["SqlContext"].ToString()))
            {
                connection.Open();

                var organizations = new List<Organization>()
                {
                    new Organization()
                    {
                        Id = 1,
                        Name = "Western Widgets Corporation",
                        Code = "1234",
                        CreatedAt = DateTime.UtcNow,
                        MinutesFormat = "minutes",
                        StripeCustomerId = string.Format("RANDOM{0}", new SecurityService().GenerateRandomString()),
                        StripeSubscriptionId = string.Format("RANDOM{0}", new SecurityService().GenerateRandomString())
                    }
                };
                var users = new List<User>()
                {
                    new User()
                    {
                        Id = 1,
                        Name = "Christopher Hitchens",
                        CreatedAt = DateTime.UtcNow,
                        IsDeleted = false,
                        OrganizationId = 1,
                        EmailAddress = "christopher.hitchens@western.com",
                        TimeZone = "America/New_York",
                        Pin = "1111",
                        Role = ""
                    },
                    new User()
                    {
                        Id = 2,
                        Name = "George Will",
                        CreatedAt = DateTime.UtcNow,
                        IsDeleted = false,
                        OrganizationId = 1,
                        EmailAddress = "george.will@western.com",
                        TimeZone = "America/New_York",
                        Pin = "2222",
                        Role = ""
                    },
                    new User()
                    {
                        Id = 3,
                        Name = "Ada Lovelace",
                        CreatedAt = DateTime.UtcNow,
                        IsDeleted = false,
                        OrganizationId = 1,
                        EmailAddress = "ada.lovelace@western.com",
                        TimeZone = "America/New_York",
                        Pin = "3333",
                        Role = ""
                    },
                    new User()
                    {
                        Id = 4,
                        Name = "Jack Welch",
                        CreatedAt = DateTime.UtcNow,
                        IsDeleted = false,
                        OrganizationId = 1,
                        EmailAddress = "jack.welch@western.com",
                        TimeZone = "America/New_York",
                        Pin = "4444",
                        Role = ""
                    }
                };

                // Insert the organizations.
                var organizationsSql = @"
                    SET IDENTITY_INSERT dbo.Organizations ON;

                    INSERT INTO Organizations (
                        Id,
                        CreatedAt,
                        Code,
                        Name,
                        MinutesFormat,
                        StripeCustomerId,
                        StripeSubscriptionId
                    ) VALUES (
                        @Id,
                        @CreatedAt,
                        @Code,
                        @Name,
                        @MinutesFormat,
                        @StripeCustomerId,
                        @StripeSubscriptionId
                    );

                    SET IDENTITY_INSERT dbo.Organizations OFF;";
                connection.Execute(organizationsSql, organizations);

                // Insert the users.
                var usersSql = @"
                    SET IDENTITY_INSERT dbo.Users ON;

                    INSERT INTO Users (
                        Id,
                        CreatedAt,
                        OrganizationId,
                        EmailAddress,
                        Name,
                        TimeZone,
                        IsDeleted,
                        Pin,
                        [Role],
                        UsesMobileClock,
                        UsesTouchToneClock,
                        UsesWebClock,
                        UsesTimesheets
                    ) VALUES (
                        @Id,
                        @CreatedAt,
                        @OrganizationId,
                        @EmailAddress,
                        @Name,
                        @TimeZone,
                        @IsDeleted,
                        @Pin,
                        @Role,
                        1,
                        1,
                        1,
                        1
                    );

                    SET IDENTITY_INSERT dbo.Users OFF;";
                connection.Execute(usersSql, users);

                var rates = new List<Rate>();
                var customers = new List<Customer>();
                var jobs = new List<Job>();
                var tasks = new List<Task>();

                // Add some payroll rates.
                rates.Add(new Rate() { Id = 1, CreatedAt = DateTime.UtcNow, OrganizationId = 1, Type = "payroll", Name = "Hourly Regular", IsDeleted = false });
                rates.Add(new Rate() { Id = 2, CreatedAt = DateTime.UtcNow, OrganizationId = 1, Type = "payroll", Name = "Hourly OT", IsDeleted = false, ParentRateId = 1 });
                rates.Add(new Rate() { Id = 3, CreatedAt = DateTime.UtcNow, OrganizationId = 1, Type = "payroll", Name = "Hourly DT", IsDeleted = false, ParentRateId = 1 });

                // Add some service rates.
                rates.Add(new Rate() { Id = 4, CreatedAt = DateTime.UtcNow, OrganizationId = 1, Type = "service", Name = "Manufacturing Regular", IsDeleted = false });
                rates.Add(new Rate() { Id = 5, CreatedAt = DateTime.UtcNow, OrganizationId = 1, Type = "service", Name = "Manufacturing OT", IsDeleted = false, ParentRateId = 4 });
                rates.Add(new Rate() { Id = 6, CreatedAt = DateTime.UtcNow, OrganizationId = 1, Type = "service", Name = "Manufacturing DT", IsDeleted = false, ParentRateId = 4 });

                // Insert the rates.
                var ratesSql = @"
                    SET IDENTITY_INSERT dbo.Rates ON;

                    INSERT INTO Rates (
                        Id,
                        CreatedAt,
                        OrganizationId,
                        [Type],
                        Name,
                        IsDeleted,
                        ParentRateId
                    ) VALUES (
                        @Id,
                        @CreatedAt,
                        @OrganizationId,
                        @Type,
                        @Name,
                        0,
                        @ParentRateId
                    );

                    SET IDENTITY_INSERT dbo.Rates OFF;";
                connection.Execute(ratesSql, rates);

                // Add some customers.
                customers.Add(new Customer() { Id = 1, CreatedAt = DateTime.UtcNow, Name = "General Mills", OrganizationId = 1, Number = "1000" });

                // Insert the customers.
                var customerSql = @"
                    SET IDENTITY_INSERT dbo.Customers ON;

                    INSERT INTO Customers (
                        Id,
                        CreatedAt,
                        Name,
                        OrganizationId,
                        Number
                    ) VALUES (
                        @Id,
                        @CreatedAt,
                        @Name,
                        @OrganizationId,
                        @Number
                    );

                    SET IDENTITY_INSERT dbo.Customers OFF;";
                connection.Execute(customerSql, customers);

                // Add some jobs.
                jobs.Add(new Job() { Id = 1, CreatedAt = DateTime.UtcNow, CustomerId = 1, Number = "1000", Name = "Manufacture Widgets", Status = "Open" });

                // Insert the jobs.
                var jobSql = @"
                    SET IDENTITY_INSERT dbo.Jobs ON;

                    INSERT INTO Jobs (
                        Id,
                        CreatedAt,
                        Name,
                        CustomerId,
                        Number,
                        Status
                    ) VALUES (
                        @Id,
                        @CreatedAt,
                        @Name,
                        @CustomerId,
                        @Number,
                        @Status
                    );

                    SET IDENTITY_INSERT dbo.Jobs OFF;";
                connection.Execute(jobSql, jobs);

                // Add some tasks.
                tasks.Add(new Task() { Id = 1, CreatedAt = DateTime.UtcNow, JobId = 1, Name = "Welding", Number = "1000", BasePayrollRateId = 1, BaseServiceRateId = 4, Order = 0 });
                tasks.Add(new Task() { Id = 2, CreatedAt = DateTime.UtcNow, JobId = 1, Name = "Cutting", Number = "1001", BasePayrollRateId = 1, BaseServiceRateId = 4, Order = 10 });
                tasks.Add(new Task() { Id = 3, CreatedAt = DateTime.UtcNow, JobId = 1, Name = "Assembly", Number = "1002", BasePayrollRateId = 1, BaseServiceRateId = 4, Order = 20 });

                // Insert the tasks.
                var taskSql = @"
                    SET IDENTITY_INSERT dbo.Tasks ON;

                    INSERT INTO Tasks (
                        [Id],
                        [CreatedAt],
                        [Name],
                        [JobId],
                        [Number],
                        [BasePayrollRateId],
                        [BaseServiceRateId],
                        [Order]
                    ) VALUES (
                        @Id,
                        @CreatedAt,
                        @Name,
                        @JobId,
                        @Number,
                        @BasePayrollRateId,
                        @BaseServiceRateId,
                        @Order
                    );

                    SET IDENTITY_INSERT dbo.Tasks OFF;";
                connection.Execute(taskSql, tasks);

                connection.Close();
            }
        }

        [TestCleanup]
        public void CleanupAfterTest()
        {
            db.Database.ExecuteSqlCommand(@"DELETE FROM [dbo].[Punches]");
            db.Database.ExecuteSqlCommand(@"DELETE FROM [dbo].[Tasks]");
            db.Database.ExecuteSqlCommand(@"DELETE FROM [dbo].[Jobs]");
            db.Database.ExecuteSqlCommand(@"DELETE FROM [dbo].[Customers]");
            db.Database.ExecuteSqlCommand(@"DELETE FROM [dbo].[Users]");
            db.Database.ExecuteSqlCommand(@"DELETE FROM [dbo].[Organizations]");
        }

        [TestMethod]
        public void PunchIn_Should_ReturnSuccessfully()
        {
            // Arrange.
            var controller = new KioskController();
            var controllerContext = new HttpControllerContext();

            // User will be authenticated.
            var userId = db.Users
                .Where(u => u.EmailAddress == "christopher.hitchens@western.com")
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

            // Act.
            var taskId = db.Tasks
                .Where(t => t.Number == "1000")
                .FirstOrDefault()
                .Id;
            var timeZone = "America/New_York";
            var latitude = "";
            var longitude = "";
            var sourceHardware = "";
            var sourceOperatingSystem = "";
            var sourceOperatingSystemVersion = "";
            var sourceBrowser = "";
            var sourceBrowserVersion = "";

            var actionResult = controller.PunchIn(
                taskId,
                timeZone,
                latitude,
                longitude,
                sourceHardware,
                sourceOperatingSystem,
                sourceOperatingSystemVersion,
                sourceBrowser,
                sourceBrowserVersion);

            var content = actionResult as OkNegotiatedContentResult<Punch>;

            // Assert.
            Assert.IsNotNull(content);
        }

        [TestMethod]
        public void PunchIn_ShouldNot_AllowPunchingInWithoutTaskId()
        {
            // Arrange.
            var controller = new KioskController();
            var controllerContext = new HttpControllerContext();

            // User will be authenticated.
            var userId = db.Users
                .Where(u => u.EmailAddress == "christopher.hitchens@western.com")
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

            // Act.
            var taskId = 0; // Invalid task id
            var timeZone = "America/New_York";
            var latitude = "";
            var longitude = "";
            var sourceHardware = "";
            var sourceOperatingSystem = "";
            var sourceOperatingSystemVersion = "";
            var sourceBrowser = "";
            var sourceBrowserVersion = "";

            var actionResult = controller.PunchIn(
                taskId,
                timeZone,
                latitude,
                longitude,
                sourceHardware,
                sourceOperatingSystem,
                sourceOperatingSystemVersion,
                sourceBrowser,
                sourceBrowserVersion);

            var content = actionResult as OkNegotiatedContentResult<Punch>;

            // Assert.
            Assert.IsNull(content);
        }

        [TestMethod]
        public void PunchOut_Should_ReturnSuccessfully()
        {
            // Arrange.
            var controller = new KioskController();
            var controllerContext = new HttpControllerContext();

            // User will be authenticated.
            var userId = db.Users
                .Where(u => u.EmailAddress == "christopher.hitchens@western.com")
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

            // Act.
            var taskId = db.Tasks
                .Where(t => t.Number == "1000")
                .FirstOrDefault()
                .Id;
            var timeZone = "America/New_York";
            var latitude = "";
            var longitude = "";
            var sourceHardware = "";
            var sourceOperatingSystem = "";
            var sourceOperatingSystemVersion = "";
            var sourceBrowser = "";
            var sourceBrowserVersion = "";

            var punchInActionResult = controller.PunchIn(
                taskId,
                timeZone,
                latitude,
                longitude,
                sourceHardware,
                sourceOperatingSystem,
                sourceOperatingSystemVersion,
                sourceBrowser,
                sourceBrowserVersion);

            var punchInContent = punchInActionResult as OkNegotiatedContentResult<Punch>;

            // Assert.
            Assert.IsNotNull(punchInContent);

            // Act again.
            var punchOutActionResult = controller.PunchOut(
                timeZone,
                latitude,
                longitude,
                sourceHardware,
                sourceOperatingSystem,
                sourceOperatingSystemVersion,
                sourceBrowser,
                sourceBrowserVersion);

            var punchOutContent = punchOutActionResult as OkNegotiatedContentResult<Punch>;

            // Assert again.
            Assert.IsNotNull(punchOutContent);
        }

        [TestMethod]
        public void PunchOut_ShouldNot_AllowPunchingOutWithoutBeingPunchedIn()
        {
            // Arrange.
            var controller = new KioskController();
            var controllerContext = new HttpControllerContext();

            // User will be authenticated.
            var userId = db.Users
                .Where(u => u.EmailAddress == "christopher.hitchens@western.com")
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

            // Act.
            var taskId = db.Tasks
                .Where(t => t.Number == "1000")
                .FirstOrDefault()
                .Id;
            var timeZone = "America/New_York";
            var latitude = "";
            var longitude = "";
            var sourceHardware = "";
            var sourceOperatingSystem = "";
            var sourceOperatingSystemVersion = "";
            var sourceBrowser = "";
            var sourceBrowserVersion = "";

            // Act.
            var actionResult = controller.PunchOut(
                timeZone,
                latitude,
                longitude,
                sourceHardware,
                sourceOperatingSystem,
                sourceOperatingSystemVersion,
                sourceBrowser,
                sourceBrowserVersion);

            var content = actionResult as OkNegotiatedContentResult<Punch>;

            // Assert.
            Assert.IsNull(content);
        }
    }
}
