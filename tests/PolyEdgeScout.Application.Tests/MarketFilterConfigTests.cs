namespace PolyEdgeScout.Application.Tests;

using PolyEdgeScout.Application.Configuration;

/// <summary>
/// Tests for <see cref="MarketFilterConfig"/> default values.
/// </summary>
public sealed class MarketFilterConfigTests
{
    [Fact]
    public void DefaultIncludeKeywords_ContainsCryptoSymbols()
    {
        var config = new MarketFilterConfig();

        Assert.NotEmpty(config.IncludeKeywords);
        Assert.Contains("bitcoin", config.IncludeKeywords);
        Assert.Contains("btc", config.IncludeKeywords);
        Assert.Contains("eth", config.IncludeKeywords);
        Assert.Contains("ethereum", config.IncludeKeywords);
        Assert.Contains("sol", config.IncludeKeywords);
        Assert.Contains("crypto", config.IncludeKeywords);
    }

    [Fact]
    public void DefaultExcludeKeywords_ContainsNonCryptoCategories()
    {
        var config = new MarketFilterConfig();

        Assert.NotEmpty(config.ExcludeKeywords);
        // Weather
        Assert.Contains("weather", config.ExcludeKeywords);
        Assert.Contains("temperature", config.ExcludeKeywords);
        // Politics
        Assert.Contains("election", config.ExcludeKeywords);
        Assert.Contains("president", config.ExcludeKeywords);
        // Sports
        Assert.Contains("nfl", config.ExcludeKeywords);
        Assert.Contains("nba", config.ExcludeKeywords);
        // Entertainment
        Assert.Contains("oscar", config.ExcludeKeywords);
        Assert.Contains("netflix", config.ExcludeKeywords);
    }

    [Fact]
    public void IncludeKeywords_CanBeOverridden()
    {
        var config = new MarketFilterConfig
        {
            IncludeKeywords = ["gold", "silver"]
        };

        Assert.Equal(2, config.IncludeKeywords.Length);
        Assert.Contains("gold", config.IncludeKeywords);
        Assert.DoesNotContain("bitcoin", config.IncludeKeywords);
    }

    [Fact]
    public void ExcludeKeywords_CanBeOverridden()
    {
        var config = new MarketFilterConfig
        {
            ExcludeKeywords = ["spam"]
        };

        Assert.Single(config.ExcludeKeywords);
        Assert.Contains("spam", config.ExcludeKeywords);
        Assert.DoesNotContain("election", config.ExcludeKeywords);
    }

    [Fact]
    public void AppConfig_MarketFilter_HasDefaultInstance()
    {
        var appConfig = new AppConfig();
        Assert.NotNull(appConfig.MarketFilter);
        Assert.NotEmpty(appConfig.MarketFilter.IncludeKeywords);
        Assert.NotEmpty(appConfig.MarketFilter.ExcludeKeywords);
    }
}
