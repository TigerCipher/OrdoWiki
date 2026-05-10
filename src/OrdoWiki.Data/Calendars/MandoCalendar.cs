namespace OrdoWiki.Data.Calendars;

/// <summary>
/// Pure calendar math. Holds no names, no era logic — those live in
/// <c>IMandoCalendarService</c> because they're admin-editable. This class is
/// the one piece of the project that genuinely benefits from unit tests, so
/// keep it free of I/O and DB lookups.
/// </summary>
public static class MandoCalendar
{
    public const int DaysPerMonth = 30;
    public const int MonthsPerYear = 12;
    public const int DaysPerYear = DaysPerMonth * MonthsPerYear;

    public static long ToEpochDay(MandoDate date)
    {
        int month = date.Month ?? 1;
        int day = date.Day ?? 1;
        return ((long)date.Year * DaysPerYear)
             + ((month - 1) * DaysPerMonth)
             + (day - 1);
    }

    public static MandoDate FromEpochDay(long epochDay)
    {
        // Floored division so negative epoch days fall into the right year:
        // -1 must map to year -1 (last day), not year 0.
        long year = epochDay >= 0
            ? epochDay / DaysPerYear
            : (epochDay - DaysPerYear + 1) / DaysPerYear;

        long dayOfYear = epochDay - (year * DaysPerYear);
        int month = (int)(dayOfYear / DaysPerMonth) + 1;
        int day = (int)(dayOfYear % DaysPerMonth) + 1;

        return new MandoDate((int)year, month, day);
    }
}
