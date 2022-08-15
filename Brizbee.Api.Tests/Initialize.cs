using Brizbee.Core.Models.Accounting;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Brizbee.Api.Tests
{
    [TestClass]
    public class Initialize
    {
        private static IConfiguration Configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").AddEnvironmentVariables().Build();
        private static string DatabaseConnectionString = Configuration.GetConnectionString("SqlContext");
        
        static Initialize()
        {
            if (string.IsNullOrEmpty(DatabaseConnectionString))
                throw new InvalidOperationException();
        }
        
        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            Trace.TraceInformation("Triggering assembly initialize");

            Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));

            Trace.TraceInformation("Deleting objects in case database was partially scaffolded");

            AssemblyCleanup();

            Trace.TraceInformation("Creating objects in the database");

            Database.SqlServer.Program.Main(new string[] { DatabaseConnectionString });

            Trace.TraceInformation("Objects have been created in the database");
        }
        
        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            Trace.TraceInformation("Triggering assembly cleanup");

            var dropSql = "";
            
            var assembly = Assembly.GetAssembly(typeof(Account));

            var resourceName = assembly!.GetManifestResourceNames()
                .Single(str => str.EndsWith("WARNING DROP OBJECTS.sql"));

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream!))
            {
                dropSql = reader.ReadToEnd();
            }

            if (string.IsNullOrEmpty(dropSql))
                throw new Exception("SQL to drop objects could not be read");

            try
            {
                using (var connection = new SqlConnection(DatabaseConnectionString))
                {
                    connection.Open();

                    Trace.TraceInformation("Dropping objects from the database");

                    connection.Execute(dropSql);

                    Trace.TraceInformation("Objects have been dropped from the database");
                }
            }
            catch (SqlException sqlException)
            {
                Trace.TraceWarning(sqlException.Message);
                throw;
            }
        }
    }
}
