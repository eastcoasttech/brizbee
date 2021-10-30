//
//  GenerateFunction.cs
//  BRIZBEE Alerts Function
//
//  Copyright (C) 2021 East Coast Technology Services, LLC
//
//  This file is part of the BRIZBEE Alerts Function.
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

using Azure.Storage.Blobs;
using Brizbee.Functions.Alerts.Serialization;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Brizbee.Functions.Alerts
{
    public class GenerateFunction
    {
        private ILogger logger;

        //[FunctionName("GenerateFunction")]
        public void Run([TimerTrigger("0 */30 * * * *", RunOnStartup = true)]TimerInfo myTimer, ILogger log)
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

                logger.LogInformation($"{monday.ToDateTimeUnspecified().ToShortDateString()} thru {sunday.ToDateTimeUnspecified().ToShortDateString()}");
                
                var connectionString = Environment.GetEnvironmentVariable("SqlContext").ToString();

                logger.LogInformation("Connecting to database");

                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    var alerts = new List<Alert>(0);

                    var organizationsSql = @"
                        SELECT
                            [O].[Id]
                        FROM
                            [Organizations] AS [O];";
                    var organizations = connection.Query<Organization>(organizationsSql);

                    foreach (var organization in organizations)
                    {
                        var usersSql = @"
                            SELECT
                                [U].[Id],
                                [U].[Name]
                            FROM
                                [Users] AS [U]
                            WHERE
                                [U].[IsDeleted] = 0 AND
                                [U].[OrganizationId] = @OrganizationId;";
                        var users = connection.Query<User>(usersSql, new
                        {
                            OrganizationId = organization.Id
                        });

                        foreach (var user in users)
                        {
                            // ----------------------------------------------------
                            // Check for a total that exceeds the threshold.
                            // ----------------------------------------------------

                            var totalThreshold = 2100; // Minutes
                            var totalSql = $@"
                                SELECT
	                                MAX ([X].[Punch_CumulativeMinutes])
                                FROM
	                                (
		                                SELECT
			                                DATEDIFF (MINUTE, [P].[InAt], [P].[OutAt]) AS [Punch_Minutes],
			                                SUM (
				                                DATEDIFF (MINUTE, [P].[InAt], [P].[OutAt])
			                                ) OVER (
				                                PARTITION BY [P].[UserId]
				                                ORDER BY [P].[InAt] ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
			                                ) AS [Punch_CumulativeMinutes]
		                                FROM
			                                [Punches] AS [P]
		                                WHERE
			                                [P].[InAt] >= @Min AND
			                                [P].[InAt] <= @Max AND
			                                [P].[OutAt] IS NOT NULL AND
			                                [P].[UserId] = @UserId
	                                ) AS [X]
                                WHERE
	                                [X].[Punch_CumulativeMinutes] > @ExceededMinutes;";

                            var total = connection.QuerySingle<long?>(totalSql, new
                            {
                                Min = monday.ToDateTimeUnspecified(),
                                Max = sunday.ToDateTimeUnspecified(),
                                UserId = user.Id,
                                ExceededMinutes = totalThreshold
                            });

                            if (total.HasValue)
                                alerts.Add(new Alert()
                                {
                                    Type = "total.exceeded",
                                    Value = total.Value,
                                    User = user
                                });

                            // ----------------------------------------------------
                            // Check for any punch that exceeds the threshold.
                            // ----------------------------------------------------

                            var punchesThreshold = 600; // Minutes
                            var punchesSql = $@"
                                SELECT
                                    [X].[Id],
	                                [X].[Punch_Minutes]
                                FROM
	                                (
		                                SELECT
                                            [P].[Id],
			                                DATEDIFF (MINUTE, [P].[InAt], [P].[OutAt]) AS [Punch_Minutes]
		                                FROM
			                                [Punches] AS [P]
		                                WHERE
			                                [P].[InAt] >= @Min AND
			                                [P].[InAt] <= @Max AND
			                                [P].[OutAt] IS NOT NULL AND
			                                [P].[UserId] = @UserId
	                                ) AS [X]
                                WHERE
	                                [X].[Punch_Minutes] > @ExceededMinutes;";

                            var punches = connection.Query<Exceeded>(punchesSql, new
                            {
                                Min = monday.ToDateTimeUnspecified(),
                                Max = sunday.ToDateTimeUnspecified(),
                                UserId = user.Id,
                                ExceededMinutes = punchesThreshold
                            });

                            foreach (var punch in punches)
                                alerts.Add(new Alert()
                                {
                                    Type = "punch.exceeded",
                                    Value = punch.Punch_Minutes,
                                    User = user
                                });
                        }

                        try
                        {
                            var json = JsonSerializer.Serialize(alerts);

                            // Prepare to upload the json.
                            var azureConnectionString = Environment.GetEnvironmentVariable("AlertsAzureStorageConnectionString").ToString();
                            BlobServiceClient blobServiceClient = new BlobServiceClient(azureConnectionString);
                            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("alerts");
                            BlobClient blobClient = containerClient.GetBlobClient($"{organization.Id}.json");

                            // Perform the upload.
                            using (var stream = new MemoryStream(Encoding.Default.GetBytes(json), false))
                            {
                                blobClient.Upload(stream, overwrite: true);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex.ToString());
                        }
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
