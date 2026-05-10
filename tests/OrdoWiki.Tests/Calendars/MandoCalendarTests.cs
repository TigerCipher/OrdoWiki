namespace OrdoWiki.Tests.Calendars;

using OrdoWiki.Data.Calendars;

public class MandoCalendarTests
{
    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(0, 12, 30)]
    [InlineData(47, 7, 15)]
    [InlineData(-1, 12, 30)]
    [InlineData(-47, 1, 1)]
    [InlineData(1000, 6, 6)]
    public void RoundTrip_FullySpecified(int year, int month, int day)
    {
        MandoDate original = new(year, month, day);
        long epoch = MandoCalendar.ToEpochDay(original);
        MandoDate roundTripped = MandoCalendar.FromEpochDay(epoch);

        Assert.Equal(original, roundTripped);
    }

    [Fact]
    public void EpochDay_Zero_IsYearZeroDayOne()
    {
        MandoDate date = MandoCalendar.FromEpochDay(0);
        Assert.Equal(new MandoDate(0, 1, 1), date);
    }

    [Fact]
    public void EpochDay_NegativeOne_IsLastDayOfYearMinusOne()
    {
        MandoDate date = MandoCalendar.FromEpochDay(-1);
        Assert.Equal(new MandoDate(-1, 12, 30), date);
    }

    [Fact]
    public void EpochDay_360_IsFirstDayOfYearOne()
    {
        MandoDate date = MandoCalendar.FromEpochDay(360);
        Assert.Equal(new MandoDate(1, 1, 1), date);
    }

    [Fact]
    public void FuzzyDate_YearOnly_MapsToFirstDay()
    {
        long epoch = MandoCalendar.ToEpochDay(new MandoDate(47));
        Assert.Equal(MandoCalendar.ToEpochDay(new MandoDate(47, 1, 1)), epoch);
    }

    [Fact]
    public void FuzzyDate_YearAndMonth_MapsToFirstDayOfMonth()
    {
        long epoch = MandoCalendar.ToEpochDay(new MandoDate(47, 6));
        Assert.Equal(MandoCalendar.ToEpochDay(new MandoDate(47, 6, 1)), epoch);
    }
}
