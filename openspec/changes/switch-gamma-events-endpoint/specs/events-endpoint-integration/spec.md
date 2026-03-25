## ADDED Requirements

### Requirement: Event response deserialization
The system SHALL deserialize Gamma API `/events` endpoint responses into `GammaEventResponse` DTOs. Each event response SHALL contain a `Markets` property with a list of nested `GammaMarketResponse` objects.

#### Scenario: Deserialize event with nested markets
- **WHEN** the API returns an event JSON object with a `markets` array containing market objects
- **THEN** the system SHALL parse it into a `GammaEventResponse` with fully populated `Markets` list

#### Scenario: Deserialize event with empty markets array
- **WHEN** the API returns an event JSON object with an empty `markets` array
- **THEN** the system SHALL parse it into a `GammaEventResponse` with an empty `Markets` list

#### Scenario: Deserialize multiple events
- **WHEN** the API returns an array of event JSON objects
- **THEN** the system SHALL parse each into a separate `GammaEventResponse` instance

### Requirement: Fetch active crypto events from Gamma API
The system SHALL provide a `FetchActiveEventsAsync` method on `IGammaApiClient` that calls the `/events` endpoint with `tag_id={CryptoTagId}&active=true&closed=false&limit=100`. The method SHALL return a list of `GammaEventResponse` objects.

#### Scenario: Successful fetch of crypto events
- **WHEN** `FetchActiveEventsAsync` is called
- **THEN** the system SHALL make an HTTP GET request to `/events?tag_id={CryptoTagId}&active=true&closed=false&limit=100`
- **THEN** the system SHALL return the deserialized list of `GammaEventResponse` objects

#### Scenario: Rate-limited response with retry
- **WHEN** the API returns HTTP 429 (Too Many Requests)
- **THEN** the system SHALL retry with exponential backoff (same behavior as existing market fetching)

#### Scenario: API returns empty response
- **WHEN** the API returns an empty JSON array
- **THEN** the system SHALL return an empty list

### Requirement: Configurable crypto tag ID
The system SHALL expose a `CryptoTagId` property on `AppConfig` that defaults to `21`. This value SHALL be used in the `/events` endpoint URL.

#### Scenario: Default tag ID
- **WHEN** no `CryptoTagId` is specified in configuration
- **THEN** the system SHALL use `21` as the default tag ID

#### Scenario: Custom tag ID from configuration
- **WHEN** `CryptoTagId` is set to `1312` in `appsettings.json`
- **THEN** the system SHALL use `1312` in the `/events` endpoint URL

### Requirement: Flatten events to markets in scanner
The `ScannerService` SHALL call `FetchActiveEventsAsync`, flatten the returned events into a flat list of markets, and continue applying keyword filtering as a secondary safety net.

#### Scenario: Flatten events with multiple markets
- **WHEN** the events endpoint returns 3 events containing 2, 3, and 1 markets respectively
- **THEN** the scanner SHALL produce a flat list of 6 markets for downstream filtering

#### Scenario: Skip events with no markets
- **WHEN** an event contains an empty `markets` array
- **THEN** the scanner SHALL not produce any markets from that event

#### Scenario: Keyword filter applied after event flattening
- **WHEN** flattened markets include a non-crypto market that passed tag filtering
- **THEN** the keyword filter SHALL still exclude it based on `MarketClassifier.IsCryptoMicro` rules

### Requirement: Generic retry infrastructure
The `GammaApiClient` retry logic SHALL be generic so both `/markets` and `/events` endpoint calls use the same retry behavior (429 handling, exponential backoff, max 3 retries).

#### Scenario: Events endpoint uses same retry behavior as markets
- **WHEN** the `/events` endpoint returns HTTP 429
- **THEN** the system SHALL apply identical retry logic (exponential backoff, max 3 retries) as the `/markets` endpoint

#### Scenario: Non-retryable client error on events endpoint
- **WHEN** the `/events` endpoint returns HTTP 400
- **THEN** the system SHALL throw without retry (same as `/markets` behavior)

### Requirement: Backward compatibility
Existing `FetchActiveMarketsAsync` and `FetchResolvedMarketsAsync` methods SHALL remain available and unchanged. `BacktestService` SHALL continue using `FetchResolvedMarketsAsync`.

#### Scenario: Existing market fetch methods remain operational
- **WHEN** `FetchActiveMarketsAsync` is called
- **THEN** the system SHALL behave identically to the current implementation

#### Scenario: BacktestService unchanged
- **WHEN** `BacktestService.RunBacktestAsync` is called
- **THEN** it SHALL continue using `FetchResolvedMarketsAsync` without modification
