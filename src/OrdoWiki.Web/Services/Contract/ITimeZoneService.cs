namespace OrdoWiki.Web.Services.Contract;

public interface ITimeZoneService
{
    Task<TimeZoneInfo> GetLocalAsync();
    Task<DateTime> ToLocalAsync(DateTime utc);
}