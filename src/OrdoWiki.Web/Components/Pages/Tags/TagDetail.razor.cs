namespace OrdoWiki.Web.Components.Pages.Tags;

using Microsoft.AspNetCore.Components;
using MudBlazor;

public partial class TagDetail
{
    private const int Preview = 6;

    private TagDto? _tag;
    private bool _loading = true;

    private List<WikiPageDto> _pages = [];
    private List<CharacterDto> _characters = [];
    private List<TimelineEventDto> _events = [];
    private List<GalleryItemDto> _images = [];

    private int _pageTotal;
    private int _characterTotal;
    private int _eventTotal;
    private int _imageTotal;
    private int _totalUses;

    [Parameter, EditorRequired]
    public required string Slug { get; set; }

    [Inject]
    private ITagService TagService { get; set; } = null!;

    [Inject]
    private IPageService PageService { get; set; } = null!;

    [Inject]
    private ICharacterService CharacterService { get; set; } = null!;

    [Inject]
    private ITimelineService TimelineService { get; set; } = null!;

    [Inject]
    private IGalleryService GalleryService { get; set; } = null!;

    protected override async Task OnParametersSetAsync()
    {
        if (!RendererInfo.IsInteractive) return;

        _loading = true;
        _tag = await TagService.GetBySlugAsync(Slug);
        if (_tag is null)
        {
            _loading = false;
            return;
        }

        ApiResponse<List<WikiPageDto>> pages = await PageService.GetPagesAsync(_tag.Id);
        if (pages.Success)
        {
            _pageTotal = pages.Value.Count;
            _pages = pages.Value.Take(Preview).ToList();
        }

        ApiResponse<List<CharacterDto>> chars = await CharacterService.GetCharactersAsync(_tag.Id);
        if (chars.Success)
        {
            _characterTotal = chars.Value.Count;
            _characters = chars.Value.Take(Preview).ToList();
        }

        ApiResponse<PagedResult<TimelineEventDto>> events = await TimelineService.GetEventsAsync(new TimelineEventFilter
        {
            TagId = _tag.Id,
            Page = 1,
            PageSize = Preview,
            Descending = true,
        });
        if (events.Success)
        {
            _eventTotal = events.Value.TotalCount;
            _events = events.Value.Items.ToList();
        }

        ApiResponse<PagedResult<GalleryItemDto>> images = await GalleryService.GetGalleryAsync(new GalleryFilter
        {
            TagId = _tag.Id,
            Page = 1,
            PageSize = Preview,
        });
        if (images.Success)
        {
            _imageTotal = images.Value.TotalCount;
            _images = images.Value.Items.ToList();
        }

        _totalUses = _pageTotal + _characterTotal + _eventTotal + _imageTotal;
        _loading = false;
    }
}
