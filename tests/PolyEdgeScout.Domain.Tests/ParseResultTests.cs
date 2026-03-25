namespace PolyEdgeScout.Domain.Tests;

using PolyEdgeScout.Domain.Enums;
using PolyEdgeScout.Domain.Services;

/// <summary>
/// Tests for the <see cref="ParseResult"/> type hierarchy —
/// verifies record properties, equality, and pattern matching.
/// </summary>
public sealed class ParseResultTests
{
    [Fact]
    public void PriceTargetResult_HasCorrectProperties()
    {
        var result = new ParseResult.PriceTarget("BTC", 100_000);

        Assert.Equal("BTC", result.Symbol);
        Assert.Equal(100_000, result.TargetPrice);
    }

    [Fact]
    public void DirectionalResult_HasCorrectProperties()
    {
        var result = new ParseResult.Directional(
            "ETH",
            MarketDirection.Up,
            new TimeOnly(9, 0),
            new TimeOnly(10, 0),
            "ET");

        Assert.Equal("ETH", result.Symbol);
        Assert.Equal(MarketDirection.Up, result.Direction);
        Assert.Equal(new TimeOnly(9, 0), result.WindowStart);
        Assert.Equal(new TimeOnly(10, 0), result.WindowEnd);
        Assert.Equal("ET", result.Timezone);
    }

    [Fact]
    public void DirectionalResult_OptionalFieldsDefaultToNull()
    {
        var result = new ParseResult.Directional("SOL", MarketDirection.Down);

        Assert.Null(result.WindowStart);
        Assert.Null(result.WindowEnd);
        Assert.Null(result.Timezone);
    }

    [Fact]
    public void UnrecognisedResult_IsUnrecognised()
    {
        var result = new ParseResult.Unrecognised();

        Assert.IsType<ParseResult.Unrecognised>(result);
    }

    [Fact]
    public void PatternMatching_WorksForAllTypes()
    {
        ParseResult[] results =
        [
            new ParseResult.PriceTarget("BTC", 50_000),
            new ParseResult.Directional("ETH", MarketDirection.Up),
            new ParseResult.Unrecognised()
        ];

        var descriptions = results.Select(r => r switch
        {
            ParseResult.PriceTarget pt => $"PriceTarget:{pt.Symbol}@{pt.TargetPrice}",
            ParseResult.Directional d  => $"Directional:{d.Symbol}:{d.Direction}",
            ParseResult.Unrecognised   => "Unrecognised",
            _                          => "Unknown"
        }).ToArray();

        Assert.Equal("PriceTarget:BTC@50000", descriptions[0]);
        Assert.Equal("Directional:ETH:Up", descriptions[1]);
        Assert.Equal("Unrecognised", descriptions[2]);
    }

    [Fact]
    public void Records_SupportValueEquality()
    {
        var a = new ParseResult.PriceTarget("BTC", 100_000);
        var b = new ParseResult.PriceTarget("BTC", 100_000);

        Assert.Equal(a, b);
    }

    [Fact]
    public void Records_DifferentValues_NotEqual()
    {
        var a = new ParseResult.PriceTarget("BTC", 100_000);
        var b = new ParseResult.PriceTarget("ETH", 100_000);

        Assert.NotEqual(a, b);
    }
}
