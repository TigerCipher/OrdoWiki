namespace OrdoWiki.Web.Services.Contract;

using Data.Calendars;
using Models;
using Models.Requests;

public interface IMandoCalendarService
{
    Task<IReadOnlyList<MandoMonthDto>> GetMonthsAsync();
    Task<IReadOnlyList<MandoEraDto>> GetErasAsync();

    /// <summary>Format a date as e.g. "12 Vhett'yc, 47 ACW".</summary>
    Task<string> FormatAsync(MandoDate date);

    Task<ApiResponse<bool>> RenameMonthAsync(RenameMonthRequest request);
    Task<ApiResponse<MandoEraDto>> CreateEraAsync(CreateEraRequest request);
    Task<ApiResponse<MandoEraDto>> UpdateEraAsync(UpdateEraRequest request);
    Task<ApiResponse<bool>> DeleteEraAsync(Guid id);
}
