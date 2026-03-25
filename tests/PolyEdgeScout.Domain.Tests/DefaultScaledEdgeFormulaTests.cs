namespace PolyEdgeScout.Domain.Tests;

using PolyEdgeScout.Domain.Services;

/// <summary>
/// Tests for <see cref="DefaultScaledEdgeFormula"/> —
/// verifies edge computation with target-price scaling and fallback to base edge.
/// </summary>
public sealed class DefaultScaledEdgeFormulaTests
{
    private readonly DefaultScaledEdgeFormula _formula = new();

    [Fact]
    public void Name_ReturnsScaled()
    {
        Assert.Equal("Scaled", _formula.Name);
    }

    [Fact]
    public void CalculateEdge_WithTargetAndCurrentPrice_ScalesByPriceRatio()
    {
        // baseEdge = 0.60 - 0.45 = 0.15
        // priceRatio = |100000 - 60000| / 60000 ≈ 0.6667
        // edge = 0.15 * (1 + 0.6667) ≈ 0.25
        var edge = _formula.CalculateEdge(0.60, 0.45, 100_000, 60_000);

        Assert.Equal(0.25, edge, precision: 2);
    }

    [Fact]
    public void CalculateEdge_WithCloseTargetAndCurrentPrice_ScalesSmaller()
    {
        // baseEdge = 0.15, priceRatio = |100000 - 95000| / 95000 ≈ 0.05263
        // edge = 0.15 * (1 + 0.05263) ≈ 0.158
        var edge = _formula.CalculateEdge(0.60, 0.45, 100_000, 95_000);

        Assert.Equal(0.158, edge, precision: 3);
    }

    [Fact]
    public void CalculateEdge_WithNullTargetPrice_FallsBackToBaseEdge()
    {
        var edge = _formula.CalculateEdge(0.60, 0.45, null, 60_000);

        Assert.Equal(0.15, edge, precision: 10);
    }

    [Fact]
    public void CalculateEdge_WithNullCurrentAssetPrice_FallsBackToBaseEdge()
    {
        var edge = _formula.CalculateEdge(0.60, 0.45, 100_000, null);

        Assert.Equal(0.15, edge, precision: 10);
    }

    [Fact]
    public void CalculateEdge_WithZeroCurrentAssetPrice_FallsBackToBaseEdge()
    {
        var edge = _formula.CalculateEdge(0.60, 0.45, 100_000, 0);

        Assert.Equal(0.15, edge, precision: 10);
    }

    [Fact]
    public void CalculateEdge_WithBothPricesNull_ReturnsBaseEdge()
    {
        var edge = _formula.CalculateEdge(0.60, 0.45, null, null);

        Assert.Equal(0.15, edge, precision: 10);
    }

    [Fact]
    public void CalculateEdge_NegativeBaseEdge_ScalesCorrectly()
    {
        // baseEdge = 0.40 - 0.55 = -0.15
        // priceRatio = |100000 - 60000| / 60000 ≈ 0.6667
        // edge = -0.15 * 1.6667 ≈ -0.25
        var edge = _formula.CalculateEdge(0.40, 0.55, 100_000, 60_000);

        Assert.Equal(-0.25, edge, precision: 2);
    }

    [Fact]
    public void CalculateEdge_EqualProbabilityAndPrice_ReturnsZero()
    {
        var edge = _formula.CalculateEdge(0.50, 0.50, null, null);

        Assert.Equal(0.0, edge, precision: 10);
    }
}
