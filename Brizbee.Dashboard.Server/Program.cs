using Brizbee.Dashboard.Server.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Radzen;

namespace Brizbee.Dashboard.Server;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorPages();
        builder.Services.AddServerSideBlazor();

        builder.Services.AddScoped<AuthenticationStateProvider,
            CustomAuthenticationStateProvider>();

        builder.Services.AddDbContextFactory<PrimaryContext>(options =>
            options.UseSqlServer(builder.Configuration["ConnectionStrings:PrimaryContext"]));

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
        builder.Services.AddSingleton<TaskService>();
        builder.Services.AddSingleton<TaskTemplateService>();
        builder.Services.AddSingleton<TimesheetEntryService>();
        builder.Services.AddSingleton<UserService>();
        
        builder.Services.AddScoped<GeolocationService>();
        builder.Services.AddScoped<SharedService>();
        builder.Services.AddScoped<LocalStorageService>();

        builder.Services.AddScoped<DialogService>();
        builder.Services.AddScoped<NotificationService>();
        builder.Services.AddScoped<TooltipService>();
        builder.Services.AddScoped<ContextMenuService>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
        }

        app.UseStaticFiles();

        app.UseRouting();

        app.MapBlazorHub();
        app.MapFallbackToPage("/_Host");

        app.Run();
    }
}
