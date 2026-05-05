namespace OrdoWiki.Web.Components.Shared.Dialogs;

using Microsoft.AspNetCore.Components;
using MudBlazor;

public partial class ImageLightboxDialog
{
    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter, EditorRequired]
    public required string Src { get; set; }

    [Parameter]
    public string Alt { get; set; } = string.Empty;
}
