namespace OrdoWiki.Data.Auth;

public static class Roles
{
    public const string Admin = nameof(Admin);
    public const string Designer = nameof(Designer);
    public const string Editor = nameof(Editor);
    public const string Reader = nameof(Reader);

    public static readonly string[] All = [Admin, Designer, Editor, Reader];
}

public static class Policies
{
    public const string CanEdit = nameof(CanEdit);
    public const string CanDesign = nameof(CanDesign);
}