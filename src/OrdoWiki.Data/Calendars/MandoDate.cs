namespace OrdoWiki.Data.Calendars;

/// <summary>
/// A date in the in-world calendar. <see cref="Year"/> is signed: zero and positive
/// values fall in the post-anchor era (e.g. ACW), negative values in the pre-anchor
/// era (e.g. BCW). A null <see cref="Month"/> or <see cref="Day"/> models a fuzzy
/// date — these sort to the start of the year/month for ordering purposes.
/// </summary>
public readonly record struct MandoDate(int Year, int? Month = null, int? Day = null);
