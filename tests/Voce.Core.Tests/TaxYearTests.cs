using Voce.Core.Services;

namespace Voce.Core.Tests;

public class TaxYearTests
{
    [Theory]
    [InlineData(2026, 4, 5, 2025)]
    [InlineData(2026, 4, 6, 2026)]
    [InlineData(2026, 11, 17, 2026)]
    [InlineData(2027, 1, 1, 2026)]
    public void Containing_uses_6_april_boundary(int y, int m, int d, int expectedStart) =>
        Assert.Equal(expectedStart, TaxYear.Containing(new DateOnly(y, m, d)).StartYear);

    [Fact]
    public void Bounds_and_label()
    {
        var ty = new TaxYear(2026);
        Assert.Equal(new DateOnly(2026, 4, 6), ty.Start);
        Assert.Equal(new DateOnly(2027, 4, 5), ty.End);
        Assert.Equal("2026/27", ty.Label);
    }

    [Fact]
    public void Range_lists_newest_first()
    {
        var years = TaxYear.Range(new DateOnly(2024, 5, 1), new DateOnly(2026, 11, 17));
        Assert.Equal([2026, 2025, 2024], years.Select(y => y.StartYear));
    }
}
