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

using Brizbee.Worker.Alerts.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
        var configuration = new ConfigurationBuilder()
            .AddCommandLine(args)
            .AddJsonFile("appsettings.json")
            .Build();

        // Determine the connection string for Application Insights.
        var operation = configuration["operation"] ?? throw new ArgumentException("operation must be provided");
        var appInsightsConnectionString = operation.ToUpper() switch
        {
            "MIDNIGHT" => configuration.GetValue<string>("ApplicationInsights:ConnectionStringForMidnightPunches"),
            "GENERATE" => configuration.GetValue<string>("ApplicationInsights:ConnectionStringForGenerateAlerts"),
            _ => throw new ArgumentException("Invalid argument for operation. Must be MIDNIGHT or GENERATE.")
        };

        return Host.CreateDefaultBuilder()
            .ConfigureHostConfiguration(config =>
            {
                config.AddCommandLine(args);
                config.AddJsonFile("appsettings.json", optional: true);
            })
            .ConfigureServices(services =>
            {
                services.AddLogging(builder =>
                {
                    if (!string.IsNullOrEmpty(appInsightsConnectionString))
                    {
                        // Application Insights is registered as a logger provider.
                        builder.AddApplicationInsights(
                            configureTelemetryConfiguration: (config) => config.ConnectionString = appInsightsConnectionString,
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
                
                switch (operation.ToUpper())
                {
                    case "MIDNIGHT":
                        services.AddHostedService<MidnightPunchesWorker>();
                        break;
                    case "GENERATE":
                        services.AddHostedService<GenerateAlertsWorker>();
                        break;
                }
            });
    }
}
