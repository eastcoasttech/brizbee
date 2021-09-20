using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using Brizbee.Common.Models;
using Brizbee.Web.Serialization;
using Brizbee.Web.Services;
using Dapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Brizbee.Web.Tests.Services
{
    [TestClass]
    public class PunchServiceTest
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

                // Insert the organizations
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

                // Insert the users
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

                // Add some payroll rates
                rates.Add(new Rate() { Id = 1, CreatedAt = DateTime.UtcNow, OrganizationId = 1, Type = "payroll", Name = "Hourly Regular", IsDeleted = false });
                rates.Add(new Rate() { Id = 2, CreatedAt = DateTime.UtcNow, OrganizationId = 1, Type = "payroll", Name = "Hourly OT", IsDeleted = false, ParentRateId = 1 });
                rates.Add(new Rate() { Id = 3, CreatedAt = DateTime.UtcNow, OrganizationId = 1, Type = "payroll", Name = "Hourly DT", IsDeleted = false, ParentRateId = 1 });

                // Add some service rates
                rates.Add(new Rate() { Id = 4, CreatedAt = DateTime.UtcNow, OrganizationId = 1, Type = "service", Name = "Manufacturing Regular", IsDeleted = false });
                rates.Add(new Rate() { Id = 5, CreatedAt = DateTime.UtcNow, OrganizationId = 1, Type = "service", Name = "Manufacturing OT", IsDeleted = false, ParentRateId = 4 });
                rates.Add(new Rate() { Id = 6, CreatedAt = DateTime.UtcNow, OrganizationId = 1, Type = "service", Name = "Manufacturing DT", IsDeleted = false, ParentRateId = 4 });

                // Insert the rates
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

                // Add some customers
                customers.Add(new Customer() { Id = 1, CreatedAt = DateTime.UtcNow, Name = "General Mills", OrganizationId = 1, Number = "1000" });

                // Insert the customers
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

                // Add some jobs
                jobs.Add(new Job() { Id = 1, CreatedAt = DateTime.UtcNow, CustomerId = 1, Number = "1000", Name = "Manufacture Widgets" });

                // Insert the jobs
                var jobSql = @"
                    SET IDENTITY_INSERT dbo.Jobs ON;

                    INSERT INTO Jobs (
                        Id,
                        CreatedAt,
                        Name,
                        CustomerId,
                        Number
                    ) VALUES (
                        @Id,
                        @CreatedAt,
                        @Name,
                        @CustomerId,
                        @Number
                    );

                    SET IDENTITY_INSERT dbo.Jobs OFF;";
                connection.Execute(jobSql, jobs);

                // Add some tasks
                tasks.Add(new Task() { Id = 1, CreatedAt = DateTime.UtcNow, JobId = 1, Name = "Welding", Number = "1000", BasePayrollRateId = 1, BaseServiceRateId = 4, Order = 0 });
                tasks.Add(new Task() { Id = 2, CreatedAt = DateTime.UtcNow, JobId = 1, Name = "Cutting", Number = "1001", BasePayrollRateId = 1, BaseServiceRateId = 4, Order = 10 });
                tasks.Add(new Task() { Id = 3, CreatedAt = DateTime.UtcNow, JobId = 1, Name = "Assembly", Number = "1002", BasePayrollRateId = 1, BaseServiceRateId = 4, Order = 20 });

                // Insert the tasks
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
            db.Database.ExecuteSqlCommand(@"DELETE FROM [dbo].[Tasks]");
            db.Database.ExecuteSqlCommand(@"DELETE FROM [dbo].[Jobs]");
            db.Database.ExecuteSqlCommand(@"DELETE FROM [dbo].[Customers]");
            db.Database.ExecuteSqlCommand(@"DELETE FROM [dbo].[Users]");
            db.Database.ExecuteSqlCommand(@"DELETE FROM [dbo].[Organizations]");
        }

        [TestMethod]
        public void Split_Midnight_Successful()
        {
            var db = new SqlContext();
            var currentUser = db.Users.Find(1);

            var service = new PunchService();

            var originalPunches = GetPunches()
                .OrderBy(p => p.UserId)
                .ThenBy(p => p.InAt)
                .ToList();

            // Assert original count
            Assert.AreEqual(19, originalPunches.Count);

            // Split at midnight
            var splitMidnight = service.SplitAtMidnight(originalPunches, currentUser);

            // Assert that there are more punches now
            Assert.AreEqual(21, splitMidnight.Count);
        }

        [TestMethod]
        public void Split_SevenAmAndFivePm_Successful()
        {
            var db = new SqlContext();
            var currentUser = db.Users.Find(1);

            var service = new PunchService();
            var inAt = new DateTime(2020, 1, 1);
            var outAt = new DateTime(2020, 1, 31);

            var originalPunches = GetPunches()
                .OrderBy(p => p.UserId)
                .ThenBy(p => p.InAt)
                .ToList();

            // Assert original count
            Assert.AreEqual(19, originalPunches.Count);

            // Split at midnight, must always do this first
            var splitMidnight = service.SplitAtMidnight(originalPunches, currentUser);

            // Assert that there are more punches now
            Assert.AreEqual(21, splitMidnight.Count);

            // Split again at 7am and assert that there are yet more punches
            var splitSevenAm = service.SplitAtMinute(splitMidnight, currentUser, minuteOfDay: 420); // Split at 7am = 420 minutes

            // Assert that there are more punches now
            Assert.AreEqual(22, splitSevenAm.Count);

            // Finally split at 5pm and assert that there are even more punches
            var splitFivePm = service.SplitAtMinute(splitSevenAm, currentUser, minuteOfDay: 1020); // Split at 5pm = 1020 minutes

            // Assert that there are more punches now
            Assert.AreEqual(34, splitFivePm.Count);
        }

        [TestMethod]
        public void Populate_PayrollBeforeSevenAmAfterFivePmAsOvertime_Successful()
        {
            var db = new SqlContext();
            var currentUser = db.Users.Find(1);

            var service = new PunchService();
            var inAt = new DateTime(2020, 1, 1);
            var outAt = new DateTime(2020, 1, 31);

            var originalPunches = GetPunches()
                .OrderBy(p => p.UserId)
                .ThenBy(p => p.InAt)
                .ToList();

            // Splitting happens automatically during populate below

            var populateOptions = new PopulateRateOptions();
            populateOptions.InAt = new DateTime(2020, 1, 1);
            populateOptions.OutAt = new DateTime(2020, 1, 31);
            populateOptions.Options = new PopulateRateOption[]
            {
                // This option should populate anything
                // from midnight through 7am with the overtime rate
                new PopulateRateOption()
                {
                    Type = "range",
                    RangeDirection = "before",
                    RangeMinutes = 420,
                    BasePayrollRateId = 1,
                    AlternatePayrollRateId = 2,
                    Order = 0
                },

                // This option should populate anything
                // from 5pm through midnight with overtime rate
                new PopulateRateOption()
                {
                    Type = "range",
                    RangeDirection = "after",
                    RangeMinutes = 1020,
                    BasePayrollRateId = 1,
                    AlternatePayrollRateId = 2,
                    Order = 1
                }
            };

            var populatedPunches = service.Populate(populateOptions, originalPunches, currentUser);

            // Assert the number of punches after automatic split
            Assert.AreEqual(34, populatedPunches.Count);

            foreach (var punch in populatedPunches.Where(p => p.UserId == 3))
            {
                var midnight = new DateTime(punch.InAt.Year, punch.InAt.Month, punch.InAt.Day, 0, 0, 0, 0);
                var spanInAt = punch.InAt.Subtract(midnight);
                var minuteOfInAt = spanInAt.TotalMinutes;
                
                switch (punch.PayrollRateId)
                {
                    case 1:
                        // Should be regular
                        Assert.IsTrue(minuteOfInAt >= 420 || minuteOfInAt <= 1020);
                        break;
                    case 2:
                        // Should be overtime
                        Assert.IsTrue(minuteOfInAt <= 420 || minuteOfInAt >= 1020);
                        break;
                }
            }
        }

        [TestMethod]
        public void Populate_PayrollFortyHoursTotalOvertime_Successful()
        {
            var db = new SqlContext();
            var currentUser = db.Users.Find(1);

            var service = new PunchService();
            var inAt = new DateTime(2020, 1, 1);
            var outAt = new DateTime(2020, 1, 31);

            var originalPunches = GetPunches()
                .OrderBy(p => p.UserId)
                .ThenBy(p => p.InAt)
                .ToList();

            // Splitting happens automatically during populate below

            var populateOptions = new PopulateRateOptions();
            populateOptions.InAt = new DateTime(2020, 1, 1);
            populateOptions.OutAt = new DateTime(2020, 1, 31);
            populateOptions.Options = new PopulateRateOption[]
            {
                // This option should populate anything
                // after 40 hours with the overtime rate
                new PopulateRateOption()
                {
                    Type = "count",
                    CountScope = "total",
                    CountMinute = 2400,
                    BasePayrollRateId = 1,
                    AlternatePayrollRateId = 2,
                    Order = 0
                }
            };

            var populatedPunches = service.Populate(populateOptions, originalPunches, currentUser);

            // Assert the number of punches after automatic split
            Assert.AreEqual(23, populatedPunches.Count);

            var userIds = populatedPunches.GroupBy(p => p.UserId).Select(g => g.Key);
            foreach (var userId in userIds)
            {
                // Assert that punches over 40 hours (2400 minutes) are overtime
                var punches = populatedPunches.Where(p => p.UserId == userId);
                var count = 0;
                foreach (var punch in punches)
                {
                    count += punch.Minutes;
                    if (count <= 2400)
                    {
                        Assert.IsTrue(punch.PayrollRateId == 1);
                    }
                    else
                    {
                        Assert.IsTrue(punch.PayrollRateId == 2);
                    }
                }
            }
        }

        [TestMethod]
        public void Populate_PayrollFourHoursPerDayOvertime_Successful()
        {
            var db = new SqlContext();
            var currentUser = db.Users.Find(1);

            var service = new PunchService();
            var inAt = new DateTime(2020, 1, 1);
            var outAt = new DateTime(2020, 1, 31);

            var originalPunches = GetPunches()
                .OrderBy(p => p.UserId)
                .ThenBy(p => p.InAt)
                .ToList();

            // Splitting happens automatically during populate below

            var populateOptions = new PopulateRateOptions();
            populateOptions.InAt = new DateTime(2020, 1, 1);
            populateOptions.OutAt = new DateTime(2020, 1, 31);
            populateOptions.Options = new PopulateRateOption[]
            {
                // This option should populate anything
                // after 4 hours per day with the overtime rate
                new PopulateRateOption()
                {
                    Type = "count",
                    CountScope = "day",
                    CountMinute = 240,
                    BasePayrollRateId = 1,
                    AlternatePayrollRateId = 2,
                    Order = 0
                }
            };

            var populatedPunches = service.Populate(populateOptions, originalPunches, currentUser);

            // Assert the number of punches after automatic split
            Assert.AreEqual(38, populatedPunches.Count);

            var userIds = populatedPunches.GroupBy(p => p.UserId).Select(g => g.Key);
            foreach (var userId in userIds)
            {
                // Assert that punches over 4 hours per day (240 minutes) are overtime
                var filtered = populatedPunches.Where(p => p.UserId == userId);
                var dates = filtered
                    .GroupBy(p => p.InAt.Date)
                    .Select(g => new {
                        Date = g.Key
                    })
                    .ToList();

                foreach (var date in dates)
                {
                    var punchesForDay = filtered
                        .Where(p => p.InAt.Date == date.Date)
                        .ToList();
                    var count = 0;
                    foreach (var punch in punchesForDay)
                    {
                        count += punch.Minutes;
                        if (count <= 240)
                        {
                            Assert.IsTrue(punch.PayrollRateId == 1);
                        }
                        else
                        {
                            Assert.IsTrue(punch.PayrollRateId == 2);
                        }
                    }
                }
            }
        }

        [TestMethod]
        public void Populate_PayrollSpecificDayOvertime_Successful()
        {
            var db = new SqlContext();
            var currentUser = db.Users.Find(1);

            var service = new PunchService();
            var inAt = new DateTime(2020, 1, 1);
            var outAt = new DateTime(2020, 1, 31);

            var originalPunches = GetPunches()
                .OrderBy(p => p.UserId)
                .ThenBy(p => p.InAt)
                .ToList();

            // Splitting happens automatically during populate below

            var populateOptions = new PopulateRateOptions();
            populateOptions.InAt = new DateTime(2020, 1, 1);
            populateOptions.OutAt = new DateTime(2020, 1, 31);
            populateOptions.Options = new PopulateRateOption[]
            {
                // This option should populate anything
                // on January 6, 2020 with the overtime rate
                new PopulateRateOption()
                {
                    Type = "date",
                    Date = new DateTime(2020, 1, 6),
                    BasePayrollRateId = 1,
                    AlternatePayrollRateId = 2,
                    Order = 0
                }
            };

            var populatedPunches = service.Populate(populateOptions, originalPunches, currentUser);

            // Assert the number of punches after automatic split
            Assert.AreEqual(21, populatedPunches.Count);

            foreach (var punch in populatedPunches)
            {
                if (punch.InAt.Date == new DateTime(2020, 1, 6))
                {
                    Assert.IsTrue(punch.PayrollRateId == 2);
                }
                else
                {
                    Assert.IsTrue(punch.PayrollRateId == 1);
                }
            }
        }

        [TestMethod]
        public void Populate_PayrollDayOfWeekOvertime_Successful()
        {
            var db = new SqlContext();
            var currentUser = db.Users.Find(1);

            var service = new PunchService();
            var inAt = new DateTime(2020, 1, 1);
            var outAt = new DateTime(2020, 1, 31);

            var originalPunches = GetPunches()
                .OrderBy(p => p.UserId)
                .ThenBy(p => p.InAt)
                .ToList();

            // Splitting happens automatically during populate below

            var populateOptions = new PopulateRateOptions();
            populateOptions.InAt = new DateTime(2020, 1, 1);
            populateOptions.OutAt = new DateTime(2020, 1, 31);
            populateOptions.Options = new PopulateRateOption[]
            {
                // This option should populate anything
                // on Saturday with the overtime rate
                new PopulateRateOption()
                {
                    Type = "dayofweek",
                    DayOfWeek = "Saturday",
                    BasePayrollRateId = 1,
                    AlternatePayrollRateId = 2,
                    Order = 0
                }
            };

            var populatedPunches = service.Populate(populateOptions, originalPunches, currentUser);

            // Assert the number of punches after automatic split
            Assert.AreEqual(21, populatedPunches.Count);

            foreach (var punch in populatedPunches)
            {
                if (punch.InAt.DayOfWeek == DayOfWeek.Saturday)
                {
                    Assert.IsTrue(punch.PayrollRateId == 2);
                }
                else
                {
                    Assert.IsTrue(punch.PayrollRateId == 1);
                }
            }
        }

        List<Punch> GetPunches()
        {
            return new List<Punch>()
            {
                // Punches for Christopher Hitchens
                new Punch()
                {
                    Id = 1,
                    CreatedAt = DateTime.UtcNow,
                    Guid = Guid.NewGuid(),
                    UserId = 1,
                    InAt = new DateTime(2020, 1, 6, 8, 15, 0, 0), // Monday
                    OutAt = new DateTime(2020, 1, 6, 17, 33, 0, 0),
                    TaskId = 1
                },
                new Punch()
                {
                    Id = 2,
                    CreatedAt = DateTime.UtcNow,
                    Guid = Guid.NewGuid(),
                    UserId = 1,
                    InAt = new DateTime(2020, 1, 7, 8, 5, 0, 0), // Tuesday
                    OutAt = new DateTime(2020, 1, 7, 17, 19, 0, 0),
                    TaskId = 1
                },
                new Punch()
                {
                    Id = 3,
                    CreatedAt = DateTime.UtcNow,
                    Guid = Guid.NewGuid(),
                    UserId = 1,
                    InAt = new DateTime(2020, 1, 8, 8, 11, 0, 0), // Wednesday
                    OutAt = new DateTime(2020, 1, 8, 17, 12, 0, 0),
                    TaskId = 1
                },
                new Punch()
                {
                    Id = 4,
                    CreatedAt = DateTime.UtcNow,
                    Guid = Guid.NewGuid(),
                    UserId = 1,
                    InAt = new DateTime(2020, 1, 9, 7, 55, 0, 0), // Thursday
                    OutAt = new DateTime(2020, 1, 9, 17, 7, 0, 0),
                    TaskId = 1
                },
                new Punch()
                {
                    Id = 5,
                    CreatedAt = DateTime.UtcNow,
                    Guid = Guid.NewGuid(),
                    UserId = 1,
                    InAt = new DateTime(2020, 1, 10, 8, 1, 0, 0), // Friday
                    OutAt = new DateTime(2020, 1, 10, 17, 2, 0, 0),
                    TaskId = 1
                },
                
                // Punches for George Will
                new Punch()
                {
                    Id = 6,
                    CreatedAt = DateTime.UtcNow,
                    Guid = Guid.NewGuid(),
                    UserId = 2,
                    InAt = new DateTime(2020, 1, 6, 8, 4, 0, 0), // Monday
                    OutAt = new DateTime(2020, 1, 6, 16, 59, 0, 0),
                    TaskId = 1
                },
                new Punch()
                {
                    Id = 7,
                    CreatedAt = DateTime.UtcNow,
                    Guid = Guid.NewGuid(),
                    UserId = 2,
                    InAt = new DateTime(2020, 1, 7, 8, 2, 0, 0), // Tuesday
                    OutAt = new DateTime(2020, 1, 7, 17, 0, 0, 0),
                    TaskId = 1
                },
                new Punch()
                {
                    Id = 8,
                    CreatedAt = DateTime.UtcNow,
                    Guid = Guid.NewGuid(),
                    UserId = 2,
                    InAt = new DateTime(2020, 1, 8, 8, 30, 0, 0), // Wednesday
                    OutAt = new DateTime(2020, 1, 8, 17, 45, 0, 0),
                    TaskId = 1
                },
                new Punch()
                {
                    Id = 9,
                    CreatedAt = DateTime.UtcNow,
                    Guid = Guid.NewGuid(),
                    UserId = 2,
                    InAt = new DateTime(2020, 1, 9, 8, 5, 0, 0), // Thursday
                    OutAt = new DateTime(2020, 1, 9, 17, 20, 0, 0),
                    TaskId = 1
                },
                new Punch()
                {
                    Id = 10,
                    CreatedAt = DateTime.UtcNow,
                    Guid = Guid.NewGuid(),
                    UserId = 2,
                    InAt = new DateTime(2020, 1, 10, 8, 15, 0, 0), // Friday
                    OutAt = new DateTime(2020, 1, 10, 17, 25, 0, 0),
                    TaskId = 1
                },
                new Punch()
                {
                    Id = 11,
                    CreatedAt = DateTime.UtcNow,
                    Guid = Guid.NewGuid(),
                    UserId = 2,
                    InAt = new DateTime(2020, 1, 11, 2, 32, 0, 0), // Saturday
                    OutAt = new DateTime(2020, 1, 11, 18, 19, 0, 0),
                    TaskId = 1
                },
                
                // Punches for Ada Lovelace
                new Punch()
                {
                    Id = 12,
                    CreatedAt = DateTime.UtcNow,
                    Guid = Guid.NewGuid(),
                    UserId = 3,
                    InAt = new DateTime(2020, 1, 6, 14, 30, 0, 0), // Monday Afternoon
                    OutAt = new DateTime(2020, 1, 7, 0, 30, 0, 0), // Tuesday AM
                    TaskId = 1
                },
                new Punch()
                {
                    Id = 13,
                    CreatedAt = DateTime.UtcNow,
                    Guid = Guid.NewGuid(),
                    UserId = 3,
                    InAt = new DateTime(2020, 1, 8, 14, 45, 0, 0), // Wednesday Afternoon
                    OutAt = new DateTime(2020, 1, 9, 1, 5, 0, 0), // Thursday AM
                    TaskId = 1
                },
                new Punch()
                {
                    Id = 14,
                    CreatedAt = DateTime.UtcNow,
                    Guid = Guid.NewGuid(),
                    UserId = 3,
                    InAt = new DateTime(2020, 1, 9, 18, 3, 0, 0), // Thursday
                    OutAt = new DateTime(2020, 1, 9, 22, 39, 0, 0),
                    TaskId = 1
                },
                new Punch()
                {
                    Id = 15,
                    CreatedAt = DateTime.UtcNow,
                    Guid = Guid.NewGuid(),
                    UserId = 3,
                    InAt = new DateTime(2020, 1, 10, 9, 8, 0, 0), // Friday
                    OutAt = new DateTime(2020, 1, 10, 16, 19, 0, 0),
                    TaskId = 1
                },
                new Punch()
                {
                    Id = 16,
                    CreatedAt = DateTime.UtcNow,
                    Guid = Guid.NewGuid(),
                    UserId = 3,
                    InAt = new DateTime(2020, 1, 11, 14, 1, 0, 0), // Saturday
                    OutAt = new DateTime(2020, 1, 11, 18, 54, 0, 0),
                    TaskId = 1
                },

                // Punches for Jack Welch
                new Punch()
                {
                    Id = 17,
                    CreatedAt = DateTime.UtcNow,
                    Guid = Guid.NewGuid(),
                    UserId = 4,
                    InAt = new DateTime(2020, 1, 6, 8, 19, 0, 0), // Monday
                    OutAt = new DateTime(2020, 1, 6, 8, 19, 0, 0),
                    TaskId = 1
                },
                new Punch()
                {
                    Id = 18,
                    CreatedAt = DateTime.UtcNow,
                    Guid = Guid.NewGuid(),
                    UserId = 4,
                    InAt = new DateTime(2020, 1, 6, 8, 19, 0, 0), // Monday
                    OutAt = new DateTime(2020, 1, 6, 17, 0, 0, 0),
                    TaskId = 1
                },
                new Punch()
                {
                    Id = 19,
                    CreatedAt = DateTime.UtcNow,
                    Guid = Guid.NewGuid(),
                    UserId = 4,
                    InAt = new DateTime(2020, 1, 6, 17, 0, 0, 0), // Monday
                    OutAt = new DateTime(2020, 1, 6, 17, 0, 0, 0),
                    TaskId = 1
                },
            };
        }
    }
}
