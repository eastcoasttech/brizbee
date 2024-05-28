//
//  GenerateAlertsWorker.cs
//  BRIZBEE Alerts Worker
//
//  Copyright (C) 2021-2024 East Coast Technology Services, LLC
//
//  This file is part of the BRIZBEE Alerts Worker.
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

using System.Configuration;
using System.Text;
using System.Text.Json;
using Azure.Storage.Blobs;
using Brizbee.Worker.Alerts.Serialization;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Brizbee.Worker.Alerts.Workers;

public class GenerateAlertsWorker(IHostApplicationLifetime hostLifetime, ILogger<GenerateAlertsWorker> logger, IConfiguration configuration)
    : IHostedService
{
    private readonly IHostApplicationLifetime _hostLifetime = hostLifetime ?? throw new ArgumentNullException(nameof(hostLifetime));
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generate alerts worker executed at: {Timestamp}", DateTime.UtcNow);

        await GenerateExceededAlertsAsync();

        _hostLifetime.StopApplication();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
    

    private async Task GenerateExceededAlertsAsync()
    {
        try
        {
            var instant = SystemClock.Instance.GetCurrentInstant();
            var systemZone = DateTimeZoneProviders.Tzdb.GetSystemDefault();
            var zonedDateTime = instant.InZone(systemZone);
            var localDateTime = zonedDateTime.LocalDateTime;

            var monday = localDateTime.Previous(IsoDayOfWeek.Monday);
            var sunday = localDateTime.Next(IsoDayOfWeek.Sunday);

            _logger.LogInformation("{Monday} thru {Sunday}", monday.ToDateTimeUnspecified().ToShortDateString(), sunday.ToDateTimeUnspecified().ToShortDateString());
            
            var connectionString = configuration.GetConnectionString("SqlContext");

            _logger.LogInformation("Connecting to database");

            await using var connection = new SqlConnection(connectionString);

            await connection.OpenAsync();

            var alerts = new List<Alert>(0);

            const string organizationsSql = """
                                            SELECT
                                                [O].[Id]
                                            FROM
                                                [Organizations] AS [O];
                                            """;
            var organizations = await connection.QueryAsync<Organization>(organizationsSql);

            foreach (var organization in organizations)
            {
                const string usersSql = """
                                        SELECT
                                            [U].[Id],
                                            [U].[Name]
                                        FROM
                                            [Users] AS [U]
                                        WHERE
                                            [U].[IsDeleted] = 0 AND
                                            [U].[OrganizationId] = @OrganizationId;
                                        """;
                var users = await connection.QueryAsync<User>(usersSql, new
                {
                    OrganizationId = organization.Id
                });

                foreach (var user in users)
                {
                    // ----------------------------------------------------
                    // Check for a total that exceeds the threshold.
                    // ----------------------------------------------------

                    const int totalThreshold = 2100; // Minutes
                    const string totalSql = """
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
                                                [X].[Punch_CumulativeMinutes] > @ExceededMinutes;
                                            """;

                    var total = await connection.QuerySingleAsync<long?>(totalSql, new
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

                    const int punchesThreshold = 600; // Minutes
                    const string punchesSql = """
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
                                                  [X].[Punch_Minutes] > @ExceededMinutes;
                                              """;

                    var punches = await connection.QueryAsync<Exceeded>(punchesSql, new
                    {
                        Min = monday.ToDateTimeUnspecified(),
                        Max = sunday.ToDateTimeUnspecified(),
                        UserId = user.Id,
                        ExceededMinutes = punchesThreshold
                    });

                    alerts.AddRange(punches.Select(punch => new Alert
                    {
                        Type = "punch.exceeded",
                        Value = punch.Punch_Minutes,
                        User = user
                    }));
                }

                try
                {
                    var json = JsonSerializer.Serialize(alerts);

                    // Prepare to upload the json.
                    var azureConnectionString = configuration.GetValue<string>("AlertsAzureStorageConnectionString");
                    var blobServiceClient = new BlobServiceClient(azureConnectionString);
                    var containerClient = blobServiceClient.GetBlobContainerClient("alerts");
                    var blobClient = containerClient.GetBlobClient($"{organization.Id}.json");

                    // Perform the upload.
                    using var stream = new MemoryStream(Encoding.Default.GetBytes(json), false);

                    await blobClient.UploadAsync(stream, overwrite: true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "{Message}", ex.Message);
                }
            }

            connection.Close();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", ex.Message);
        }
    }
}
