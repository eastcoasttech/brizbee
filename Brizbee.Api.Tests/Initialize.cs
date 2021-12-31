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
        public static IConfiguration _configuration { get; set; }

        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            Trace.TraceInformation("Triggering assembly initialize");

            if (context == null) { throw new ArgumentNullException(nameof(context)); }

            // Setup logging
            Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));

            // Setup configuration
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonFile("appsettings.json");
            _configuration = configurationBuilder.Build();

            // Scaffold database
            Trace.TraceInformation("Creating objects in the database");

            var connectionString = _configuration.GetConnectionString("SqlContext");

            Brizbee.Database.SqlServer.Program.Main(new string[] { connectionString });

            Trace.TraceInformation("Objects have been created in the database");
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            Trace.TraceInformation("Triggering assembly cleanup");

            // Setup configuration
            //var configurationBuilder = new ConfigurationBuilder();
            //configurationBuilder.AddJsonFile("appsettings.json");
            //_configuration = configurationBuilder.Build();

            var dropSql = "";

            var assembly = Assembly.GetExecutingAssembly();

            string resourceName = assembly.GetManifestResourceNames()
                .Single(str => str.EndsWith("WARNING DROP OBJECTS.sql"));

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                dropSql = reader.ReadToEnd();
            }

            if (string.IsNullOrEmpty(dropSql))
            {
                throw new Exception("SQL to drop objects could not be read");
            }

            try
            {
                Trace.TraceInformation("Printing connection string on assembly cleanup");

                var connectionString = _configuration.GetConnectionString("SqlContext");
                Trace.TraceInformation(connectionString);
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    var sqlToExecute = string.Format(dropSql, connection.Database);

                    var command = new SqlCommand(sqlToExecute, connection);

                    Trace.TraceInformation("Dropping objects from the database");

                    command.ExecuteNonQuery();

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
