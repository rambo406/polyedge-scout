## 1. DTOs and Configuration

- [x] 1.1 Create `GammaEventResponse` record in `PolyEdgeScout.Application/DTOs/` with `Id`, `Title`, `Slug`, `Active`, `Closed`, and `Markets` (List<GammaMarketResponse>) properties with JSON serialization attributes
- [x] 1.2 Add `CryptoTagId` property to `AppConfig` with default value `21`
- [x] 1.3 Add `CryptoTagId` entry to `appsettings.json` under the `PolyEdgeScout` section
- [x] 1.4 Update `ConfigurationLoader` to bind the `CryptoTagId` setting from configuration

## 2. API Client Interface and Implementation

- [x] 2.1 Add `FetchActiveEventsAsync(CancellationToken ct)` method to `IGammaApiClient` interface returning `Task<List<GammaEventResponse>>`
- [x] 2.2 Refactor `GammaApiClient.FetchWithRetryAsync` to be generic (`FetchWithRetryAsync<T>`) so it can deserialize both `List<GammaMarketResponse>` and `List<GammaEventResponse>`
- [x] 2.3 Implement `FetchActiveEventsAsync` in `GammaApiClient` calling `/events?tag_id={CryptoTagId}&active=true&closed=false&limit=100` using the generic retry method

## 3. Scanner Service Integration

- [x] 3.1 Update `ScannerService.ScanMarketsAsync` to call `FetchActiveEventsAsync` instead of `FetchActiveMarketsAsync`
- [x] 3.2 Add event-to-market flattening logic using `SelectMany(e => e.Markets)` after the API call
- [x] 3.3 Verify keyword filter (`MarketClassifier.IsCryptoMicro`) remains applied as secondary safety net after flattening

## 4. Tests

- [x] 4.1 Add unit tests for `GammaEventResponse` deserialization — single event, multiple events, empty markets array
- [x] 4.2 Add unit tests for `AppConfig.CryptoTagId` default value
- [x] 4.3 Update existing `ScannerService` tests (e.g., `ScanOrchestrationServiceTests`) to mock `FetchActiveEventsAsync` instead of `FetchActiveMarketsAsync`
- [x] 4.4 Add unit tests for event flattening — multiple events produce flat market list, empty events handled
- [x] 4.5 Run full test suite to verify no regressions
