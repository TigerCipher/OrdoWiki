namespace OrdoWiki.Web.Components.Shared;

using Data.Auth;
using Microsoft.AspNetCore.Components;
using MudBlazor;

public partial class AuthorName
{
    private string _initials = "?";
    private string _displayName = "Unknown";
    private string? _avatarPath;
    private bool _showRole;
    private Color _roleColor = Color.Default;
    
    [Parameter]
    public string Class { get; set; } = string.Empty;

    [Parameter]
    public UserDto? User { get; set; }

    [Parameter]
    public Size AvatarSize { get; set; } = Size.Small;

    [Parameter]
    public Size ChipSize { get; set; } = Size.Small;

    protected override void OnParametersSet()
    {
        if (User is null)
        {
            _initials = "?";
            _displayName = "Unknown";
            _avatarPath = null;
            _showRole = false;
            return;
        }

        _displayName = !string.IsNullOrWhiteSpace(User.DisplayName)
            ? User.DisplayName
            : User.Username;

        _initials = ComputeInitials(_displayName);
        _avatarPath = User.AvatarPath;

        _showRole = !string.IsNullOrWhiteSpace(User.Role)
                    && !string.Equals(User.Role, Roles.Reader, StringComparison.OrdinalIgnoreCase);

        _roleColor = User.Role switch
        {
            Roles.Admin => Color.Error,
            Roles.Designer => Color.Secondary,
            Roles.Editor => Color.Primary,
            _ => Color.Default
        };
    }

    private static string ComputeInitials(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "?";

        string[] parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
            return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();

        return name[0].ToString().ToUpperInvariant();
    }
}
