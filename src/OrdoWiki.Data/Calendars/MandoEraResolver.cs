namespace OrdoWiki.Data.Calendars;

/// <summary>
/// Pure logic for mapping signed absolute years to (and from) era + displayed-year
/// pairs. Eras are configured at runtime, but the math is deterministic and lives
/// here so it can be tested without a database.
/// </summary>
/// <remarks>
/// <para>Forward era at AnchorYear=A applies to absoluteYear &gt;= A and
/// &lt; (next forward era's anchor). Display year = absoluteYear − A.</para>
/// <para>Backward era at AnchorYear=A applies to absoluteYear &lt; A and
/// &gt;= (the next backward era anchor below, if any). Display year = A − absoluteYear.</para>
/// </remarks>
public static class MandoEraResolver
{
    /// <summary>
    /// Find the era that contains <paramref name="absoluteYear"/>. Returns null
    /// if no configured era covers it.
    /// </summary>
    public static MandoEraInfo? Resolve(IReadOnlyList<MandoEraInfo> eras, int absoluteYear)
    {
        // Forward: pick the era with the largest anchor <= absoluteYear.
        MandoEraInfo? forwardMatch = null;
        foreach (MandoEraInfo era in eras)
        {
            if (era.Direction != EraDirection.Forward) continue;
            if (era.AnchorYear > absoluteYear) continue;
            if (forwardMatch is null || era.AnchorYear > forwardMatch.Value.AnchorYear)
                forwardMatch = era;
        }

        // Backward: pick the era with the smallest anchor > absoluteYear.
        MandoEraInfo? backwardMatch = null;
        foreach (MandoEraInfo era in eras)
        {
            if (era.Direction != EraDirection.Backward) continue;
            if (era.AnchorYear <= absoluteYear) continue;
            if (backwardMatch is null || era.AnchorYear < backwardMatch.Value.AnchorYear)
                backwardMatch = era;
        }

        // Both could match in principle (overlapping configuration). Forward wins
        // for years inside its bounds; backward only fills the gap below.
        return forwardMatch ?? backwardMatch;
    }

    public static int DisplayYear(MandoEraInfo era, int absoluteYear) =>
        era.Direction == EraDirection.Forward
            ? absoluteYear - era.AnchorYear
            : era.AnchorYear - absoluteYear;

    public static int ToAbsoluteYear(MandoEraInfo era, int displayYear) =>
        era.Direction == EraDirection.Forward
            ? era.AnchorYear + displayYear
            : era.AnchorYear - displayYear;
}
