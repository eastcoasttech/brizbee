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
                client.BaseAddress = new Uri("https://app-brizbee-api-prod-slot-1.azurewebsites.net");
                client.Timeout = TimeSpan.FromMinutes(10);
            });

            // Configure additional services.
            builder.Services.AddSingleton<AccountService>();
            builder.Services.AddSingleton<AuditService>();
            builder.Services.AddSingleton<AuthService>();
            builder.Services.AddSingleton<CustomerService>();
            builder.Services.AddSingleton<EntryService>();
            builder.Services.AddSingleton<ExportService>();
            builder.Services.AddSingleton<GeolocationService>();
            builder.Services.AddSingleton<InvoiceService>();
            builder.Services.AddSingleton<JobService>();
            builder.Services.AddSingleton<KioskService>();
            builder.Services.AddSingleton<LockService>();
            builder.Services.AddSingleton<OrganizationService>();
            builder.Services.AddSingleton<PaycheckService>();
            builder.Services.AddSingleton<PopulateTemplateService>();
            builder.Services.AddSingleton<PunchService>();
            builder.Services.AddSingleton<QBDInventoryItemService>();
            builder.Services.AddSingleton<QBDInventoryItemSyncService>();
            builder.Services.AddSingleton<QBDInventoryConsumptionService>();
            builder.Services.AddSingleton<QBDInventoryConsumptionSyncService>();
            builder.Services.AddSingleton<RateService>();
            builder.Services.AddSingleton<SharedService>();
            builder.Services.AddSingleton<TaskService>();
            builder.Services.AddSingleton<TaskTemplateService>();
            builder.Services.AddSingleton<TimesheetEntryService>();
            builder.Services.AddSingleton<UserService>();

            builder.Services.AddScoped<LocalStorageService>();

            builder.Services.AddScoped<DialogService>();
            builder.Services.AddScoped<NotificationService>();
            builder.Services.AddScoped<TooltipService>();
            builder.Services.AddScoped<ContextMenuService>();

            await builder.Build().RunAsync();
        }
    }
}
