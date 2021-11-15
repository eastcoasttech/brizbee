using Brizbee.Api.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web.Http;

namespace Brizbee.Api.Tests
{
    [TestClass]
    public class UsersControllerTest
    {
        public IConfiguration Configuration { get; set; }
        public SqlContext _context { get; set; }

        public UsersControllerTest()
        {
            // Setup configuration
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonFile("appsettings.json");
            Configuration = configurationBuilder.Build();

            // Setup database context
            var options = new DbContextOptionsBuilder<SqlContext>()
                .UseSqlServer(Configuration["ConnectionStrings:SqlContext"])
                .Options;
            _context = new SqlContext(options);
        }

        [TestInitialize]
        public void PrepareForTest()
        {
            //new DatabaseServices().Scaffold();
        }

        [TestCleanup]
        public void CleanupAfterTest()
        {
            _context.Database.ExecuteSqlRaw(@"DELETE FROM [dbo].[Commits]");
            _context.Database.ExecuteSqlRaw(@"DELETE FROM [dbo].[Punches]");
            _context.Database.ExecuteSqlRaw(@"DELETE FROM [dbo].[Tasks]");
            _context.Database.ExecuteSqlRaw(@"DELETE FROM [dbo].[Jobs]");
            _context.Database.ExecuteSqlRaw(@"DELETE FROM [dbo].[Customers]");
            _context.Database.ExecuteSqlRaw(@"DELETE FROM [dbo].[Users]");
            _context.Database.ExecuteSqlRaw(@"DELETE FROM [dbo].[Organizations]");
        }
    }
}
