namespace Brizbee.Dashboard.Server.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBrowserTimeProvider(this IServiceCollection services)
        => services.AddScoped<TimeProvider, BrowserTimeProvider>();
}
