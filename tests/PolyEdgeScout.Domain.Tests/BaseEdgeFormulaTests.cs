namespace PolyEdgeScout.Domain.Tests;

using PolyEdgeScout.Domain.Services;

/// <summary>
/// Tests for <see cref="BaseEdgeFormula"/> —
/// verifies simple modelProbability − marketPrice computation without scaling.
/// </summary>
public sealed class BaseEdgeFormulaTests
{
    private readonly BaseEdgeFormula _formula = new();

    [Fact]
    public void Name_ReturnsBase()
    {
        Assert.Equal("Base", _formula.Name);
    }

    [Fact]
    public void CalculateEdge_ReturnsSimpleDifference()
    {
        var edge = _formula.CalculateEdge(0.60, 0.45, null, null);

        Assert.Equal(0.15, edge, precision: 10);
    }

    [Fact]
    public void CalculateEdge_IgnoresTargetAndCurrentPrices()
    {
        // Even when target/current prices are provided, BaseEdgeFormula ignores them
        var edge = _formula.CalculateEdge(0.60, 0.45, 100_000, 60_000);

        Assert.Equal(0.15, edge, precision: 10);
    }

    [Fact]
    public void CalculateEdge_NegativeEdge_WhenMarketHigherThanModel()
    {
        var edge = _formula.CalculateEdge(0.40, 0.55, null, null);

        Assert.Equal(-0.15, edge, precision: 10);
    }

    [Fact]
    public void CalculateEdge_Zero_WhenProbabilitiesEqual()
    {
        var edge = _formula.CalculateEdge(0.50, 0.50, null, null);

        Assert.Equal(0.0, edge, precision: 10);
    }

    [Fact]
    public void CalculateEdge_WithNullPrices_ReturnsBaseEdge()
    {
        var edge = _formula.CalculateEdge(0.70, 0.30, null, null);

        Assert.Equal(0.40, edge, precision: 10);
    }

    [Fact]
    public void CalculateEdge_WithZeroCurrentAssetPrice_StillReturnsBaseEdge()
    {
        var edge = _formula.CalculateEdge(0.60, 0.45, 100_000, 0);

        Assert.Equal(0.15, edge, precision: 10);
    }
}
