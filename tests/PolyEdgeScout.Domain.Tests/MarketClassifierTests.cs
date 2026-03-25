namespace PolyEdgeScout.Domain.Tests;

using PolyEdgeScout.Domain.Services;

/// <summary>
/// Tests for <see cref="MarketClassifier.IsCryptoMicro"/> with configurable keyword lists.
/// </summary>
public sealed class MarketClassifierTests
{
    private static readonly string[] DefaultInclude = ["bitcoin", "btc", "eth", "crypto", "token"];
    private static readonly string[] DefaultExclude = ["election", "weather", "nfl", "movie"];

    // ── Include keyword matching ──────────────────────────────────────

    [Theory]
    [InlineData("Will Bitcoin hit $100k by end of day?")]
    [InlineData("Will BTC reach $50,000?")]
    [InlineData("ETH price above $3000 today?")]
    [InlineData("Is this crypto market going up?")]
    public void IsCryptoMicro_IncludeKeywordPresent_ReturnsTrue(string question)
    {
        var result = MarketClassifier.IsCryptoMicro(question, DefaultInclude, DefaultExclude);
        Assert.True(result);
    }

    [Theory]
    [InlineData("Will it rain tomorrow in NYC?")]
    [InlineData("Who will win the Super Bowl?")]
    [InlineData("What color is the sky?")]
    public void IsCryptoMicro_NoIncludeKeyword_ReturnsFalse(string question)
    {
        var result = MarketClassifier.IsCryptoMicro(question, DefaultInclude, DefaultExclude);
        Assert.False(result);
    }

    // ── Exclude-first logic ───────────────────────────────────────────

    [Theory]
    [InlineData("Will the Bitcoin election results affect crypto?")]
    [InlineData("BTC weather forecast and token price")]
    [InlineData("Crypto nfl game score prediction")]
    public void IsCryptoMicro_ExcludeKeywordPresent_ReturnsFalseEvenWithIncludeMatch(string question)
    {
        var result = MarketClassifier.IsCryptoMicro(question, DefaultInclude, DefaultExclude);
        Assert.False(result);
    }

    [Fact]
    public void IsCryptoMicro_ExcludeCheckedBeforeInclude()
    {
        // "movie" is in exclude list; even though "bitcoin" is in include, exclude wins
        var question = "Bitcoin movie premiere token launch";
        var result = MarketClassifier.IsCryptoMicro(question, DefaultInclude, DefaultExclude);
        Assert.False(result);
    }

    // ── Edge cases ────────────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsCryptoMicro_NullOrWhitespace_ReturnsFalse(string? question)
    {
        var result = MarketClassifier.IsCryptoMicro(question!, DefaultInclude, DefaultExclude);
        Assert.False(result);
    }

    [Fact]
    public void IsCryptoMicro_CaseInsensitiveMatching()
    {
        var result = MarketClassifier.IsCryptoMicro("BITCOIN price today", DefaultInclude, DefaultExclude);
        Assert.True(result);
    }

    [Fact]
    public void IsCryptoMicro_EmptyIncludeKeywords_ReturnsFalse()
    {
        var result = MarketClassifier.IsCryptoMicro("Bitcoin price", [], DefaultExclude);
        Assert.False(result);
    }

    [Fact]
    public void IsCryptoMicro_EmptyExcludeKeywords_OnlyChecksIncludes()
    {
        var result = MarketClassifier.IsCryptoMicro("Bitcoin election results", DefaultInclude, []);
        Assert.True(result);
    }

    [Fact]
    public void IsCryptoMicro_CustomKeywords_Override()
    {
        string[] customInclude = ["gold", "silver"];
        string[] customExclude = ["paper"];

        Assert.True(MarketClassifier.IsCryptoMicro("Gold price rising", customInclude, customExclude));
        Assert.False(MarketClassifier.IsCryptoMicro("Bitcoin price rising", customInclude, customExclude));
        Assert.False(MarketClassifier.IsCryptoMicro("Gold paper trading", customInclude, customExclude));
    }
}
