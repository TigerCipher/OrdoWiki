namespace OrdoWiki.Web.Services;

using Microsoft.JSInterop;

public class TimeZoneService(
    IJSRuntime jsRuntime) : ITimeZoneService
{
    private TimeZoneInfo? _cached;

    public async Task<TimeZoneInfo> GetLocalAsync()
    {
        if (_cached is not null) return _cached;

        try
        {
            string id = await jsRuntime.InvokeAsync<string>("ordoTime.getTimeZone");
            _cached = TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch
        {
            _cached = TimeZoneInfo.Utc; // safe fallback if JS fails
        }

        return _cached;
    }

    public async Task<DateTime> ToLocalAsync(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(utc, DateTimeKind.Utc),
            await GetLocalAsync());
}