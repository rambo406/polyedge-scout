namespace PolyEdgeScout.Domain.Tests;

using PolyEdgeScout.Domain.Enums;
using PolyEdgeScout.Domain.Services;

/// <summary>
/// Tests for <see cref="QuestionParser.ParseStructured"/>,
/// <see cref="QuestionParser.ExtractDirection"/>, and
/// <see cref="QuestionParser.ExtractTimeWindow"/> covering directional market parsing.
/// </summary>
public sealed class QuestionParserDirectionalTests
{
    // ── Directional detection ─────────────────────────────────────────

    [Fact]
    public void ParseStructured_BitcoinUpOrDown_ReturnsDirectional()
    {
        var result = QuestionParser.ParseStructured("Will Bitcoin go up or down today?");

        var directional = Assert.IsType<ParseResult.Directional>(result);
        Assert.Equal("BTC", directional.Symbol);
        Assert.Equal(MarketDirection.Up, directional.Direction);
    }

    [Fact]
    public void ParseStructured_EthHigherOrLower_ReturnsDirectional()
    {
        var result = QuestionParser.ParseStructured("Will ETH go higher or lower this hour?");

        var directional = Assert.IsType<ParseResult.Directional>(result);
        Assert.Equal("ETH", directional.Symbol);
        Assert.Equal(MarketDirection.Up, directional.Direction);
    }

    [Fact]
    public void ParseStructured_CaseInsensitive_ReturnsDirectional()
    {
        var result = QuestionParser.ParseStructured("Will SOL go UP OR DOWN tonight?");

        var directional = Assert.IsType<ParseResult.Directional>(result);
        Assert.Equal("SOL", directional.Symbol);
        Assert.Equal(MarketDirection.Up, directional.Direction);
    }

    // ── Time window extraction ────────────────────────────────────────

    [Fact]
    public void ParseStructured_WithTimeWindow_ExtractsStartEndTimezone()
    {
        var result = QuestionParser.ParseStructured(
            "Will Bitcoin go up or down 9:45AM-10:00AM ET?");

        var directional = Assert.IsType<ParseResult.Directional>(result);
        Assert.Equal("BTC", directional.Symbol);
        Assert.Equal(new TimeOnly(9, 45), directional.WindowStart);
        Assert.Equal(new TimeOnly(10, 0), directional.WindowEnd);
        Assert.Equal("ET", directional.Timezone);
    }

    [Fact]
    public void ParseStructured_NoTimeWindow_ReturnsNullWindow()
    {
        var result = QuestionParser.ParseStructured("Will Bitcoin go up or down today?");

        var directional = Assert.IsType<ParseResult.Directional>(result);
        Assert.Null(directional.WindowStart);
        Assert.Null(directional.WindowEnd);
        Assert.Null(directional.Timezone);
    }

    [Fact]
    public void ParseStructured_CrossAmPmTimeWindow_Works()
    {
        var result = QuestionParser.ParseStructured(
            "Will ETH go up or down 11:00AM-1:00PM EST?");

        var directional = Assert.IsType<ParseResult.Directional>(result);
        Assert.Equal("ETH", directional.Symbol);
        Assert.Equal(new TimeOnly(11, 0), directional.WindowStart);
        Assert.Equal(new TimeOnly(13, 0), directional.WindowEnd);
        Assert.Equal("EST", directional.Timezone);
    }

    // ── Price target fallback ─────────────────────────────────────────

    [Fact]
    public void ParseStructured_PriceTargetQuestion_ReturnsPriceTarget()
    {
        var result = QuestionParser.ParseStructured("Will Bitcoin hit $100K?");

        var priceTarget = Assert.IsType<ParseResult.PriceTarget>(result);
        Assert.Equal("BTC", priceTarget.Symbol);
        Assert.Equal(100_000, priceTarget.TargetPrice);
    }

    // ── Unrecognised ──────────────────────────────────────────────────

    [Fact]
    public void ParseStructured_NoSymbol_ReturnsUnrecognised()
    {
        var result = QuestionParser.ParseStructured("Will it rain tomorrow?");

        Assert.IsType<ParseResult.Unrecognised>(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ParseStructured_EmptyOrNull_ReturnsUnrecognised(string? question)
    {
        var result = QuestionParser.ParseStructured(question!);

        Assert.IsType<ParseResult.Unrecognised>(result);
    }

    [Fact]
    public void ParseStructured_SymbolButNoPriceOrDirection_ReturnsUnrecognised()
    {
        var result = QuestionParser.ParseStructured("Bitcoin is interesting.");

        Assert.IsType<ParseResult.Unrecognised>(result);
    }

    // ── ExtractDirection edge cases ───────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ExtractDirection_NullOrEmpty_ReturnsNull(string? question)
    {
        Assert.Null(QuestionParser.ExtractDirection(question!));
    }

    [Fact]
    public void ExtractDirection_NoPhrasePresent_ReturnsNull()
    {
        Assert.Null(QuestionParser.ExtractDirection("Will Bitcoin hit $100K?"));
    }

    // ── ExtractTimeWindow edge cases ──────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ExtractTimeWindow_NullOrEmpty_ReturnsNulls(string? question)
    {
        var (start, end, tz) = QuestionParser.ExtractTimeWindow(question!);
        Assert.Null(start);
        Assert.Null(end);
        Assert.Null(tz);
    }

    [Fact]
    public void ExtractTimeWindow_NoTimePattern_ReturnsNulls()
    {
        var (start, end, tz) = QuestionParser.ExtractTimeWindow("Will Bitcoin go up today?");
        Assert.Null(start);
        Assert.Null(end);
        Assert.Null(tz);
    }

    [Fact]
    public void ExtractTimeWindow_WithoutTimezone_ReturnsNullTimezone()
    {
        var (start, end, tz) = QuestionParser.ExtractTimeWindow(
            "Will BTC go up or down 2:00PM-3:00PM?");
        Assert.Equal(new TimeOnly(14, 0), start);
        Assert.Equal(new TimeOnly(15, 0), end);
        Assert.Null(tz);
    }
}
