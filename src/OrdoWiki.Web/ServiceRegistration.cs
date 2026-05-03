namespace OrdoWiki.Web;

using Services;

public static class ServiceRegistration
{
    public static IServiceCollection RegisterOrdoServices(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPageService, PageService>();
        
        return services;
    }
}