namespace OrdoWiki.Web.Components.Shared.Dialogs;

using Microsoft.AspNetCore.Components;
using MudBlazor;

public partial class EditMediaTagsDialog
{
    private List<string> _tagNames = [];
    private bool _saving;

    [CascadingParameter]
    private IMudDialogInstance Dialog { get; set; } = null!;

    [Parameter, EditorRequired]
    public required Guid AssetId { get; set; }

    [Parameter]
    public string? ImageUrl { get; set; }

    [Parameter]
    public string? Caption { get; set; }

    [Parameter]
    public IReadOnlyList<string> InitialTags { get; set; } = [];

    [Inject]
    private ITagService TagService { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    protected override void OnInitialized()
    {
        _tagNames = InitialTags.ToList();
    }

    private void OnTagsChanged(IReadOnlyList<string> tags) => _tagNames = tags.ToList();

    private async Task SaveAsync()
    {
        _saving = true;
        ApiResponse<bool> response = await TagService.SetTagsAsync(TagTarget.MediaAsset, AssetId, _tagNames);
        _saving = false;

        if (!response.Success)
        {
            Snackbar.Add($"Failed to save tags: {response.Error}", Severity.Error);
            return;
        }

        Dialog.Close(DialogResult.Ok(_tagNames));
    }

    private void Cancel() => Dialog.Cancel();
}
