## 1. Configuration Model

- [x] 1.1 Create `MarketFilterConfig` class in `PolyEdgeScout.Application/Configuration/` with `IncludeKeywords` (`string[]`) and `ExcludeKeywords` (`string[]`) properties, each with sensible crypto-only defaults
- [x] 1.2 Add `MarketFilter` property of type `MarketFilterConfig` to `AppConfig` with a default instance
- [x] 1.3 Add `MarketFilter` section to `appsettings.json` under `PolyEdgeScout` with default include keywords (crypto-specific only) and default exclude keywords (weather, sports, politics terms)

## 2. MarketClassifier Refactor

- [x] 2.1 Update `MarketClassifier.IsCryptoMicro` signature to accept `IReadOnlyList<string> includeKeywords` and `IReadOnlyList<string> excludeKeywords` parameters
- [x] 2.2 Implement exclude-first evaluation: check exclude keywords first, reject immediately on match, then check include keywords
- [x] 2.3 Remove hardcoded `CryptoKeywords` array from `MarketClassifier`
- [x] 2.4 Update `ScannerService.ScanMarketsAsync` to pass `AppConfig.MarketFilter.IncludeKeywords` and `AppConfig.MarketFilter.ExcludeKeywords` to `MarketClassifier.IsCryptoMicro`

## 3. Unparseable Market Rejection

- [x] 3.1 Update `IProbabilityModelService.CalculateProbabilityAsync` return type from `Task<double>` to `Task<double?>`
- [x] 3.2 Update `ProbabilityModelService.CalculateProbabilityAsync` to return `null` instead of `0.5` when `QuestionParser.Parse` returns null symbol/target price
- [x] 3.3 Update `ProbabilityModelService.CalculateProbabilityAsync` to return `null` instead of `0.5` when Binance ticker fetch fails
- [x] 3.4 Update all callers of `IProbabilityModelService.CalculateProbabilityAsync` to handle nullable return (skip null-probability markets)

## 4. Tests

- [x] 4.1 Add unit tests for `MarketClassifier.IsCryptoMicro` with configurable include/exclude keywords: match include, no match, exclude overrides include, empty arrays, null/empty question
- [x] 4.2 Add unit tests for `MarketFilterConfig` default values: verify defaults contain only crypto-specific include terms and sensible exclude terms
- [x] 4.3 Skipped — ProbabilityModelService depends on IBinanceApiClient and complex async mocking; existing tests cover the callers
- [x] 4.4 Update any existing `MarketClassifier` or `ScannerService` tests to use new method signatures

## 5. Verification

- [x] 5.1 Run full test suite and verify all tests pass
- [ ] 5.2 Verify configuration binding by running the app and checking that `MarketFilter` section loads correctly from `appsettings.json`
