using Brizbee.Common.Models;
using Brizbee.Web.Repositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Brizbee.Web.Tests.Repositories
{
    [TestClass]
    public class UserRepositoryTest
    {
        private readonly SqlContext db = new SqlContext();

        [TestInitialize]
        public void PrepareForTest()
        {
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
        public void Register_Successful()
        {
            // Arrange
            var repo = new UserRepository();
            var user = new User()
            {
                Name = "Joshua Shane Martin",
                EmailAddress = "joshuasmartin@brizbee.com",
                Password = "ThisIsMyPassword!",
                Pin = "1111",
                TimeZone = "America/New_York"
            };
            var organization = new Organization()
            {
                Name = "East Coast Technology Services, LLC",
                Code = "1234",
                PlanId = 1
            };

            // Act
            var created = repo.Register(user, organization);

            // Assert
            Assert.IsNotNull(created);
        }
    }
}
