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
    private IReadOnlyList<TagDto> _allTags = [];
    private UserDto? _selectedUploader;
    private TagDto? _selectedTag;
    private bool _loading = true;
    private int _pageCount = 1;

    [SupplyParameterFromQuery(Name = "tag")]
    public string? TagSlug { get; set; }

    [Inject]
    private IGalleryService GalleryService { get; set; } = null!;

    [Inject]
    private ITagService TagService { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private IDialogService DialogService { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        if (!RendererInfo.IsInteractive) return;

        ApiResponse<List<UserDto>> uploadersResponse = await GalleryService.GetUploadersAsync();
        if (uploadersResponse) _uploaders = uploadersResponse.Value;

        _allTags = await TagService.GetAllAsync();
        _selectedTag = string.IsNullOrEmpty(TagSlug)
            ? null
            : _allTags.FirstOrDefault(t => string.Equals(t.Slug, TagSlug, StringComparison.OrdinalIgnoreCase));
        _filter.TagId = _selectedTag?.Id;

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

    private async Task OnTagFilterChangedAsync(TagDto? tag)
    {
        _selectedTag = tag;
        _filter.TagId = tag?.Id;
        _filter.Page = 1;
        string url = tag is null
            ? Navigation.GetUriWithQueryParameter("tag", (string?)null)
            : Navigation.GetUriWithQueryParameter("tag", tag.Slug);
        Navigation.NavigateTo(url, replace: true);
        await LoadAsync();
    }

    private async Task EditTagsAsync(GalleryItemDto item)
    {
        DialogParameters parameters = new()
        {
            { nameof(EditMediaTagsDialog.AssetId), item.Asset.Id },
            { nameof(EditMediaTagsDialog.ImageUrl), item.Asset.StoragePath },
            { nameof(EditMediaTagsDialog.Caption), item.Asset.OriginalName },
            { nameof(EditMediaTagsDialog.InitialTags), (IReadOnlyList<string>)item.Tags.Select(t => t.Name).ToList() },
        };

        DialogOptions options = new()
        {
            CloseButton = true,
            BackdropClick = false,
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
        };

        IDialogReference dialog = await DialogService.ShowAsync<EditMediaTagsDialog>("Edit tags", parameters, options);
        DialogResult? result = await dialog.Result;

        if (result is { Canceled: false })
        {
            // Refresh tags list (new tags may have been created) and reload page
            // so chip rows reflect updated tagging.
            _allTags = await TagService.GetAllAsync();
            await LoadAsync();
        }
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

    private async Task DeleteAsync(GalleryItemDto item)
    {
        // Attached images live on a page or character — bounce the user to that
        // page's editor instead of deleting the asset out from under it.
        if (item.Source is { } src)
        {
            bool? goEdit = await DialogService.ShowMessageBoxAsync(
                $"Image attached to {src.Kind.ToLowerInvariant()}",
                $"This image belongs to the {src.Kind.ToLowerInvariant()} \"{src.Name}\" and can only be removed by editing that page.",
                yesText: $"Edit {src.Name}",
                cancelText: "Close");

            if (goEdit == true)
                Navigation.NavigateTo($"{src.Url}/edit");
            return;
        }

        bool? confirm = await DialogService.ShowMessageBoxAsync(
            "Delete image",
            "Permanently delete this image? This cannot be undone.",
            yesText: "Delete",
            cancelText: "Cancel");

        if (confirm != true) return;

        ApiResponse<bool> response = await GalleryService.DeleteStandaloneAsync(item.Asset.Id);
        if (!response.Success)
        {
            Snackbar.Add($"Failed to delete: {response.Error}", Severity.Error);
            return;
        }

        Snackbar.Add("Image deleted", Severity.Success);
        await LoadAsync();
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
