namespace OrdoWiki.Data.Calendars;

/// <summary>
/// Lightweight value type passed to <see cref="MandoEraResolver"/>. Decouples the
/// pure era logic from any EF entity / DTO so the resolver stays unit-testable
/// without a database.
/// </summary>
public readonly record struct MandoEraInfo(
    string Name,
    string ShortCode,
    int AnchorYear,
    EraDirection Direction);
