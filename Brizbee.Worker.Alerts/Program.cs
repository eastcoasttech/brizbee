//
//  Program.cs
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

using Brizbee.Worker.Alerts.Jobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Brizbee.Worker.Alerts;

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        var host = CreateHostBuilder(args);

        await host.RunConsoleAsync();

        return Environment.ExitCode;
    }

    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        string? appInsightsConnectionString = null;
        string operation = string.Empty;
        string schedule = string.Empty;

        return Host.CreateDefaultBuilder()
            .ConfigureHostConfiguration(configurationBuilder =>
            {
                configurationBuilder.AddCommandLine(args);
                configurationBuilder.AddJsonFile("appsettings.json", optional: true);
                configurationBuilder.AddEnvironmentVariables();
            })
            .ConfigureAppConfiguration((hostingContext, configurationBuilder) =>
            {
                var hostEnvironment = hostingContext.HostingEnvironment;

                configurationBuilder.AddCommandLine(args);
                configurationBuilder.AddJsonFile("appsettings.json", optional: true);
                configurationBuilder.AddJsonFile($"appsettings.{hostEnvironment.EnvironmentName}.json", optional: true);
                configurationBuilder.AddEnvironmentVariables();

                var configuration = configurationBuilder.Build();

                if (string.IsNullOrEmpty(configuration.GetConnectionString("Default")))
                {
                    throw new InvalidOperationException(
                        "Database connection string 'Default' must be provided.");
                }

                // Determine the connection string for Application Insights.
                operation = configuration["operation"] ?? throw new ArgumentException("operation must be provided");
                appInsightsConnectionString = operation.ToUpper() switch
                {
                    "MIDNIGHT" => configuration.GetValue<string>("ApplicationInsights:ConnectionStringForMidnightPunches"),
                    "GENERATE" => configuration.GetValue<string>("ApplicationInsights:ConnectionStringForGenerateAlerts"),
                    _ => throw new ArgumentException("Invalid argument for operation. Must be MIDNIGHT or GENERATE.")
                };

                schedule = configuration["schedule"] ?? throw new ArgumentException("schedule must be provided");
            })
            .ConfigureServices(services =>
            {
                services.AddLogging(builder =>
                {
                    if (!string.IsNullOrEmpty(appInsightsConnectionString))
                    {
                        // Application Insights is registered as a logger provider.
                        builder.AddApplicationInsights(
                            configureTelemetryConfiguration: config => config.ConnectionString = appInsightsConnectionString,
                            configureApplicationInsightsLoggerOptions: _ => { }
                        );
                    }

                    // Log to the console.
                    builder.AddConsole();
                });

                if (!string.IsNullOrEmpty(appInsightsConnectionString))
                {
                    services.AddApplicationInsightsTelemetryWorkerService(
                        new Microsoft.ApplicationInsights.WorkerService.ApplicationInsightsServiceOptions
                        {
                            ConnectionString = appInsightsConnectionString
                        });
                }

                services.Configure<ConsoleLifetimeOptions>(options => options.SuppressStatusMessages = true);

                services.AddQuartzHostedService(options =>
                {
                    options.WaitForJobsToComplete = true;
                });

                switch (operation!.ToUpper())
                {
                    case "MIDNIGHT":

                        services.AddQuartz(q =>
                        {
                            var jobKey = new JobKey("MidnightPunchesJob");
                            q.AddJob<MidnightPunchesJob>(options => options.WithIdentity(jobKey));

                            q.AddTrigger(options => options
                                .ForJob(jobKey)
                                .WithIdentity("MidnightPunchesJob-trigger")
                                .WithCronSchedule(schedule)
                            );
                        });

                        break;
                    case "GENERATE":

                        services.AddQuartz(q =>
                        {
                            var jobKey = new JobKey("GenerateAlertsJob");
                            q.AddJob<GenerateAlertsJob>(options => options.WithIdentity(jobKey));

                            q.AddTrigger(options => options
                                .ForJob(jobKey)
                                .WithIdentity("GenerateAlertsJob-trigger")
                                .WithCronSchedule(schedule)
                            );
                        });

                        break;
                }
            });
    }
}
