//
//  MidnightPunchFunction.cs
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

using Brizbee.Functions.Alerts.Serialization;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NodaTime;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text.Json.Serialization;

namespace Brizbee.Functions.Alerts
{
    public class MidnightPunchFunction
    {
        private ILogger logger;

        [FunctionName("MidnightPunchFunction")]
        public void Run([TimerTrigger("0 30 6 * * *")]TimerInfo myTimer, ILogger log)
        {
            logger = log;
            log.LogInformation($"MidnightPunchFunction executed at: {DateTime.Now}");

            GenerateMidnightPunchEmails();
        }

        private void GenerateMidnightPunchEmails()
        {
            try
            {
                var instant = SystemClock.Instance.GetCurrentInstant();
                var systemZone = DateTimeZoneProviders.Tzdb.GetSystemDefault();
                var zonedDateTime = instant.InZone(systemZone);
                var localDateTime = zonedDateTime.LocalDateTime;

                var midnight = new DateTime(localDateTime.Year, localDateTime.Month, localDateTime.Day, 0, 0, 0);

                logger.LogInformation($"{midnight.ToShortDateString()} {midnight.ToShortTimeString()}");

                var connectionString = Environment.GetEnvironmentVariable("SqlContext").ToString();

                logger.LogInformation("Connecting to database");

                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    var organizationsSql = @"
                        SELECT
                            [O].[Id]
                        FROM
                            [Organizations] AS [O];";
                    var organizations = connection.Query<Organization>(organizationsSql);

                    foreach (var organization in organizations)
                    {
                        // Collect the recipients for this Email.
                        var recipientsSql = @"
                            SELECT
                                [U].[Id],
                                [U].[Name],
                                [U].[EmailAddress]
                            FROM
                                [Users] AS [U]
                            WHERE
                                [U].[IsDeleted] = 0 AND
                                [U].[ShouldSendMidnightPunchEmail] = 1 AND
                                [U].[OrganizationId] = @OrganizationId;";
                        var recipients = connection.Query<User>(recipientsSql, new
                        {
                            OrganizationId = organization.Id
                        });

                        // No need to continue if no one should receive the Email.
                        if (!recipients.Any())
                            continue;

                        // Find punches that were still open through last midnight.
                        var midnightPunchesSql = @"
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
                                [U].[OrganizationId] = @OrganizationId;";

                        var midnightPunches = connection.Query<MidnightPunch>(midnightPunchesSql, new
                        {
                            Midnight = midnight,
                            OrganizationId = organization.Id
                        });

                        logger.LogInformation($"{midnightPunches.Count()} punches through midnight");

                        // Do not continue if there are no punches.
                        if (!midnightPunches.Any())
                            continue;

                        try
                        {
                            var tos = new List<EmailAddress>();
                            foreach (var recipient in recipients)
                                tos.Add(new EmailAddress() { Email = recipient.EmailAddress, Name = recipient.Name });

                            var apiKey = Environment.GetEnvironmentVariable("SendGridApiKey").ToString();
                            var templateId = Environment.GetEnvironmentVariable("SendGridMidnightPunchTemplateId").ToString();

                            var dynamicTemplateData = new DynamicTemplateData()
                            {
                                MidnightPunches = midnightPunches.ToList()
                            };

                            var client = new SendGridClient(apiKey);
                            var from = new EmailAddress("BRIZBEE <administrator@brizbee.com>");
                            var msg = MailHelper.CreateSingleTemplateEmailToMultipleRecipients(from, tos, templateId, dynamicTemplateData);
                            var response = client.SendEmailAsync(msg);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex.Message);
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

    class DynamicTemplateData
    {
        [JsonProperty("midnight_punches")]
        [JsonPropertyName("midnight_punches")]
        public List<MidnightPunch> MidnightPunches { get; set; }
    }
}
