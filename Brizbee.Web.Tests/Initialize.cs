using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Brizbee.Web.Tests
{
    [TestClass]
    public class Initialize
    {
        private static readonly string connectionString = ConfigurationManager.ConnectionStrings["SqlContext"].ToString();

        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            if (context == null) { throw new ArgumentNullException(nameof(context)); }

            Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));

            Trace.TraceInformation("Creating objects in the database");

            Brizbee.Database.SqlServer.Program.Main(new string[] { connectionString });

            Trace.TraceInformation("Objects have been created in the database");
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            var dropSql = "";

            var assembly = Assembly.GetAssembly(typeof(Brizbee.Common.Models.Commit));

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
