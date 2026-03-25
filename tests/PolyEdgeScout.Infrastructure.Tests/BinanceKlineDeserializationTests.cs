namespace PolyEdgeScout.Infrastructure.Tests;

using System.Text.Json;
using PolyEdgeScout.Application.DTOs;

public class BinanceKlineDeserializationTests
{
    [Fact]
    public void DeserializeKlines_ValidJson_ParsesCorrectly()
    {
        const string json = """
            [
                [1499040000000,"0.01634","0.80000","0.01575","0.01577","148976.11",1499644799999,"2434.19",308,"1756.87","28.46","0"],
                [1499644800000,"0.01577","0.09000","0.01500","0.01620","200000.00",1499731199999,"3200.00",400,"2000.00","35.00","0"]
            ]
            """;

        var rawKlines = JsonSerializer.Deserialize<JsonElement[]>(json);

        Assert.NotNull(rawKlines);
        Assert.Equal(2, rawKlines!.Length);

        var klines = rawKlines.Select(ParseKline).ToList();

        Assert.Equal(1499040000000L, klines[0].OpenTime);
        Assert.Equal(0.01634, klines[0].Open, precision: 5);
        Assert.Equal(0.80000, klines[0].High, precision: 5);
        Assert.Equal(0.01575, klines[0].Low, precision: 5);
        Assert.Equal(0.01577, klines[0].Close, precision: 5);
        Assert.Equal(1499644799999L, klines[0].CloseTime);

        Assert.Equal(1499644800000L, klines[1].OpenTime);
        Assert.Equal(0.01577, klines[1].Open, precision: 5);
        Assert.Equal(0.01620, klines[1].Close, precision: 5);
    }

    [Fact]
    public void DeserializeKlines_EmptyArray_ReturnsEmptyList()
    {
        const string json = "[]";

        var rawKlines = JsonSerializer.Deserialize<JsonElement[]>(json);

        Assert.NotNull(rawKlines);
        Assert.Empty(rawKlines!);
    }

    [Fact]
    public void DeserializeKlines_SingleKline_ParsesCorrectly()
    {
        const string json = """
            [
                [1609459200000,"29000.00","29500.00","28500.00","29300.00","50000.00",1609545599999,"1450000000.00",100000,"25000.00","725000000.00","0"]
            ]
            """;

        var rawKlines = JsonSerializer.Deserialize<JsonElement[]>(json);

        Assert.NotNull(rawKlines);
        Assert.Single(rawKlines!);

        var kline = ParseKline(rawKlines![0]);

        Assert.Equal(1609459200000L, kline.OpenTime);
        Assert.Equal(29000.00, kline.Open, precision: 2);
        Assert.Equal(29500.00, kline.High, precision: 2);
        Assert.Equal(28500.00, kline.Low, precision: 2);
        Assert.Equal(29300.00, kline.Close, precision: 2);
        Assert.Equal(1609545599999L, kline.CloseTime);
    }

    /// <summary>
    /// Mirrors the parsing logic used in BinanceApiClient.FetchKlinesAsync.
    /// </summary>
    private static KlineData ParseKline(JsonElement element)
    {
        return new KlineData
        {
            OpenTime = element[0].GetInt64(),
            Open = double.Parse(element[1].GetString()!),
            High = double.Parse(element[2].GetString()!),
            Low = double.Parse(element[3].GetString()!),
            Close = double.Parse(element[4].GetString()!),
            CloseTime = element[6].GetInt64(),
        };
    }
}
