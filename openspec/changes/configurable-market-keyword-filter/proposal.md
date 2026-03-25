## Why

The `MarketClassifier.IsCryptoMicro()` method uses hardcoded keywords to identify crypto markets, but includes overly broad terms like "hit", "reach", "above", "below", "today", "$", and "by" that match non-crypto markets (weather, sports, politics). There is no exclusion mechanism to reject obvious non-crypto markets. Additionally, `ProbabilityModelService` assigns a default 0.5 probability to markets that `QuestionParser` cannot parse, which creates phantom edges on non-crypto markets that slip through the filter. These three issues compound: loose keywords let junk markets in, there's no second filter to catch them, and unparseable junk gets a 50% probability that generates false trading signals.

Making the keyword lists configurable via `appsettings.json` allows operators to tune filtering without code changes, enabling rapid iteration as new market categories appear on Polymarket.

## What Changes

- **Remove overly broad keywords** from the default include list — drop generic terms ("hit", "reach", "above", "below", "today", "$", "by", "eod", "milestone", "price") that match non-crypto markets
- **Add `MarketFilter` configuration section** to `appsettings.json` with `IncludeKeywords` and `ExcludeKeywords` arrays
- **Add `MarketFilterConfig` class** to `AppConfig` (or as a nested configuration object) to strongly type the new settings
- **Refactor `MarketClassifier`** from a static class with hardcoded keywords to accept keyword configuration via dependency injection or method parameters
- **Add exclusion keyword checking** — markets matching ANY exclude keyword are rejected regardless of include matches
- **Fix `ProbabilityModelService` default** — return `null` (skip market) instead of `0.5` when `QuestionParser` cannot parse the market question, preventing phantom edges
- **Update `ScannerService`** to handle nullable probability results by skipping unparseable markets

## Capabilities

### New Capabilities
- `configurable-keyword-filter`: Externalized include/exclude keyword lists loaded from `appsettings.json`, with a `MarketFilterConfig` model and updated `MarketClassifier` that accepts configuration
- `unparseable-market-rejection`: Changed `ProbabilityModelService` behavior to return null for unparseable markets instead of a 0.5 default, with `ScannerService` filtering out null-probability results

### Modified Capabilities

## Impact

- **Domain layer** (`PolyEdgeScout.Domain`): `MarketClassifier` changes from static hardcoded keywords to accepting keyword arrays — breaks current static API
- **Application layer** (`PolyEdgeScout.Application`): `AppConfig` gains `MarketFilter` property; `ProbabilityModelService.CalculateProbabilityAsync` return type changes from `Task<double>` to `Task<double?>` (nullable); `ScannerService` updated to filter null probability results
- **Configuration** (`appsettings.json`): New `MarketFilter` section with `IncludeKeywords` and `ExcludeKeywords` arrays
- **Tests**: Existing `MarketClassifier` tests (if any) need updating for new signature; new tests for exclude keywords, configuration binding, and null probability handling
- **No external API or dependency changes** — this is an internal filtering/configuration change
