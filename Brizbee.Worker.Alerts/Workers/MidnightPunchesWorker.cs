//
//  MidnightPunchesWorker.cs
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

using System.Text.Json.Serialization;
using Brizbee.Worker.Alerts.Serialization;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NodaTime;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Brizbee.Worker.Alerts.Workers;

public class MidnightPunchesWorker(IHostApplicationLifetime hostLifetime, ILogger<MidnightPunchesWorker> logger, IConfiguration configuration)
    : IHostedService
{
    private readonly IHostApplicationLifetime _hostLifetime = hostLifetime ?? throw new ArgumentNullException(nameof(hostLifetime));
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Midnight punches worker executed at: {Timestamp}", DateTime.UtcNow);

        await GenerateMidnightPunchEmailsAsync();

        _hostLifetime.StopApplication();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task GenerateMidnightPunchEmailsAsync()
    {
        try
        {
            var instant = SystemClock.Instance.GetCurrentInstant();
            var systemZone = DateTimeZoneProviders.Tzdb.GetZoneOrNull("America/New_York");
            var zonedDateTime = instant.InZone(systemZone!);
            var localDateTime = zonedDateTime.LocalDateTime;

            var midnight = new DateTime(localDateTime.Year, localDateTime.Month, localDateTime.Day, 0, 0, 0);

            _logger.LogInformation("{MidnightDate} {MidnightTime}", midnight.ToShortDateString(), midnight.ToShortTimeString());

            var connectionString = configuration.GetConnectionString("SqlContext");

            _logger.LogInformation("Connecting to database");

            await using var connection = new SqlConnection(connectionString);

            await connection.OpenAsync();

            const string organizationsSql = """
                                            SELECT
                                                [O].[Id]
                                            FROM
                                                [Organizations] AS [O];
                                            """;
            var organizations = await connection.QueryAsync<Organization>(organizationsSql);

            foreach (var organization in organizations)
            {
                // Collect the recipients for this Email.
                const string recipientsSql = """
                                             SELECT
                                                 [U].[Id],
                                                 [U].[Name],
                                                 [U].[EmailAddress]
                                             FROM
                                                 [Users] AS [U]
                                             WHERE
                                                 [U].[IsDeleted] = 0 AND
                                                 [U].[ShouldSendMidnightPunchEmail] = 1 AND
                                                 [U].[OrganizationId] = @OrganizationId;
                                             """;
                var recipients = await connection.QueryAsync<User>(recipientsSql, new
                {
                    OrganizationId = organization.Id
                });

                var recipientsList = recipients.ToList();

                // No need to continue if no one should receive the Email.
                if (recipientsList.Count == 0)
                {
                    continue;
                }

                // Find punches that were still open through last midnight.
                const string midnightPunchesSql = """
                                                  SELECT
                                                      [U].[Name] AS [User_Name],
                      
                                                      FORMAT([P].[InAt], 'ddd, MMM dd, yyyy h:mm tt') AS [Punch_InAt],
                      
                                                      CASE
                                                          WHEN [P].[OutAt] IS NOT NULL THEN FORMAT([P].[OutAt], 'ddd, MMM dd, yyyy h:mm tt')
                                                          WHEN [P].[OutAt] IS NULL THEN 'STILL WORKING'
                                                          ELSE 'STILL WORKING'
                                                      END AS [Punch_OutAt],
                      
                                                      [T].[Number] AS [Task_Number],
                                                      [T].[Name] AS [Task_Name],
                      
                                                      [J].[Number] AS [Project_Number],
                                                      [J].[Name] AS [Project_Name],
                      
                                                      [C].[Number] AS [Customer_Number],
                                                      [C].[Name] AS [Customer_Name]
                                                  FROM
                                                      [Punches] AS [P]
                                                  JOIN
                                                      [Users] AS [U] ON [U].[Id] = [P].[UserId]
                                                  JOIN
                                                      [Tasks] AS [T] ON [T].[Id] = [P].[TaskId]
                                                  JOIN
                                                      [Jobs] AS [J] ON [J].[Id] = [T].[JobId]
                                                  JOIN
                                                      [Customers] AS [C] ON [C].[Id] = [J].[CustomerId]
                                                  WHERE
                                                      [P].[InAt] < @Midnight AND
                                                      ([P].[OutAt] > @Midnight OR [P].[OutAt] IS NULL) AND
                                                      [U].[IsDeleted] = 0 AND
                                                      [U].[OrganizationId] = @OrganizationId;
                                                  """;

                var midnightPunches = await connection.QueryAsync<MidnightPunch>(midnightPunchesSql, new
                {
                    Midnight = midnight,
                    OrganizationId = organization.Id
                });

                var midnightPunchesList = midnightPunches.ToList();

                _logger.LogInformation("{MidnightPunchCount} punches through midnight", midnightPunchesList.Count);

                // Do not continue if there are no punches.
                if (midnightPunchesList.Count == 0)
                {
                    continue;
                }

                try
                {
                    var tos = new List<EmailAddress>();
                    foreach (var recipient in recipientsList.Where(r => !string.IsNullOrEmpty(r.EmailAddress)))
                        tos.Add(new EmailAddress() { Email = recipient.EmailAddress, Name = recipient.Name });

                    var apiKey = configuration.GetValue<string>("SendGridApiKey");
                    var templateId = configuration.GetValue<string>("SendGridMidnightPunchTemplateId");

                    var dynamicTemplateData = new DynamicTemplateData()
                    {
                        MidnightPunches = [.. midnightPunchesList]
                    };

                    var client = new SendGridClient(apiKey);
                    var from = new EmailAddress("BRIZBEE <administrator@brizbee.com>");
                    var msg = MailHelper.CreateSingleTemplateEmailToMultipleRecipients(from, tos, templateId, dynamicTemplateData);
                    await client.SendEmailAsync(msg);
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

    internal class DynamicTemplateData
    {
        [JsonPropertyName("midnight_punches")]
        public List<MidnightPunch>? MidnightPunches { get; set; }
    }
}
