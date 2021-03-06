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
                client.BaseAddress = new Uri("https://app-brizbee-prod.azurewebsites.net");
            });

            // Configure additional services.
            builder.Services.AddSingleton<PunchService>();
            builder.Services.AddSingleton<UserService>();
            builder.Services.AddSingleton<CustomerService>();
            builder.Services.AddSingleton<TaskService>();
            builder.Services.AddSingleton<JobService>();
            builder.Services.AddSingleton<SharedService>();

            builder.Services.AddScoped<LocalStorageService>();

            builder.Services.AddScoped<DialogService>();
            builder.Services.AddScoped<NotificationService>();
            builder.Services.AddScoped<TooltipService>();
            builder.Services.AddScoped<ContextMenuService>();

            await builder.Build().RunAsync();
        }
    }
}
