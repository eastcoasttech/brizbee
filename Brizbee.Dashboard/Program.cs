using Brizbee.Dashboard.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Radzen;
using System;
using System.Threading.Tasks;

namespace Brizbee.Dashboard
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");

            // Configure HttpClient to communicate with API.
            builder.Services.AddHttpClient<ApiService>(client =>
            {
                client.BaseAddress = new Uri("http://localhost:54313");
            });

            // Configure additional services.
            builder.Services.AddSingleton<ExportService>();
            builder.Services.AddSingleton<TimesheetEntryService>();
            builder.Services.AddSingleton<CommitService>();
            builder.Services.AddSingleton<PunchService>();
            builder.Services.AddSingleton<UserService>();
            builder.Services.AddSingleton<CustomerService>();
            builder.Services.AddSingleton<TaskService>();
            builder.Services.AddSingleton<TaskTemplateService>();
            builder.Services.AddSingleton<JobService>();
            builder.Services.AddSingleton<RateService>();
            builder.Services.AddSingleton<OrganizationService>();
            builder.Services.AddSingleton<TimesheetEntryService>();
            builder.Services.AddSingleton<QBDInventoryItemService>();
            builder.Services.AddSingleton<QBDInventoryItemSyncService>();
            builder.Services.AddSingleton<QBDInventoryConsumptionService>();
            builder.Services.AddSingleton<QBDInventoryConsumptionSyncService>();
            builder.Services.AddSingleton<SharedService>();
            builder.Services.AddSingleton<GeolocationService>();

            builder.Services.AddScoped<LocalStorageService>();

            builder.Services.AddScoped<DialogService>();
            builder.Services.AddScoped<NotificationService>();
            builder.Services.AddScoped<TooltipService>();
            builder.Services.AddScoped<ContextMenuService>();

            await builder.Build().RunAsync();
        }
    }
}
