namespace OrdoWiki.Web.Components.Shared;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

public partial class NavSearch : IDisposable
{
    private const int DebounceMs = 200;
    private const int MinChars = 2;
    private const int SuggestionLimit = 5;

    private MudTextField<string>? _field;
    private string _input = string.Empty;
    private SearchResultsDto _results = new();
    private bool _open;
    private bool _searching;
    private CancellationTokenSource? _debounceCts;
    private CancellationTokenSource? _searchCts;

    [Inject]
    private ISearchService SearchService { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    private async Task OnInputChanged(string value)
    {
        _input = value ?? string.Empty;

        // Restart the debounce window. The previous wakeup is cancelled, so only
        // the final keystroke in a burst issues a query.
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        CancellationToken token = _debounceCts.Token;

        if (_input.Trim().Length < MinChars)
        {
            _open = false;
            _results = new SearchResultsDto();
            StateHasChanged();
            return;
        }

        try
        {
            await Task.Delay(DebounceMs, token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        await RunSearchAsync(_input);
    }

    private async Task RunSearchAsync(string text)
    {
        string trimmed = (text ?? string.Empty).Trim();
        if (trimmed.Length < MinChars) return;

        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        CancellationToken token = _searchCts.Token;

        _searching = true;
        _open = true;
        StateHasChanged();

        try
        {
            SearchResultsDto results = await SearchService.SearchAsync(trimmed, SuggestionLimit, token);
            if (token.IsCancellationRequested) return;
            _results = results;
        }
        catch (OperationCanceledException)
        {
            return;
        }
        finally
        {
            if (!token.IsCancellationRequested)
            {
                _searching = false;
                StateHasChanged();
            }
        }
    }

    private async Task OnKeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            GoToResults();
        }
        else if (e.Key == "Escape")
        {
            _open = false;
            await Task.CompletedTask;
            StateHasChanged();
        }
    }

    private async Task OnBlur(FocusEventArgs e)
    {
        // Defer so a click on a suggestion item still registers before the popover closes.
        await Task.Delay(150);
        _open = false;
        StateHasChanged();
    }

    private void NavigateTo(string href)
    {
        _open = false;
        Navigation.NavigateTo(href);
    }

    private void GoToResults()
    {
        string trimmed = _input.Trim();
        if (trimmed.Length < MinChars) return;
        _open = false;
        Navigation.NavigateTo($"/search?q={Uri.EscapeDataString(trimmed)}");
    }

    public void Dispose()
    {
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _searchCts?.Cancel();
        _searchCts?.Dispose();
    }
}
