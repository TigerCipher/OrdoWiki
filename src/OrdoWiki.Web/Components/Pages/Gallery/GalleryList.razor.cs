namespace OrdoWiki.Web.Components.Pages.Gallery;

using Data.Entities;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using OrdoWiki.Web.Components.Shared.Dialogs;

public partial class GalleryList
{
    private readonly GalleryFilter _filter = new() { PageSize = 24 };
    private List<GalleryItemDto> _items = [];
    private List<UserDto> _uploaders = [];
    private UserDto? _selectedUploader;
    private bool _loading = true;
    private int _pageCount = 1;

    [Inject]
    private IGalleryService GalleryService { get; set; } = null!;

    [Inject]
    private IDialogService DialogService { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        if (!RendererInfo.IsInteractive) return;

        ApiResponse<List<UserDto>> uploadersResponse = await GalleryService.GetUploadersAsync();
        if (uploadersResponse) _uploaders = uploadersResponse.Value;

        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _loading = true;
        ApiResponse<PagedResult<GalleryItemDto>> response = await GalleryService.GetGalleryAsync(_filter);
        _loading = false;

        if (!response)
        {
            Snackbar.Add($"Failed to load gallery: {response.Error}", Severity.Error);
            return;
        }

        PagedResult<GalleryItemDto> page = response;
        _items = page.Items.ToList();
        _pageCount = Math.Max(1, page.TotalPages);
    }

    private async Task OnSourceFilterChanged(MediaSourceType? source)
    {
        _filter.SourceType = source;
        _filter.Page = 1;
        await LoadAsync();
    }

    private async Task OnUploaderChanged(UserDto? user)
    {
        _selectedUploader = user;
        _filter.UploaderId = user?.Id;
        _filter.Page = 1;
        await LoadAsync();
    }

    private async Task OnPageChanged(int page)
    {
        _filter.Page = page;
        await LoadAsync();
    }

    private Task<IEnumerable<UserDto>> SearchUploadersAsync(string? value, CancellationToken cancellationToken)
    {
        IEnumerable<UserDto> matches = string.IsNullOrWhiteSpace(value)
            ? _uploaders
            : _uploaders.Where(u =>
                (u.DisplayName?.Contains(value, StringComparison.OrdinalIgnoreCase) ?? false)
                || u.Username.Contains(value, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(matches);
    }

    private async Task OpenLightboxAsync(GalleryItemDto item)
    {
        DialogParameters parameters = new()
        {
            { nameof(ImageLightboxDialog.Src), item.Asset.StoragePath },
            { nameof(ImageLightboxDialog.Alt), item.Asset.OriginalName }
        };

        DialogOptions options = new()
        {
            CloseButton = true,
            BackdropClick = true,
            MaxWidth = MaxWidth.False,
        };

        await DialogService.ShowAsync<ImageLightboxDialog>(string.Empty, parameters, options);
    }

    private async Task OpenUploadAsync()
    {
        DialogOptions options = new()
        {
            CloseButton = true,
            BackdropClick = false,
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
        };

        IDialogReference dialog = await DialogService.ShowAsync<UploadImageDialog>("Upload image", options);
        DialogResult? result = await dialog.Result;

        if (result is { Canceled: false, Data: ImageInsertResult })
        {
            // The dialog uploads with default Standalone source, then returns the URL.
            // Reload uploaders + first page so the new tile appears at the top.
            ApiResponse<List<UserDto>> uploadersResponse = await GalleryService.GetUploadersAsync();
            if (uploadersResponse) _uploaders = uploadersResponse.Value;

            _filter.Page = 1;
            await LoadAsync();
        }
    }
}
