namespace PolyEdgeScout.Application.DTOs;

/// <summary>
/// Represents a single kline (candlestick) data point from Binance.
/// </summary>
public sealed record KlineData
{
    /// <summary>Kline open time (Unix milliseconds).</summary>
    public long OpenTime { get; init; }

    /// <summary>Opening price.</summary>
    public double Open { get; init; }

    /// <summary>High price.</summary>
    public double High { get; init; }

    /// <summary>Low price.</summary>
    public double Low { get; init; }

    /// <summary>Closing price.</summary>
    public double Close { get; init; }

    /// <summary>Kline close time (Unix milliseconds).</summary>
    public long CloseTime { get; init; }
}
