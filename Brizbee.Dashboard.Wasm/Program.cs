using Brizbee.Dashboard.Wasm.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Radzen;

namespace Brizbee.Dashboard.Wasm
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            // Configure HttpClient to communicate with API.
            builder.Services.AddHttpClient<ApiService>(client =>
            {
                client.BaseAddress = new Uri("https://api-dashboard.brizbee.com");
                client.Timeout = TimeSpan.FromMinutes(10);
            });

            // Configure additional services.
            builder.Services.AddSingleton<AuditService>();
            builder.Services.AddSingleton<AuthService>();
            builder.Services.AddSingleton<KioskService>();
            builder.Services.AddSingleton<ExportService>();
            builder.Services.AddSingleton<TimesheetEntryService>();
            builder.Services.AddSingleton<LockService>();
            builder.Services.AddSingleton<PunchService>();
            builder.Services.AddSingleton<UserService>();
            builder.Services.AddSingleton<CustomerService>();
            builder.Services.AddSingleton<TaskService>();
            builder.Services.AddSingleton<TaskTemplateService>();
            builder.Services.AddSingleton<PopulateTemplateService>();
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
