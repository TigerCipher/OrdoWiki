namespace OrdoWiki.Web.Services.Contract;

using Models;
using Models.Requests;

public interface ISiteThemeService
{
    /// <summary>The current live theme. Always returns a non-null DTO (defaults to empty overrides).</summary>
    Task<SiteThemeDto> GetAsync();

    Task<IReadOnlyList<CustomThemeVariableDto>> GetCustomVariablesAsync();

    /// <summary>Persist a theme update. Designer (CanDesign) only.</summary>
    Task<ApiResponse<SiteThemeDto>> SaveAsync(SaveSiteThemeRequest request);

    /// <summary>Add a custom CSS variable to the registry. Admin only.</summary>
    Task<ApiResponse<CustomThemeVariableDto>> CreateCustomVariableAsync(CreateCustomVariableRequest request);

    Task<ApiResponse<CustomThemeVariableDto>> UpdateCustomVariableAsync(UpdateCustomVariableRequest request);

    Task<ApiResponse<bool>> DeleteCustomVariableAsync(Guid id);
}
