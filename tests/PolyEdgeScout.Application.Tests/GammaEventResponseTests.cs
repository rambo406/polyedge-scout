namespace PolyEdgeScout.Application.Tests;

using System.Text.Json;
using PolyEdgeScout.Application.Configuration;
using PolyEdgeScout.Application.DTOs;

/// <summary>
/// Tests for <see cref="GammaEventResponse"/> deserialization and event-to-market flattening.
/// </summary>
public sealed class GammaEventResponseTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public void Deserialize_SingleEventWithMarkets_ReturnsCorrectStructure()
    {
        const string json = """
        [
            {
                "id": "event-1",
                "title": "BTC Price Event",
                "slug": "btc-price-event",
                "active": true,
                "closed": false,
                "markets": [
                    {
                        "conditionId": "cond-1",
                        "question": "Will BTC hit $100k?",
                        "active": true,
                        "closed": false,
                        "volumeNum": 500
                    },
                    {
                        "conditionId": "cond-2",
                        "question": "Will BTC hit $120k?",
                        "active": true,
                        "closed": false,
                        "volumeNum": 300
                    }
                ]
            }
        ]
        """;

        var events = JsonSerializer.Deserialize<List<GammaEventResponse>>(json, JsonOptions);

        Assert.NotNull(events);
        Assert.Single(events);
        Assert.Equal("event-1", events[0].Id);
        Assert.Equal("BTC Price Event", events[0].Title);
        Assert.Equal("btc-price-event", events[0].Slug);
        Assert.True(events[0].Active);
        Assert.False(events[0].Closed);
        Assert.Equal(2, events[0].Markets.Count);
        Assert.Equal("cond-1", events[0].Markets[0].ConditionId);
        Assert.Equal("Will BTC hit $100k?", events[0].Markets[0].Question);
        Assert.Equal("cond-2", events[0].Markets[1].ConditionId);
    }

    [Fact]
    public void Deserialize_EventWithEmptyMarkets_ReturnsEmptyList()
    {
        const string json = """
        [
            {
                "id": "event-empty",
                "title": "Empty Event",
                "slug": "empty-event",
                "active": true,
                "closed": false,
                "markets": []
            }
        ]
        """;

        var events = JsonSerializer.Deserialize<List<GammaEventResponse>>(json, JsonOptions);

        Assert.NotNull(events);
        Assert.Single(events);
        Assert.Empty(events[0].Markets);
    }

    [Fact]
    public void Deserialize_MultipleEvents_ReturnsAll()
    {
        const string json = """
        [
            {
                "id": "event-1",
                "title": "BTC Event",
                "slug": "btc-event",
                "active": true,
                "closed": false,
                "markets": [
                    { "conditionId": "cond-1", "question": "BTC Q1", "active": true, "closed": false, "volumeNum": 100 }
                ]
            },
            {
                "id": "event-2",
                "title": "ETH Event",
                "slug": "eth-event",
                "active": true,
                "closed": false,
                "markets": [
                    { "conditionId": "cond-2", "question": "ETH Q1", "active": true, "closed": false, "volumeNum": 200 },
                    { "conditionId": "cond-3", "question": "ETH Q2", "active": true, "closed": false, "volumeNum": 150 }
                ]
            }
        ]
        """;

        var events = JsonSerializer.Deserialize<List<GammaEventResponse>>(json, JsonOptions);

        Assert.NotNull(events);
        Assert.Equal(2, events.Count);
        Assert.Single(events[0].Markets);
        Assert.Equal(2, events[1].Markets.Count);
    }

    [Fact]
    public void Deserialize_DefaultValues_AreCorrect()
    {
        var response = new GammaEventResponse();

        Assert.Equal("", response.Id);
        Assert.Equal("", response.Title);
        Assert.Equal("", response.Slug);
        Assert.False(response.Active);
        Assert.False(response.Closed);
        Assert.Empty(response.Markets);
        Assert.Empty(response.Tags);
    }

    [Fact]
    public void FlattenEvents_SelectMany_ReturnsAllMarkets()
    {
        var events = new List<GammaEventResponse>
        {
            new()
            {
                Id = "event-1",
                Title = "BTC Event",
                Markets =
                [
                    new GammaMarketResponse { ConditionId = "cond-1", Question = "BTC Q1" },
                    new GammaMarketResponse { ConditionId = "cond-2", Question = "BTC Q2" },
                ],
            },
            new()
            {
                Id = "event-2",
                Title = "ETH Event",
                Markets =
                [
                    new GammaMarketResponse { ConditionId = "cond-3", Question = "ETH Q1" },
                ],
            },
        };

        var flattenedMarkets = events.SelectMany(e => e.Markets).ToList();

        Assert.Equal(3, flattenedMarkets.Count);
        Assert.Equal("cond-1", flattenedMarkets[0].ConditionId);
        Assert.Equal("cond-2", flattenedMarkets[1].ConditionId);
        Assert.Equal("cond-3", flattenedMarkets[2].ConditionId);
    }

    [Fact]
    public void FlattenEvents_EmptyEventList_ReturnsEmptyMarkets()
    {
        var events = new List<GammaEventResponse>();

        var flattenedMarkets = events.SelectMany(e => e.Markets).ToList();

        Assert.Empty(flattenedMarkets);
    }

    [Fact]
    public void FlattenEvents_EventsWithNoMarkets_ReturnsEmptyMarkets()
    {
        var events = new List<GammaEventResponse>
        {
            new() { Id = "event-1", Title = "Empty Event 1", Markets = [] },
            new() { Id = "event-2", Title = "Empty Event 2", Markets = [] },
        };

        var flattenedMarkets = events.SelectMany(e => e.Markets).ToList();

        Assert.Empty(flattenedMarkets);
    }

    [Fact]
    public void AppConfig_ServerEventTagId_DefaultsToUpOrDown()
    {
        var config = new AppConfig();

        Assert.Equal(102127, config.ServerEventTagId);
    }

    [Fact]
    public void AppConfig_InMemoryEventTagIds_DefaultsToCrypto()
    {
        var config = new AppConfig();

        Assert.Equal([21], config.InMemoryEventTagIds);
    }

    [Fact]
    public void Deserialize_EventWithTags_ParsesTagsCorrectly()
    {
        const string json = """
        [
            {
                "id": "event-1",
                "title": "BTC Up or Down",
                "slug": "btc-up-or-down",
                "active": true,
                "closed": false,
                "markets": [],
                "tags": [
                    { "id": "102127", "label": "Up or Down", "slug": "up-or-down" },
                    { "id": "21", "label": "Crypto", "slug": "crypto" }
                ]
            }
        ]
        """;

        var events = JsonSerializer.Deserialize<List<GammaEventResponse>>(json, JsonOptions);

        Assert.NotNull(events);
        Assert.Single(events);
        Assert.Equal(2, events[0].Tags.Count);
        Assert.Equal("102127", events[0].Tags[0].Id);
        Assert.Equal("Up or Down", events[0].Tags[0].Label);
        Assert.Equal("21", events[0].Tags[1].Id);
        Assert.Equal("Crypto", events[0].Tags[1].Label);
    }

    [Fact]
    public void Deserialize_EventWithoutTags_DefaultsToEmptyList()
    {
        const string json = """
        [
            {
                "id": "event-1",
                "title": "Some Event",
                "slug": "some-event",
                "active": true,
                "closed": false,
                "markets": []
            }
        ]
        """;

        var events = JsonSerializer.Deserialize<List<GammaEventResponse>>(json, JsonOptions);

        Assert.NotNull(events);
        Assert.Single(events);
        Assert.Empty(events[0].Tags);
    }
}
