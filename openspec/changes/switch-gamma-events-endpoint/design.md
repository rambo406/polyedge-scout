## Context

The Polymarket Gamma API provides two relevant endpoints for market data:

1. **`/markets`** (current): Returns a flat list of individual markets. Requires client-side keyword filtering to isolate crypto markets from all active markets on the platform.
2. **`/events`** (target): Returns events grouped by topic, with nested `markets[]` arrays. Supports `tag_id` filtering at the API level (tag_id=21 = Crypto, 1312 = Crypto Prices, 100150 = Memecoins).

Currently, `GammaApiClient.FetchActiveMarketsAsync()` calls `/markets?active=true&closed=false&volume_num_min=...&volume_num_max=...&limit=50`, and `ScannerService` applies `MarketClassifier.IsCryptoMicro()` in-memory to filter results. This works but is wasteful — most fetched markets are non-crypto.

The `/events` endpoint with `tag_id=21` pre-filters to crypto at the API level, reducing payload and false negatives while providing richer event-level context.

## Goals / Non-Goals

**Goals:**
- Pre-filter to crypto markets at the API level using the `/events` endpoint with tag-based filtering
- Reduce network payload by fetching only crypto-tagged events
- Make the crypto tag ID externally configurable (default: 21)
- Keep keyword filtering as a secondary safety net after tag-based pre-filtering
- Maintain backward compatibility (existing `/markets` methods remain available)

**Non-Goals:**
- Removing the keyword filter system entirely
- Changing the BacktestService data source (resolved markets continue using `/markets`)
- Adding support for multiple tag IDs simultaneously (future enhancement)
- Displaying event-level grouping in the UI (markets are flattened for existing consumers)

## Decisions

### 1. New `GammaEventResponse` DTO with nested markets

**Decision:** Create a `GammaEventResponse` record that mirrors the `/events` response schema, containing a `List<GammaMarketResponse> Markets` property.

**Rationale:** The `/events` endpoint returns a different shape than `/markets`. A dedicated DTO preserves type safety and keeps the existing `GammaMarketResponse` unchanged. Markets nested within events use the same field schema as the `/markets` endpoint, so we can reuse `GammaMarketResponse` as the nested type.

**Alternative considered:** Mapping event responses directly to `GammaMarketResponse` lists in the HTTP client — rejected because it hides the event→market relationship and loses event metadata.

### 2. Add `FetchActiveEventsAsync()` to `IGammaApiClient`

**Decision:** Add a new method `Task<List<GammaEventResponse>> FetchActiveEventsAsync(CancellationToken ct)` alongside existing methods.

**Rationale:** Keeps the interface additive (no breaking changes). Consumers choose which data source to use. The existing retry infrastructure in `GammaApiClient` is reusable via a generic `FetchWithRetryAsync<T>` refactor.

**Alternative considered:** Replacing `FetchActiveMarketsAsync()` entirely — rejected because it would break BacktestService and remove the fallback option.

### 3. Flatten events→markets in `ScannerService`

**Decision:** `ScannerService.ScanMarketsAsync()` calls `FetchActiveEventsAsync()`, then uses `SelectMany(e => e.Markets)` to flatten event-nested markets into a flat list for downstream processing.

**Rationale:** The rest of the pipeline (MarketMapper, MarketClassifier, CLOB enrichment) expects flat market lists. Flattening at the service boundary keeps changes minimal.

### 4. Refactor `FetchWithRetryAsync` to be generic

**Decision:** Change the private retry method from `FetchWithRetryAsync(string url, CancellationToken ct)` returning `List<GammaMarketResponse>` to `FetchWithRetryAsync<T>(string url, CancellationToken ct)` returning `T?` (or a specific list variant).

**Rationale:** The retry logic (429 handling, exponential backoff, error logging) is identical for both endpoints. Genericizing avoids code duplication.

### 5. Configurable `CryptoTagId` in `AppConfig`

**Decision:** Add `int CryptoTagId { get; set; } = 21;` to `AppConfig` with a corresponding entry in `appsettings.json`.

**Rationale:** Hardcoding tag_id=21 would work but limits flexibility. Making it configurable allows switching to other tags (1312, 100150) or adapting if Polymarket changes tag IDs, without recompilation.

### 6. Keep keyword filter as secondary safety net

**Decision:** After tag-based API filtering, continue applying `MarketClassifier.IsCryptoMicro()` as a secondary filter.

**Rationale:** Tag-based filtering is broad (all crypto). The keyword filter provides fine-grained control (specific coins, exclude sports-related crypto mentions). Defense-in-depth: if the tag includes unexpected markets, keywords catch them.

## Risks / Trade-offs

- **[API change risk]** Polymarket could change tag IDs or event structure → **Mitigation:** Tag ID is configurable; existing `/markets` methods remain as fallback.
- **[Increased API response size]** `/events?limit=100` returns full event objects with nested markets which could be larger than `/markets?limit=50` → **Mitigation:** The pre-filtering to crypto significantly reduces total results vs. unfiltered `/markets`.
- **[Deserialization complexity]** Nested event→market JSON requires correct DTO modeling → **Mitigation:** Thorough unit tests for event response parsing.
- **[Dual-path maintenance]** Keeping both `/events` and `/markets` code paths increases surface area → **Mitigation:** They share the same retry infrastructure and market DTOs. The `/markets` path is stable and rarely touched.
