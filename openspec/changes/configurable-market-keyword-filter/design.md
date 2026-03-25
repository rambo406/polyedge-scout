## Context

PolyEdgeScout is a Polymarket crypto micro-market scanner built with .NET Clean Architecture. The scanning pipeline works as follows:

1. `ScannerService` fetches active markets from the Gamma API
2. `MarketClassifier.IsCryptoMicro()` filters markets using a hardcoded `CryptoKeywords` array
3. `MarketClassifier.MeetsFilterCriteria()` applies freshness/volume filters
4. `ProbabilityModelService.CalculateProbabilityAsync()` uses `QuestionParser` to extract token symbol and target price, then calculates probability via Binance price data

Two bugs compound to produce false trading signals:
- **Overly broad include keywords** ("hit", "reach", "$", "by", etc.) in the static `MarketClassifier.CryptoKeywords` array let non-crypto markets through
- **0.5 default probability** in `ProbabilityModelService` for unparseable questions gives these junk markets a 50% probability, which combined with market prices creates phantom edges

The `MarketClassifier` is currently a static class in the Domain layer with no dependencies. `AppConfig` is a flat POCO bound from the `PolyEdgeScout` section of `appsettings.json`.

## Goals / Non-Goals

**Goals:**
- Make include and exclude keyword lists configurable via `appsettings.json` without code changes
- Add exclusion keyword support to reject obvious non-crypto markets (weather, sports, politics)
- Tighten default include keywords to crypto-specific terms only
- Eliminate phantom edges by skipping unparseable markets instead of assigning 0.5 probability
- Maintain Clean Architecture boundaries (Domain has no dependency on configuration infrastructure)

**Non-Goals:**
- ML-based or NLP-based market classification — this is keyword matching only
- Changing the `QuestionParser` logic itself — only changing how its null result is handled
- Adding a UI for keyword management — configuration file editing is sufficient
- Supporting regex patterns in keywords — plain substring matching is sufficient for now
- Changing the `MeetsFilterCriteria` volume/freshness logic

## Decisions

### 1. Pass keyword arrays as method parameters to `MarketClassifier`

**Decision**: Change `IsCryptoMicro(string question)` to `IsCryptoMicro(string question, IReadOnlyList<string> includeKeywords, IReadOnlyList<string> excludeKeywords)`. Keep `MarketClassifier` as a static class in the Domain layer.

**Alternatives considered**:
- *Make `MarketClassifier` a non-static service with DI*: Would require an interface, registration, and adds complexity for what is fundamentally a pure function. The Domain layer should not depend on `AppConfig`.
- *Move `MarketClassifier` to Application layer*: Violates the principle of keeping domain logic in the Domain layer. Classification is domain logic, configuration is application concern.

**Rationale**: Passing keyword arrays keeps the Domain layer dependency-free while allowing the Application layer (`ScannerService`) to supply configured values. The method remains a pure function that's easy to test.

### 2. Add `MarketFilterConfig` as a nested class in `AppConfig`

**Decision**: Add a `MarketFilterConfig` property to `AppConfig` with `IncludeKeywords` and `ExcludeKeywords` string arrays, bound from a `MarketFilter` section nested under the existing `PolyEdgeScout` section in `appsettings.json`.

**Alternatives considered**:
- *Separate top-level config section*: Would break the pattern of everything living under `PolyEdgeScout`.
- *Flat properties on AppConfig (e.g., `MarketFilterIncludeKeywords`)*: Arrays don't express well as flat properties and the nesting makes the JSON clearer.

**Rationale**: Matches existing config binding pattern. `AppConfig` already serves as the strongly-typed root.

### 3. Exclude-first evaluation order

**Decision**: Check exclude keywords first. If a market matches ANY exclude keyword, reject it immediately regardless of include matches. Then check include keywords.

**Alternatives considered**:
- *Include-first, then exclude*: Wastes time checking includes on markets that would be excluded anyway.
- *Scoring/weighting system*: Over-engineered for keyword matching.

**Rationale**: Exclude-first is simpler, faster, and more intuitive. If a question contains "temperature" or "election", it's not crypto regardless of whether it also contains "$".

### 4. Return `double?` from `CalculateProbabilityAsync` for unparseable markets

**Decision**: Change `ProbabilityModelService.CalculateProbabilityAsync` return type from `Task<double>` to `Task<double?>`. Return `null` when `QuestionParser.Parse` fails to extract symbol or target price. Update `IProbabilityModelService` interface accordingly.

**Alternatives considered**:
- *Throw exception for unparseable*: Exceptions for expected flow are anti-pattern.
- *Return 0.0*: Could be confused with a legitimately calculated near-zero probability.
- *Return a sentinel value (e.g., -1)*: Nullable is the idiomatic C# approach.

**Rationale**: Nullable double clearly communicates "no probability could be calculated" and forces callers to handle the case explicitly. The scanner pipeline already filters, so it naturally skips null results.

### 5. Default keyword values hardcoded in `MarketFilterConfig`

**Decision**: `MarketFilterConfig` property initializers contain sensible defaults (crypto-only include terms, common non-crypto exclude terms). If `appsettings.json` omits the `MarketFilter` section, the defaults apply.

**Rationale**: Zero-config should work. Operators who don't care about tuning get safe defaults. Those who do can override in `appsettings.json`.

## Risks / Trade-offs

- **[Breaking change to `IsCryptoMicro` signature]** → All callers must be updated. Mitigated by the fact that there are only two known callers (`ScannerService`, `BacktestService` if it exists), and the change is compile-time visible.
- **[Breaking change to `IProbabilityModelService` return type]** → All consumers must handle nullable. Mitigated by small number of callers and compile-time enforcement.
- **[Keyword tuning requires restart]** → Configuration is read at startup. Mitigated by the scan interval being short (60s) and restarts being cheap. Hot-reload via `IOptionsMonitor` could be added later if needed.
- **[Substring matching can still produce false positives]** → e.g., "BTC" matching in "ABTC Corp". Mitigated by having exclude keywords as a second filter and by the probability model rejecting markets QuestionParser can't parse. Regex support is a future enhancement if needed.
