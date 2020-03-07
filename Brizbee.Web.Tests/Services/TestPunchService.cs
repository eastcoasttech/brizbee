using System;
using System.Collections.Generic;
using System.Linq;
using Brizbee.Common.Models;
using Brizbee.Web.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Brizbee.Web.Tests.Services
{
    [TestClass]
    public class TestPunchService
    {
        [TestMethod]
        public void SplitAtMidnight()
        {
            var service = new PunchService(new TestSqlContext());
            var inAt = new DateTime(2020, 1, 1);
            var outAt = new DateTime(2020, 1, 31);

            var currentUser = GetCurrentUser();

            var punches = GetPunches()
                .OrderBy(p => p.UserId)
                .ThenBy(p => p.InAt)
                .ToList();

            var splitMidnight = service.SplitAtMidnight(punches, currentUser);
            var splitSevenAm = service.SplitAtMinute(splitMidnight, currentUser, minuteOfDay: 420); // Split at 7am = 420 minutes
            var splitFivePm = service.SplitAtMinute(splitSevenAm, currentUser, minuteOfDay: 1020); // Split at 5pm = 1020 minutes
        }

        User GetCurrentUser()
        {
            return GetUsers().Find(u => u.Id == 1);
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
                    InAt = new DateTime(2020, 1, 6, 8, 15, 0), // Monday
                    OutAt = new DateTime(2020, 1, 6, 17, 33, 0)
                },
                new Punch()
                {
                    Id = 2,
                    CreatedAt = DateTime.UtcNow,
                    UserId = 1,
                    InAt = new DateTime(2020, 1, 7, 8, 5, 0), // Tuesday
                    OutAt = new DateTime(2020, 1, 7, 17, 19, 0)
                },
                new Punch()
                {
                    Id = 3,
                    CreatedAt = DateTime.UtcNow,
                    UserId = 1,
                    InAt = new DateTime(2020, 1, 8, 8, 11, 0), // Wednesday
                    OutAt = new DateTime(2020, 1, 8, 17, 12, 0)
                },
                new Punch()
                {
                    Id = 4,
                    CreatedAt = DateTime.UtcNow,
                    UserId = 1,
                    InAt = new DateTime(2020, 1, 9, 7, 55, 0), // Thursday
                    OutAt = new DateTime(2020, 1, 9, 17, 7, 0)
                },
                new Punch()
                {
                    Id = 5,
                    CreatedAt = DateTime.UtcNow,
                    UserId = 1,
                    InAt = new DateTime(2020, 1, 10, 8, 1, 0), // Friday
                    OutAt = new DateTime(2020, 1, 10, 17, 2, 0)
                },
                
                // Punches for George Will
                new Punch()
                {
                    Id = 6,
                    CreatedAt = DateTime.UtcNow,
                    Guid = Guid.NewGuid(),
                    UserId = 2,
                    InAt = new DateTime(2020, 1, 6, 8, 4, 0), // Monday
                    OutAt = new DateTime(2020, 1, 6, 16, 59, 0)
                },
                new Punch()
                {
                    Id = 7,
                    CreatedAt = DateTime.UtcNow,
                    UserId = 2,
                    InAt = new DateTime(2020, 1, 7, 8, 2, 0), // Tuesday
                    OutAt = new DateTime(2020, 1, 7, 17, 0, 0)
                },
                new Punch()
                {
                    Id = 8,
                    CreatedAt = DateTime.UtcNow,
                    UserId = 2,
                    InAt = new DateTime(2020, 1, 8, 8, 30, 0), // Wednesday
                    OutAt = new DateTime(2020, 1, 8, 17, 45, 0)
                },
                new Punch()
                {
                    Id = 9,
                    CreatedAt = DateTime.UtcNow,
                    UserId = 2,
                    InAt = new DateTime(2020, 1, 9, 8, 5, 0), // Thursday
                    OutAt = new DateTime(2020, 1, 9, 17, 20, 0)
                },
                new Punch()
                {
                    Id = 10,
                    CreatedAt = DateTime.UtcNow,
                    UserId = 2,
                    InAt = new DateTime(2020, 1, 10, 8, 15, 0), // Friday
                    OutAt = new DateTime(2020, 1, 10, 17, 25, 0)
                },
                new Punch()
                {
                    Id = 11,
                    CreatedAt = DateTime.UtcNow,
                    UserId = 2,
                    InAt = new DateTime(2020, 1, 11, 2, 32, 0), // Saturday
                    OutAt = new DateTime(2020, 1, 11, 18, 19, 0)
                },
                
                // Punches for George Will
                new Punch()
                {
                    Id = 12,
                    CreatedAt = DateTime.UtcNow,
                    Guid = Guid.NewGuid(),
                    UserId = 2,
                    InAt = new DateTime(2020, 1, 6, 14, 30, 0), // Monday Afternoon
                    OutAt = new DateTime(2020, 1, 7, 0, 30, 0) // Tuesday AM
                },
                new Punch()
                {
                    Id = 13,
                    CreatedAt = DateTime.UtcNow,
                    UserId = 2,
                    InAt = new DateTime(2020, 1, 8, 14, 45, 0), // Wednesday Afternoon
                    OutAt = new DateTime(2020, 1, 9, 1, 5, 0) // Thursday AM
                },
                new Punch()
                {
                    Id = 14,
                    CreatedAt = DateTime.UtcNow,
                    UserId = 2,
                    InAt = new DateTime(2020, 1, 9, 18, 3, 0), // Thursday
                    OutAt = new DateTime(2020, 1, 9, 22, 39, 0)
                },
                new Punch()
                {
                    Id = 15,
                    CreatedAt = DateTime.UtcNow,
                    UserId = 2,
                    InAt = new DateTime(2020, 1, 10, 9, 8, 0), // Friday
                    OutAt = new DateTime(2020, 1, 10, 16, 19, 0)
                },
                new Punch()
                {
                    Id = 16,
                    CreatedAt = DateTime.UtcNow,
                    UserId = 2,
                    InAt = new DateTime(2020, 1, 11, 14, 1, 0), // Saturday
                    OutAt = new DateTime(2020, 1, 11, 18, 54, 0)
                }
            };
        }

        List<User> GetUsers()
        {
            return new List<User>()
            {
                new User()
                {
                    Id = 1,
                    Name = "Christopher Hitchens",
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false,
                    OrganizationId = 1,
                    EmailAddress = "christopher.hitchens@western.com",
                    TimeZone = "America/New_York"
                },
                new User()
                {
                    Id = 2,
                    Name = "George Will",
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false,
                    OrganizationId = 1,
                    EmailAddress = "george.will@western.com",
                    TimeZone = "America/New_York"
                },
                new User()
                {
                    Id = 3,
                    Name = "Ada Lovelace",
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false,
                    OrganizationId = 1,
                    EmailAddress = "ada.lovelace@western.com",
                    TimeZone = "America/New_York"
                },
                new User()
                {
                    Id = 4,
                    Name = "Jack Welch",
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false,
                    OrganizationId = 1,
                    EmailAddress = "jack.welch@western.com",
                    TimeZone = "America/New_York"
                }
            };
        }

        List<Organization> GetOrganizations()
        {
            return new List<Organization>()
            {
                new Organization()
                {
                    Id = 1,
                    Name = "Western Fabrication Company",
                    Code = "1234",
                    CreatedAt = DateTime.UtcNow,
                    MinutesFormat = "minutes"
                }
            };
        }
    }
}
