namespace OrdoWiki.Web.Components.Pages.Wiki;

using Microsoft.AspNetCore.Components;
using MudBlazor;
using Shared.Dialogs;

public partial class Page
{

    private List<WikiPageDto> _pages = [];
    private int _rowsPerPage = 10;
    private bool _loading = true;
    
    [Inject]
    private IPageService PageService { get; set; } = null!;

    [Inject]
    private IOrdoDialogs OrdoDialogs { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        if (!RendererInfo.IsInteractive) return;
        
        ApiResponse<List<WikiPageDto>> response = await PageService.GetPagesAsync();
        _loading = false;

        if (!response)
        {
            IDialogReference dialog = await OrdoDialogs.ShowErrorAsync($"Failed to load pages - {response.Error}");
            await dialog.Result;
            Navigation.NavigateTo("/");
            return;
        }
        
        _pages = response;
    }

    private void OpenPage(DataGridRowClickEventArgs<WikiPageDto> args)
    {
        Navigation.NavigateTo($"/logs/{args.Item.Slug}");
    }

    private void CreatePage()
    {
        Navigation.NavigateTo("/logs/new");
    }

    private void EditPage(WikiPageDto item)
    {
        Navigation.NavigateTo($"/logs/{item.Slug}/edit");
    }
}