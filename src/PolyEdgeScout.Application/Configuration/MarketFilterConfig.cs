namespace PolyEdgeScout.Application.Configuration;

/// <summary>
/// Configuration for market keyword filtering.
/// Markets must match at least one include keyword and no exclude keywords.
/// </summary>
public sealed class MarketFilterConfig
{
    /// <summary>
    /// Keywords that indicate a market is relevant for trading (e.g., crypto symbols).
    /// A market must contain at least one of these keywords (case-insensitive).
    /// </summary>
    public string[] IncludeKeywords { get; set; } =
    [
        "bitcoin", "btc", "eth", "ethereum", "sol", "solana", "doge", "dogecoin",
        "xrp", "ripple", "ada", "cardano", "bnb", "avax", "avalanche",
        "matic", "polygon", "link", "chainlink", "dot", "polkadot", "crypto",
        "token", "coin", "shib", "pepe", "arb", "arbitrum", "op", "optimism",
        "sui", "apt", "aptos", "sei", "tia", "celestia", "near", "atom", "cosmos",
        "ftm", "fantom", "inj", "injective", "rune", "thorchain",
        "aave", "uni", "uniswap", "mkr", "maker", "ldo", "lido", "crv", "curve", "snx", "synthetix",
        "5 minutes", "15 minutes"
    ];

    /// <summary>
    /// Keywords that disqualify a market from trading (e.g., weather, sports, politics).
    /// If a market contains ANY of these keywords, it is excluded regardless of include matches.
    /// </summary>
    public string[] ExcludeKeywords { get; set; } =
    [
        "temperature", "weather", "rain", "snow", "wind", "humidity", "forecast", "fahrenheit", "celsius",
        "election", "vote", "president", "congress", "senate", "governor", "democrat", "republican",
        "touchdown", "goal", "team", "game score", "nfl", "nba", "mlb", "nhl", "fifa",
        "oscar", "grammy", "emmy", "netflix", "movie", "album"
    ];
}
