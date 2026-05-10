namespace OrdoWiki.Web.Components.Pages.Admin;

using Microsoft.AspNetCore.Components;
using MudBlazor;
using Web.Models.Requests;

public partial class CalendarTab
{
    private List<MandoMonthDto> _months = [];
    private List<MandoEraDto> _eras = [];
    private bool _loading = true;

    [Inject]
    private IMandoCalendarService Calendar { get; set; } = null!;

    [Inject]
    private IDialogService Dialog { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _loading = true;
        _months = (await Calendar.GetMonthsAsync()).ToList();
        _eras = (await Calendar.GetErasAsync()).ToList();
        _loading = false;
        StateHasChanged();
    }

    private async Task RenameMonthAsync(MandoMonthDto month)
    {
        ApiResponse<bool> response = await Calendar.RenameMonthAsync(new RenameMonthRequest
        {
            MonthIndex = month.MonthIndex,
            Name = month.Name,
        });

        if (!response.Success)
        {
            Snackbar.Add($"Failed to rename: {response.Error}", Severity.Error);
            return;
        }

        Snackbar.Add($"Saved month {month.MonthIndex}.", Severity.Success);
    }

    private async Task OpenCreateEraAsync()
    {
        IDialogReference dialog = await Dialog.ShowAsync<EraEditDialog>("New era");
        DialogResult? result = await dialog.Result;

        if (result is { Canceled: false, Data: CreateEraRequest req })
        {
            ApiResponse<MandoEraDto> response = await Calendar.CreateEraAsync(req);
            if (!response.Success)
            {
                Snackbar.Add($"Failed to create era: {response.Error}", Severity.Error);
                return;
            }

            await LoadAsync();
            Snackbar.Add($"Era '{response.Value.ShortCode}' created.", Severity.Success);
        }
    }

    private async Task OpenEditEraAsync(MandoEraDto era)
    {
        DialogParameters parameters = new()
        {
            { nameof(EraEditDialog.Existing), era },
        };

        IDialogReference dialog = await Dialog.ShowAsync<EraEditDialog>("Edit era", parameters);
        DialogResult? result = await dialog.Result;

        if (result is { Canceled: false, Data: UpdateEraRequest req })
        {
            ApiResponse<MandoEraDto> response = await Calendar.UpdateEraAsync(req);
            if (!response.Success)
            {
                Snackbar.Add($"Failed to update era: {response.Error}", Severity.Error);
                return;
            }

            await LoadAsync();
            Snackbar.Add($"Era '{response.Value.ShortCode}' updated.", Severity.Success);
        }
    }

    private async Task DeleteEraAsync(MandoEraDto era)
    {
        bool? confirm = await Dialog.ShowMessageBoxAsync(
            "Delete era",
            $"Delete the '{era.Name}' ({era.ShortCode}) era? Existing timeline events will display as raw years until another era covers them.",
            "Delete", cancelText: "Cancel");

        if (confirm != true) return;

        ApiResponse<bool> response = await Calendar.DeleteEraAsync(era.Id);
        if (!response.Success)
        {
            Snackbar.Add($"Failed to delete: {response.Error}", Severity.Error);
            return;
        }

        await LoadAsync();
        Snackbar.Add($"Era '{era.ShortCode}' deleted.", Severity.Success);
    }
}
