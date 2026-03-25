## ADDED Requirements

### Requirement: Configurable include keywords
The system SHALL load market include keywords from the `PolyEdgeScout:MarketFilter:IncludeKeywords` configuration section in `appsettings.json`. The `MarketClassifier.IsCryptoMicro` method SHALL accept an `IReadOnlyList<string>` of include keywords instead of using hardcoded values. A market SHALL be considered a match only if its question text contains at least one include keyword (case-insensitive substring match).

#### Scenario: Market matches a configured include keyword
- **WHEN** the `IncludeKeywords` configuration contains `["bitcoin", "eth", "sol"]` and a market question is `"Will Bitcoin hit $100K today?"`
- **THEN** `IsCryptoMicro` SHALL return `true`

#### Scenario: Market does not match any include keyword
- **WHEN** the `IncludeKeywords` configuration contains `["bitcoin", "eth", "sol"]` and a market question is `"Will it rain in New York tomorrow?"`
- **THEN** `IsCryptoMicro` SHALL return `false`

#### Scenario: Default include keywords when configuration is absent
- **WHEN** the `MarketFilter` section is not present in `appsettings.json`
- **THEN** the system SHALL use default include keywords containing only crypto-specific terms (token symbols and cryptocurrency names such as `"bitcoin"`, `"btc"`, `"eth"`, `"sol"`, `"crypto"`, `"token"`, `"coin"`) and SHALL NOT include generic terms like `"hit"`, `"reach"`, `"above"`, `"below"`, `"today"`, `"$"`, `"by"`, `"eod"`, `"milestone"`, or `"price"`

### Requirement: Configurable exclude keywords
The system SHALL load market exclude keywords from the `PolyEdgeScout:MarketFilter:ExcludeKeywords` configuration section in `appsettings.json`. The `MarketClassifier.IsCryptoMicro` method SHALL accept an `IReadOnlyList<string>` of exclude keywords. A market SHALL be rejected if its question text contains ANY exclude keyword (case-insensitive substring match), regardless of whether it also matches an include keyword.

#### Scenario: Market matches an exclude keyword and an include keyword
- **WHEN** the `ExcludeKeywords` configuration contains `["temperature", "weather"]` and the `IncludeKeywords` contains `["$"]` and a market question is `"Will temperature reach $100 today?"`
- **THEN** `IsCryptoMicro` SHALL return `false`

#### Scenario: Market matches an exclude keyword only
- **WHEN** the `ExcludeKeywords` configuration contains `["election", "president"]` and a market question is `"Will the president win the election?"`
- **THEN** `IsCryptoMicro` SHALL return `false`

#### Scenario: Default exclude keywords when configuration is absent
- **WHEN** the `MarketFilter` section is not present in `appsettings.json`
- **THEN** the system SHALL use default exclude keywords containing common non-crypto terms such as `"temperature"`, `"weather"`, `"rain"`, `"election"`, `"president"`, `"touchdown"`, `"goal"`, `"nfl"`, `"nba"`, `"mlb"`, `"hurricane"`, `"earthquake"`

### Requirement: Exclude-first evaluation order
The `MarketClassifier.IsCryptoMicro` method SHALL evaluate exclude keywords before include keywords. If a market matches any exclude keyword, it SHALL be rejected immediately without checking include keywords.

#### Scenario: Exclude check runs before include check
- **WHEN** a market question is `"Will Bitcoin price be affected by the election?"` and `ExcludeKeywords` contains `["election"]` and `IncludeKeywords` contains `["bitcoin"]`
- **THEN** `IsCryptoMicro` SHALL return `false` because the exclude match takes precedence

### Requirement: MarketFilterConfig strongly-typed configuration
The system SHALL provide a `MarketFilterConfig` class with `IncludeKeywords` and `ExcludeKeywords` properties (both `string[]`), nested under `AppConfig` as a `MarketFilter` property. The configuration SHALL bind from the `PolyEdgeScout:MarketFilter` section of `appsettings.json`.

#### Scenario: Configuration binding from appsettings.json
- **WHEN** `appsettings.json` contains a `MarketFilter` section with `IncludeKeywords` and `ExcludeKeywords` arrays under the `PolyEdgeScout` section
- **THEN** `AppConfig.MarketFilter.IncludeKeywords` and `AppConfig.MarketFilter.ExcludeKeywords` SHALL contain the configured values

#### Scenario: Empty keyword arrays
- **WHEN** `appsettings.json` sets `IncludeKeywords` to `[]` and `ExcludeKeywords` to `[]`
- **THEN** `IsCryptoMicro` SHALL return `false` for all markets (no includes match, so nothing passes)
