namespace PolyEdgeScout.Domain.Tests;

using PolyEdgeScout.Domain.Services;

public class MathHelperDriftTests
{
    [Fact]
    public void CalculateDrift_ZeroDrift_ReturnsZero()
    {
        var prices = new List<double> { 100.0, 100.0, 100.0 };

        double drift = MathHelper.CalculateDrift(prices);

        Assert.Equal(0.0, drift);
    }

    [Fact]
    public void CalculateDrift_PositiveDrift_ReturnsPositive()
    {
        var prices = new List<double> { 100.0, 105.0, 110.0 };

        double drift = MathHelper.CalculateDrift(prices);

        Assert.Equal(0.10, drift, precision: 10);
    }

    [Fact]
    public void CalculateDrift_NegativeDrift_ReturnsNegative()
    {
        var prices = new List<double> { 100.0, 95.0, 90.0 };

        double drift = MathHelper.CalculateDrift(prices);

        Assert.Equal(-0.10, drift, precision: 10);
    }

    [Fact]
    public void CalculateDrift_SinglePrice_ReturnsZero()
    {
        var prices = new List<double> { 100.0 };

        double drift = MathHelper.CalculateDrift(prices);

        Assert.Equal(0.0, drift);
    }

    [Fact]
    public void CalculateDrift_EmptyList_ReturnsZero()
    {
        var prices = new List<double>();

        double drift = MathHelper.CalculateDrift(prices);

        Assert.Equal(0.0, drift);
    }

    [Theory]
    [InlineData(0.05, 0.05)]
    [InlineData(-0.03, -0.03)]
    [InlineData(0.0, 0.0)]
    public void ClampDrift_WithinRange_ReturnsUnchanged(double input, double expected)
    {
        double result = MathHelper.ClampDrift(input);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0.15, 0.10)]
    [InlineData(0.50, 0.10)]
    public void ClampDrift_ExceedsMax_ReturnsClamped(double input, double expected)
    {
        double result = MathHelper.ClampDrift(input);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(-0.15, -0.10)]
    [InlineData(-0.50, -0.10)]
    public void ClampDrift_BelowMin_ReturnsClamped(double input, double expected)
    {
        double result = MathHelper.ClampDrift(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ClampDrift_CustomMaxDrift_UsesCustomRange()
    {
        double result = MathHelper.ClampDrift(0.30, maxDrift: 0.20);

        Assert.Equal(0.20, result);
    }

    [Fact]
    public void CalculateDrift_FirstPriceZero_ReturnsZero()
    {
        var prices = new List<double> { 0.0, 100.0 };

        double drift = MathHelper.CalculateDrift(prices);

        Assert.Equal(0.0, drift);
    }
}
