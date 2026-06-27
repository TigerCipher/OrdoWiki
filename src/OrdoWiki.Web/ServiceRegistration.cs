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
        services.AddScoped<IGalleryService, GalleryService>();
        services.AddScoped<IMandoCalendarService, MandoCalendarService>();
        services.AddScoped<ITimelineService, TimelineService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<IBannerService, BannerService>();
        services.AddScoped<BannerState>();
        services.AddScoped<ISiteThemeService, SiteThemeService>();
        services.AddScoped<SiteThemeState>();
        services.AddScoped<IHomePageService, HomePageService>();
        services.AddScoped<IRelatedItemsService, RelatedItemsService>();
        services.AddScoped<IGhostUserService, GhostUserService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IOrdoDialogs, OrdoDialogs>();
        services.AddScoped<ITimeZoneService, TimeZoneService>();
        services.AddScoped<ThemeState>();
        services.AddSingleton<IMarkdownService, MarkdownService>();
        
        return services;
    }
}