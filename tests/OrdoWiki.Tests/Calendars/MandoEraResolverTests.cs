namespace OrdoWiki.Tests.Calendars;

using OrdoWiki.Data.Calendars;

public class MandoEraResolverTests
{
    private static readonly MandoEraInfo Bcw = new("Before Civil War", "BCW", 0, EraDirection.Backward);
    private static readonly MandoEraInfo Acw = new("After Civil War", "ACW", 0, EraDirection.Forward);
    private static readonly MandoEraInfo Ar  = new("After Reformation", "AR", 100, EraDirection.Forward);

    private static readonly IReadOnlyList<MandoEraInfo> SeedEras = [Bcw, Acw];
    private static readonly IReadOnlyList<MandoEraInfo> WithReformation = [Bcw, Acw, Ar];

    [Theory]
    [InlineData(0, "ACW")]
    [InlineData(47, "ACW")]
    [InlineData(-1, "BCW")]
    [InlineData(-47, "BCW")]
    public void Resolve_SeedEras(int absoluteYear, string expectedShortCode)
    {
        MandoEraInfo? era = MandoEraResolver.Resolve(SeedEras, absoluteYear);
        Assert.NotNull(era);
        Assert.Equal(expectedShortCode, era.Value.ShortCode);
    }

    [Theory]
    [InlineData(0, "ACW")]
    [InlineData(99, "ACW")]
    [InlineData(100, "AR")]
    [InlineData(150, "AR")]
    [InlineData(-1, "BCW")]
    public void Resolve_WithThirdEra_PrefersLatestForwardEraThatStillCoversYear(int absoluteYear, string expectedShortCode)
    {
        MandoEraInfo? era = MandoEraResolver.Resolve(WithReformation, absoluteYear);
        Assert.NotNull(era);
        Assert.Equal(expectedShortCode, era.Value.ShortCode);
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(-47, 47)]
    public void DisplayYear_BackwardEra(int absoluteYear, int expectedDisplay)
    {
        Assert.Equal(expectedDisplay, MandoEraResolver.DisplayYear(Bcw, absoluteYear));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(47, 47)]
    public void DisplayYear_ForwardEraAcw(int absoluteYear, int expectedDisplay)
    {
        Assert.Equal(expectedDisplay, MandoEraResolver.DisplayYear(Acw, absoluteYear));
    }

    [Theory]
    [InlineData(100, 0)]
    [InlineData(150, 50)]
    public void DisplayYear_ForwardEraAfterReformation(int absoluteYear, int expectedDisplay)
    {
        Assert.Equal(expectedDisplay, MandoEraResolver.DisplayYear(Ar, absoluteYear));
    }

    [Theory]
    [InlineData(1, -1)]
    [InlineData(47, -47)]
    public void ToAbsoluteYear_Backward(int displayYear, int expectedAbsolute)
    {
        Assert.Equal(expectedAbsolute, MandoEraResolver.ToAbsoluteYear(Bcw, displayYear));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(47, 47)]
    public void ToAbsoluteYear_ForwardAcw(int displayYear, int expectedAbsolute)
    {
        Assert.Equal(expectedAbsolute, MandoEraResolver.ToAbsoluteYear(Acw, displayYear));
    }

    [Theory]
    [InlineData(0, 100)]
    [InlineData(50, 150)]
    public void ToAbsoluteYear_ForwardAr(int displayYear, int expectedAbsolute)
    {
        Assert.Equal(expectedAbsolute, MandoEraResolver.ToAbsoluteYear(Ar, displayYear));
    }

    [Fact]
    public void ToAbsoluteYear_RoundTripsThroughDisplayYear()
    {
        foreach (MandoEraInfo era in WithReformation)
        {
            for (int displayed = 0; displayed < 50; displayed++)
            {
                int absolute = MandoEraResolver.ToAbsoluteYear(era, displayed);
                Assert.Equal(displayed, MandoEraResolver.DisplayYear(era, absolute));
            }
        }
    }
}
