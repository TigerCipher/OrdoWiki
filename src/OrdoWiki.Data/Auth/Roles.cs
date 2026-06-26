namespace OrdoWiki.Data.Auth;

public static class Roles
{
    public const string Admin = nameof(Admin);
    public const string Designer = nameof(Designer);
    public const string Editor = nameof(Editor);
    public const string Reader = nameof(Reader);

    public static readonly string[] All = [Admin, Designer, Editor, Reader];

    // Highest privilege first.
    public static readonly string[] ByPriority = [Admin, Designer, Editor, Reader];

    public static string? PickHighest(IEnumerable<string?> roles)
    {
        HashSet<string> set = new(
            roles.Where(r => !string.IsNullOrWhiteSpace(r)).Select(r => r!),
            StringComparer.OrdinalIgnoreCase);

        foreach (string role in ByPriority)
        {
            if (set.Contains(role)) return role;
        }
        return null;
    }
}

public static class Policies
{
    public const string CanEdit = nameof(CanEdit);
    public const string CanDesign = nameof(CanDesign);
    public const string IsAdmin = nameof(IsAdmin);
}