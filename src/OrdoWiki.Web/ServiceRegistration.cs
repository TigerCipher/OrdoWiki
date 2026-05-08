namespace OrdoWiki.Web;

using Components.Shared.Dialogs;
using Services;

public static class ServiceRegistration
{
    public static IServiceCollection RegisterOrdoServices(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPageService, PageService>();
        services.AddScoped<IMediaService, MediaService>();
        services.AddScoped<ICharacterService, CharacterService>();
        services.AddScoped<IOrdoDialogs, OrdoDialogs>();
        services.AddScoped<ITimeZoneService, TimeZoneService>();
        services.AddScoped<ThemeState>();
        services.AddSingleton<IMarkdownService, MarkdownService>();
        
        return services;
    }
}