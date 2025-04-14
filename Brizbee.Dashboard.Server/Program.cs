using Brizbee.Dashboard.Server.Components;
using Brizbee.Dashboard.Server.Services;
using Microsoft.EntityFrameworkCore;
using Radzen;

namespace Brizbee.Dashboard.Server;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();
        
        // Add Radzen Blazor components.
        builder.Services.AddRadzenComponents();
        
        // Add the Entity Framework database context.
        builder.Services.AddDbContextFactory<PrimaryDbContext>(options =>
            options.UseSqlServer(builder.Configuration["ConnectionStrings:PrimaryDbContext"]));

        // Configure HttpClient to communicate with API.
        builder.Services.AddHttpClient<ApiService>(client =>
        {
            client.BaseAddress = new Uri("http://localhost:5113");
            client.Timeout = TimeSpan.FromMinutes(10);
        });

        // Configure additional services.
        builder.Services.AddScoped<AuditService>();
        builder.Services.AddScoped<AuthService>();
        builder.Services.AddScoped<KioskService>();
        builder.Services.AddScoped<ExportService>();
        builder.Services.AddScoped<TimesheetEntryService>();
        builder.Services.AddScoped<LockService>();
        builder.Services.AddScoped<PunchService>();
        builder.Services.AddScoped<UserService>();
        builder.Services.AddScoped<CustomerService>();
        builder.Services.AddScoped<TaskService>();
        builder.Services.AddScoped<TaskTemplateService>();
        builder.Services.AddScoped<PopulateTemplateService>();
        builder.Services.AddScoped<JobService>();
        builder.Services.AddScoped<RateService>();
        builder.Services.AddScoped<OrganizationService>();
        builder.Services.AddScoped<TimesheetEntryService>();
        builder.Services.AddScoped<QBDInventoryItemService>();
        builder.Services.AddScoped<QBDInventoryItemSyncService>();
        builder.Services.AddScoped<QBDInventoryConsumptionService>();
        builder.Services.AddScoped<QBDInventoryConsumptionSyncService>();

        builder.Services.AddScoped<GeolocationService>();
        builder.Services.AddScoped<SharedService>();

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

        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
    }
}