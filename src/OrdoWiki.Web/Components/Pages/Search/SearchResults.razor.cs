namespace OrdoWiki.Web.Components.Pages.Search;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

public partial class SearchResults
{
    private SearchResultsDto _results = new();
    private string _query = string.Empty;
    private string _input = string.Empty;
    private bool _loading;

    [SupplyParameterFromQuery(Name = "q")]
    public string? Q { get; set; }

    [Inject]
    private ISearchService SearchService { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    protected override async Task OnParametersSetAsync()
    {
        string incoming = (Q ?? string.Empty).Trim();
        if (incoming == _query) return;

        _query = incoming;
        _input = incoming;
        if (string.IsNullOrWhiteSpace(_query))
        {
            _results = new SearchResultsDto();
            return;
        }

        _loading = true;
        _results = await SearchService.SearchAsync(_query, limit: 50);
        _loading = false;
    }

    private void OnInputChanged(string value) => _input = value ?? string.Empty;

    private void OnInputKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter") RunSearch(_input);
    }

    private Task RunSearchAsync(string value)
    {
        RunSearch(value);
        return Task.CompletedTask;
    }

    private void RunSearch(string value)
    {
        string normalized = (value ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(normalized))
        {
            Navigation.NavigateTo("/search");
            return;
        }

        Navigation.NavigateTo($"/search?q={Uri.EscapeDataString(normalized)}");
    }
}
