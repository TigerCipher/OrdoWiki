namespace OrdoWiki.Web.Components.Pages.Admin;

using Data.Calendars;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Web.Models.Requests;

public partial class EraEditDialog
{
    private string _name = string.Empty;
    private string _shortCode = string.Empty;
    private int _anchorYear;
    private EraDirection _direction = EraDirection.Forward;
    private int _sortOrder;

    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    public MandoEraDto? Existing { get; set; }

    protected override void OnInitialized()
    {
        if (Existing is null) return;

        _name = Existing.Name;
        _shortCode = Existing.ShortCode;
        _anchorYear = Existing.AnchorYear;
        _direction = Existing.Direction;
        _sortOrder = Existing.SortOrder;
    }

    private void Save()
    {
        if (Existing is null)
        {
            MudDialog.Close(DialogResult.Ok(new CreateEraRequest
            {
                Name = _name,
                ShortCode = _shortCode,
                AnchorYear = _anchorYear,
                Direction = _direction,
                SortOrder = _sortOrder,
            }));
        }
        else
        {
            MudDialog.Close(DialogResult.Ok(new UpdateEraRequest
            {
                Id = Existing.Id,
                Name = _name,
                ShortCode = _shortCode,
                AnchorYear = _anchorYear,
                Direction = _direction,
                SortOrder = _sortOrder,
            }));
        }
    }

    private void Cancel() => MudDialog.Cancel();
}
