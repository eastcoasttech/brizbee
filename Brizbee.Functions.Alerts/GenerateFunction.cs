using Brizbee.Functions.Alerts.Serialization;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using NodaTime;
using System;
using System.Data.SqlClient;

namespace Brizbee.Functions.Alerts
{
    public class GenerateFunction
    {
        private ILogger logger;

        [FunctionName("GenerateFunction")]
        public void Run([TimerTrigger("0 */10 * * * *")]TimerInfo myTimer, ILogger log)
        {
            logger = log;
            log.LogInformation($"GenerateFunction executed at: {DateTime.Now}");

            GenerateExceededAlerts();
        }

        private void GenerateExceededAlerts()
        {
            try
            {
                var instant = SystemClock.Instance.GetCurrentInstant();
                var systemZone = DateTimeZoneProviders.Tzdb.GetSystemDefault();
                var zonedDateTime = instant.InZone(systemZone);
                var localDateTime = zonedDateTime.LocalDateTime;

                var monday = localDateTime.Previous(IsoDayOfWeek.Monday);
                var sunday = localDateTime.Next(IsoDayOfWeek.Sunday);

                logger.LogInformation(monday.ToDateTimeUnspecified().ToShortDateString());
                logger.LogInformation(sunday.ToDateTimeUnspecified().ToShortDateString());

                var connectionString = Environment.GetEnvironmentVariable("SqlContext").ToString();

                logger.LogInformation("Connecting to database");

                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    var usersSql = @"
                        SELECT
                            [U].[Id],
                            [U].[Name]
                        FROM
                            [Users] AS [U]
                        WHERE
                            [U].[IsDeleted] = 0 AND
                            [U].[OrganizationId] = 26;";
                    var users = connection.Query<User>(usersSql);

                    foreach (var user in users)
                    {
                        // --------------------------------------------------------
                        // Determine if user has reached 2100 minutes this week.
                        // --------------------------------------------------------

                        var minutes = 2100;
                        var exceededSql = $@"
                            DROP TABLE IF EXISTS #cumulative_minutes_{user.Id};

                            SELECT
                                DATEDIFF (MINUTE, [P].[InAt], [P].[OutAt]) AS [Punch_Minutes],
                                SUM (
		                            DATEDIFF (MINUTE, [P].[InAt], [P].[OutAt])
	                            ) OVER (
		                            PARTITION BY [P].[UserId]
		                            ORDER BY [P].[InAt] ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
	                            ) AS [Punch_CumulativeMinutes]
                            INTO
	                            #cumulative_minutes_{user.Id}
                            FROM
                                [Punches] AS [P]
                            WHERE
                                [P].[InAt] >= @Min AND
                                [P].[InAt] <= @Max AND
	                            [P].[OutAt] IS NOT NULL AND
                                [P].[UserId] = @UserId;

                            SELECT
	                            MAX ([Punch_CumulativeMinutes])
                            FROM
	                            #cumulative_minutes_{user.Id}
                            WHERE
	                            [Punch_CumulativeMinutes] > @ExceededMinutes;";

                        var exceeded = connection.QuerySingle<long?>(exceededSql, new { Min = monday.ToDateTimeUnspecified(), Max = sunday.ToDateTimeUnspecified(), UserId = user.Id, ExceededMinutes = minutes });

                        if (exceeded.HasValue)
                            logger.LogInformation($"User {user.Name} has exceeded minutes at {exceeded}");
                    }

                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
        }
    }
}
