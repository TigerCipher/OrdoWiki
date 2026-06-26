namespace OrdoWiki.Web.Models;

public class UserDto
{
    public required string Id { get; set; }
    public required string Username { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarPath { get; set; }
    public string? Role { get; set; }
    public bool IsPasswordResetRequired { get; set; }
    public bool IsGhost { get; set; }
}