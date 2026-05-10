namespace OrdoWiki.Web.Services;

using Data;
using Data.Calendars;
using Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Models;
using Models.Requests;

/// <summary>
/// Provides cached read access to calendar configuration plus admin CRUD for it.
/// Cached because reads happen on every timeline render and rarely change; the
/// cache is dropped whenever an admin modifies a month or era.
/// </summary>
public class MandoCalendarService(
    ApplicationDbContext context,
    IMemoryCache cache) : IMandoCalendarService
{
    private const string MonthsCacheKey = "mando:months";
    private const string ErasCacheKey = "mando:eras";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(12);

    public async Task<IReadOnlyList<MandoMonthDto>> GetMonthsAsync()
    {
        if (cache.TryGetValue(MonthsCacheKey, out IReadOnlyList<MandoMonthDto>? cached) && cached is not null)
            return cached;

        List<MandoMonthDto> months = await context.MandoMonths
            .AsNoTracking()
            .OrderBy(m => m.MonthIndex)
            .Select(m => MapToDto(m))
            .ToListAsync();

        cache.Set(MonthsCacheKey, (IReadOnlyList<MandoMonthDto>)months, CacheTtl);
        return months;
    }

    public async Task<IReadOnlyList<MandoEraDto>> GetErasAsync()
    {
        if (cache.TryGetValue(ErasCacheKey, out IReadOnlyList<MandoEraDto>? cached) && cached is not null)
            return cached;

        List<MandoEraDto> eras = await context.MandoEras
            .AsNoTracking()
            .OrderBy(e => e.SortOrder)
            .ThenBy(e => e.AnchorYear)
            .Select(e => MapToDto(e))
            .ToListAsync();

        cache.Set(ErasCacheKey, (IReadOnlyList<MandoEraDto>)eras, CacheTtl);
        return eras;
    }

    public async Task<string> FormatAsync(MandoDate date)
    {
        IReadOnlyList<MandoEraDto> eras = await GetErasAsync();
        IReadOnlyList<MandoMonthDto> months = await GetMonthsAsync();

        IReadOnlyList<MandoEraInfo> eraInfos = eras.Select(e => e.ToInfo()).ToList();
        MandoEraInfo? era = MandoEraResolver.Resolve(eraInfos, date.Year);
        string eraSuffix = era is null
            ? date.Year.ToString()
            : $"{MandoEraResolver.DisplayYear(era.Value, date.Year)} {era.Value.ShortCode}";

        if (date.Month is null) return eraSuffix;

        string monthName = months.FirstOrDefault(m => m.MonthIndex == date.Month.Value)?.Name
            ?? $"Month {date.Month}";

        if (date.Day is null) return $"{monthName}, {eraSuffix}";

        return $"{date.Day} {monthName}, {eraSuffix}";
    }

    public async Task<ApiResponse<bool>> RenameMonthAsync(RenameMonthRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest<bool>("Month name is required.");

        MandoMonth? month = await context.MandoMonths
            .SingleOrDefaultAsync(m => m.MonthIndex == request.MonthIndex);
        if (month is null) return NotFound<bool>();

        month.Name = request.Name.Trim();
        await context.SaveChangesAsync();
        InvalidateCache();

        return Ok(true);
    }

    public async Task<ApiResponse<MandoEraDto>> CreateEraAsync(CreateEraRequest request)
    {
        ApiResponse<bool> validation = await ValidateEraAsync(request.Name, request.ShortCode, existingId: null);
        if (!validation.Success) return BadRequest<MandoEraDto>(validation.Error ?? "Invalid era.");

        MandoEra era = new()
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            ShortCode = request.ShortCode.Trim(),
            AnchorYear = request.AnchorYear,
            Direction = request.Direction,
            SortOrder = request.SortOrder,
        };

        context.MandoEras.Add(era);
        await context.SaveChangesAsync();
        InvalidateCache();

        return Ok(MapToDto(era));
    }

    public async Task<ApiResponse<MandoEraDto>> UpdateEraAsync(UpdateEraRequest request)
    {
        MandoEra? era = await context.MandoEras.SingleOrDefaultAsync(e => e.Id == request.Id);
        if (era is null) return NotFound<MandoEraDto>();

        ApiResponse<bool> validation = await ValidateEraAsync(request.Name, request.ShortCode, existingId: era.Id);
        if (!validation.Success) return BadRequest<MandoEraDto>(validation.Error ?? "Invalid era.");

        era.Name = request.Name.Trim();
        era.ShortCode = request.ShortCode.Trim();
        era.AnchorYear = request.AnchorYear;
        era.Direction = request.Direction;
        era.SortOrder = request.SortOrder;

        await context.SaveChangesAsync();
        InvalidateCache();

        return Ok(MapToDto(era));
    }

    public async Task<ApiResponse<bool>> DeleteEraAsync(Guid id)
    {
        MandoEra? era = await context.MandoEras.SingleOrDefaultAsync(e => e.Id == id);
        if (era is null) return NotFound<bool>();

        context.MandoEras.Remove(era);
        await context.SaveChangesAsync();
        InvalidateCache();

        return Ok(true);
    }

    private async Task<ApiResponse<bool>> ValidateEraAsync(string name, string shortCode, Guid? existingId)
    {
        if (string.IsNullOrWhiteSpace(name)) return BadRequest<bool>("Name is required.");
        if (string.IsNullOrWhiteSpace(shortCode)) return BadRequest<bool>("Short code is required.");
        if (shortCode.Length > 8) return BadRequest<bool>("Short code must be 8 characters or fewer.");

        bool clash = await context.MandoEras
            .AnyAsync(e => e.ShortCode == shortCode.Trim() && (existingId == null || e.Id != existingId));
        if (clash) return BadRequest<bool>($"An era with the short code '{shortCode}' already exists.");

        return Ok(true);
    }

    private void InvalidateCache()
    {
        cache.Remove(MonthsCacheKey);
        cache.Remove(ErasCacheKey);
    }
}
