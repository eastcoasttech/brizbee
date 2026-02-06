//
//  MidnightPunchesJob.cs
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

using Brizbee.Worker.Alerts.Serialization;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NodaTime;
using Quartz;
using RestSharp;
using RestSharp.Authenticators;
using System.Text.Json;

namespace Brizbee.Worker.Alerts.Jobs;

public class MidnightPunchesJob(ILogger<MidnightPunchesJob> logger, IConfiguration configuration) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var instant = SystemClock.Instance.GetCurrentInstant();
            var systemZone = DateTimeZoneProviders.Tzdb.GetZoneOrNull("America/New_York");
            var zonedDateTime = instant.InZone(systemZone!);
            var localDateTime = zonedDateTime.LocalDateTime;

            var midnight = new DateTime(localDateTime.Year, localDateTime.Month, localDateTime.Day, 0, 0, 0);

            logger.LogInformation("{MidnightDate} {MidnightTime}", midnight.ToShortDateString(), midnight.ToShortTimeString());

            var connectionString = configuration.GetConnectionString("Default");

            logger.LogInformation("Connecting to database");

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

                logger.LogInformation("{MidnightPunchCount} punches through midnight", midnightPunchesList.Count);

                // Do not continue if there are no punches.
                if (midnightPunchesList.Count == 0)
                {
                    continue;
                }

                try
                {
                    var dynamicTemplateData = new
                    {
                        midnight_punches = midnightPunchesList.Select(p => new
                        {
                            user_name = p.User_Name,
                            punch_outAt = p.Punch_OutAt,
                            punch_inAt = p.Punch_InAt,
                            task_number = p.Task_Number,
                            task_name = p.Task_Name,
                            project_number = p.Project_Number,
                            project_name = p.Project_Name,
                            customer_number = p.Customer_Number,
                            customer_name = p.Customer_Name
                        })
                    };

                    var apiKey = configuration.GetValue<string>("MailgunApiKey");

                    var options = new RestClientOptions("https://api.mailgun.net")
                    {
                        Authenticator = new HttpBasicAuthenticator("api", apiKey ?? "API_KEY")
                    };

                    var client = new RestClient(options);

                    var request = new RestRequest("/v3/mg.brizbee.com/messages", Method.Post);

                    request.AlwaysMultipartFormData = true;

                    request.AddParameter("from", "Brizbee <postmaster@mg.brizbee.com>");
                    request.AddParameter("to", string.Join(',', recipientsList.Select(r => $"{r.Name} <{r.EmailAddress}>")));
                    request.AddParameter("subject", "Punches through Midnight");
                    request.AddParameter("template", "midnight punches");
                    request.AddParameter("t:variables", JsonSerializer.Serialize(dynamicTemplateData));

                    await client.ExecuteAsync(request);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "{Message}", ex.Message);
                }
            }

            connection.Close();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Message}", ex.Message);
        }
    }
}
