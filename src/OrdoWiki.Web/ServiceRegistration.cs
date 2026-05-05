namespace OrdoWiki.Web;

using Components.Shared.Dialogs;
using Services;

public static class ServiceRegistration
{
    public static IServiceCollection RegisterOrdoServices(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPageService, PageService>();
        services.AddScoped<IOrdoDialogs, OrdoDialogs>();
        services.AddScoped<ITimeZoneService, TimeZoneService>();
        services.AddSingleton<IMarkdownService, MarkdownService>();
        
        return services;
    }
}