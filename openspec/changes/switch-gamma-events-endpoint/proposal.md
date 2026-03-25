## Why

The app currently uses the Gamma API `/markets` endpoint with in-memory keyword filtering to find crypto markets. This is inefficient — we fetch all active markets and discard most of them. Polymarket's Gamma API supports tag-based filtering on the `/events` endpoint (tag_id=21 for Crypto), which pre-filters at the API level and returns richer event-level data. Switching to this endpoint reduces network payload, eliminates false negatives, and aligns with Polymarket's recommended API usage pattern.

## What Changes

- **Add `GammaEventResponse` DTO** to model the `/events` response, which wraps an array of nested `GammaMarketResponse` objects per event.
- **Add `FetchActiveEventsAsync()` method** to `IGammaApiClient` and `GammaApiClient` that calls `/events?tag_id={CryptoTagId}&active=true&closed=false&limit=100`.
- **Update `ScannerService`** to call `FetchActiveEventsAsync()`, flatten the events→markets hierarchy, and continue applying keyword filtering as a secondary safety net.
- **Add `CryptoTagId` configuration** to `AppConfig` (default: `21`) so the tag ID is externally configurable.
- **Keep existing `/markets` endpoint methods** as fallback options for backward compatibility and for backtesting (resolved markets).
- **Keep keyword filtering** as a secondary filter after tag-based pre-filtering.

## Capabilities

### New Capabilities
- `events-endpoint-integration`: Fetching crypto markets via the Gamma `/events` endpoint with tag-based filtering, including the new DTO, API client method, event-to-market flattening, and configurable tag ID.

### Modified Capabilities

## Impact

- **Application layer**: `IGammaApiClient` gets a new method. `ScannerService` changes its primary data source. `AppConfig` gains a new property.
- **Infrastructure layer**: `GammaApiClient` implements new endpoint call with existing retry logic.
- **DTOs**: New `GammaEventResponse` record added alongside existing `GammaMarketResponse`.
- **Configuration**: `appsettings.json` gains `CryptoTagId` field under `PolyEdgeScout` section.
- **Tests**: Existing scanner tests need updating; new tests for event flattening and the new API method.
- **No breaking changes**: Existing `/markets` methods remain available. BacktestService continues using `FetchResolvedMarketsAsync`.
