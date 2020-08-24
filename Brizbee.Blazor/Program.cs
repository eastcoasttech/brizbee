using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Blazored.Modal;
using Syncfusion.Blazor;

namespace Brizbee.Blazor
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Register the key for Syncfusion library
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("MjgwMDcxQDMxMzgyZTMxMmUzMEE1S2FnQTAwNjI5QUxFbEhIZWVTcEIvNlNGNXhvTEMrVVJmbnpmclRNQjA9");

            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("app");
            builder.Services.AddTransient(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
            builder.Services.AddBlazoredModal();
            builder.Services.AddSyncfusionBlazor();

            await builder.Build().RunAsync();
        }
    }
}
