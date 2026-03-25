namespace PolyEdgeScout.Domain.Tests;

using PolyEdgeScout.Domain.Enums;
using PolyEdgeScout.Domain.Interfaces;
using PolyEdgeScout.Domain.Services;
using PolyEdgeScout.Domain.ValueObjects;

/// <summary>
/// Tests for <see cref="EdgeCalculation"/> value object —
/// verifies edge computation with target-price scaling, fallback, and action determination.
/// </summary>
public sealed class EdgeCalculationTests
{
    private static readonly IEdgeFormula Formula = new DefaultScaledEdgeFormula();

    [Fact]
    public void Edge_WithTargetAndCurrentPrice_ScalesByPriceRatio()
    {
        // Model=0.60, Market=0.45, Current=60000, Target=100000
        // baseEdge = 0.15, priceRatio = |100000-60000|/60000 ≈ 0.6667
        // edge = 0.15 * (1 + 0.6667) = 0.15 * 1.6667 ≈ 0.25
        var edge = EdgeCalculation.Create(Formula, 0.60, 0.45, 100_000, 60_000);

        Assert.Equal(0.25, edge.Edge, precision: 2);
    }

    [Fact]
    public void Edge_WithTargetAndCurrentPriceClose_ScalesSmaller()
    {
        // Model=0.60, Market=0.45, Current=95000, Target=100000
        // baseEdge = 0.15, priceRatio = |100000-95000|/95000 ≈ 0.05263
        // edge = 0.15 * (1 + 0.05263) ≈ 0.15789 ≈ 0.158
        var edge = EdgeCalculation.Create(Formula, 0.60, 0.45, 100_000, 95_000);

        Assert.Equal(0.158, edge.Edge, precision: 3);
    }

    [Fact]
    public void Edge_WithoutTargetPrice_FallsBackToBaseEdge()
    {
        // Model=0.60, Market=0.45 → baseEdge = 0.15
        var edge = EdgeCalculation.Create(Formula, 0.60, 0.45, null, 60_000);

        Assert.Equal(0.15, edge.Edge, precision: 10);
    }

    [Fact]
    public void Edge_WithoutCurrentAssetPrice_FallsBackToBaseEdge()
    {
        var edge = EdgeCalculation.Create(Formula, 0.60, 0.45, 100_000, null);

        Assert.Equal(0.15, edge.Edge, precision: 10);
    }

    [Fact]
    public void Edge_WithZeroCurrentPrice_FallsBackToBaseEdge()
    {
        var edge = EdgeCalculation.Create(Formula, 0.60, 0.45, 100_000, 0);

        Assert.Equal(0.15, edge.Edge, precision: 10);
    }

    [Theory]
    [InlineData(0.60, 0.45, 0.08, TradeAction.Buy)]   // edge=0.15 > 0.08 → Buy
    [InlineData(0.40, 0.55, 0.08, TradeAction.Sell)]   // edge=-0.15 < -0.08 → Sell
    [InlineData(0.50, 0.45, 0.08, TradeAction.Hold)]   // edge=0.05 within [-0.08, 0.08] → Hold
    [InlineData(0.45, 0.50, 0.08, TradeAction.Hold)]   // edge=-0.05 within [-0.08, 0.08] → Hold
    [InlineData(0.60, 0.45, 0.15, TradeAction.Hold)]   // edge=0.15 exactly at threshold → Hold (not >)
    public void DetermineAction_ReturnsCorrectAction(
        double modelProb, double marketPrice, double minEdge, TradeAction expected)
    {
        var edge = EdgeCalculation.Create(Formula, modelProb, marketPrice, null, null);

        Assert.Equal(expected, edge.DetermineAction(minEdge));
    }

    [Fact]
    public void HasSufficientEdge_WhenEdgeAboveMin_ReturnsTrue()
    {
        var edge = EdgeCalculation.Create(Formula, 0.60, 0.45, null, null);

        Assert.True(edge.HasSufficientEdge(0.08));
    }

    [Fact]
    public void HasSufficientEdge_WhenEdgeBelowMin_ReturnsFalse()
    {
        var edge = EdgeCalculation.Create(Formula, 0.50, 0.45, null, null);

        Assert.False(edge.HasSufficientEdge(0.08));
    }

    [Fact]
    public void ToString_WithoutPrices_ContainsModelMarketEdge()
    {
        var edge = EdgeCalculation.Create(Formula, 0.60, 0.45, null, null);

        var result = edge.ToString();

        Assert.Contains("Model=", result);
        Assert.Contains("Market=", result);
        Assert.Contains("Edge=", result);
    }

    [Fact]
    public void ToString_WithPrices_ContainsTargetAndCurrent()
    {
        var edge = EdgeCalculation.Create(Formula, 0.60, 0.45, 100_000, 60_000);

        var result = edge.ToString();

        Assert.Contains("Target=", result);
        Assert.Contains("Current=", result);
    }
}
