namespace OrdoWiki.Web.Components.Shared;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using Web.Models.Requests;

public partial class CharacterGalleryEditor
{
    private const long MaxImageBytes = 10L * 1024 * 1024;

    private readonly string _inputId = $"gallery-{Guid.NewGuid():N}";
    private bool _uploading;
    private int? _dragSourceIndex;
    private int? _dragOverIndex;

    [Parameter, EditorRequired]
    public required Guid CharacterId { get; set; }

    [Parameter, EditorRequired]
    public required List<CharacterImageDto> Images { get; set; }

    [Parameter]
    public int? MaxImages { get; set; }

    [Parameter]
    public EventCallback OnChanged { get; set; }

    [Inject]
    private ICharacterService CharacterService { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    private bool _atCap => MaxImages.HasValue && Images.Count >= MaxImages.Value;

    private async Task OnFilesPickedAsync(InputFileChangeEventArgs e)
    {
        if (e.FileCount == 0) return;

        _uploading = true;
        StateHasChanged();
        try
        {
            foreach (IBrowserFile file in e.GetMultipleFiles(maximumFileCount: 20))
            {
                if (_atCap)
                {
                    Snackbar.Add($"Image limit of {MaxImages} reached.", Severity.Warning);
                    break;
                }

                if (file.Size > MaxImageBytes)
                {
                    Snackbar.Add($"'{file.Name}' is larger than the {MaxImageBytes / (1024 * 1024)} MB limit.", Severity.Warning);
                    continue;
                }

                await using Stream stream = file.OpenReadStream(MaxImageBytes);
                ApiResponse<CharacterImageDto> response = await CharacterService.AttachImageAsync(
                    CharacterId, stream, file.Name, file.ContentType, file.Size);

                if (!response)
                {
                    Snackbar.Add($"Failed to upload '{file.Name}': {response.Error}", Severity.Error);
                    continue;
                }

                Images.Add(response.Value);
            }

            await OnChanged.InvokeAsync();
        }
        finally
        {
            _uploading = false;
            StateHasChanged();
        }
    }

    private async Task DeleteAsync(CharacterImageDto image)
    {
        ApiResponse<bool> response = await CharacterService.RemoveImageAsync(image.Id);
        if (!response.Success)
        {
            Snackbar.Add($"Failed to delete image: {response.Error}", Severity.Error);
            return;
        }

        Images.Remove(image);
        await OnChanged.InvokeAsync();
    }

    private async Task OnCaptionChangedAsync(CharacterImageDto image, string newValue)
    {
        string? trimmed = string.IsNullOrWhiteSpace(newValue) ? null : newValue.Trim();
        if (trimmed == image.Caption) return;

        ApiResponse<CharacterImageDto> response = await CharacterService.UpdateImageCaptionAsync(
            new UpdateImageCaptionRequest { ImageId = image.Id, Caption = trimmed });

        if (!response)
        {
            Snackbar.Add($"Failed to save caption: {response.Error}", Severity.Error);
            return;
        }

        image.Caption = response.Value.Caption;
    }

    private void OnDragStart(int index)
    {
        _dragSourceIndex = index;
        _dragOverIndex = null;
    }

    private void OnDragEnter(int index)
    {
        if (_dragSourceIndex is null || _dragSourceIndex == index) return;
        _dragOverIndex = index;
    }

    private void OnDragLeave() => _dragOverIndex = null;

    private async Task OnDropAsync(int targetIndex)
    {
        int? source = _dragSourceIndex;
        _dragSourceIndex = null;
        _dragOverIndex = null;

        if (source is null || source == targetIndex) return;

        CharacterImageDto moved = Images[source.Value];
        Images.RemoveAt(source.Value);
        Images.Insert(targetIndex, moved);

        for (int i = 0; i < Images.Count; i++)
            Images[i].OrderIndex = i;

        ReorderCharacterImagesRequest request = new()
        {
            CharacterId = CharacterId,
            Order = Images
                .Select(img => new CharacterImageOrder { ImageId = img.Id, OrderIndex = img.OrderIndex })
                .ToList(),
        };

        ApiResponse<bool> response = await CharacterService.ReorderImagesAsync(request);
        if (!response.Success)
        {
            Snackbar.Add($"Failed to save new order: {response.Error}", Severity.Error);
            return;
        }

        await OnChanged.InvokeAsync();
    }
}
