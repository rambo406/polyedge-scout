## 1. Domain Layer — EdgeCalculation & ModelEvaluation

- [x] 1.1 Add `TargetPrice` (double?) and `CurrentAssetPrice` (double?) properties to `EdgeCalculation` value object
- [x] 1.2 Update the `Edge` computed property to use the target-price-aware formula: `(ModelProbability - MarketPrice) * (1 + PriceRatio)` when both target and current prices are present, falling back to `ModelProbability - MarketPrice` otherwise
- [x] 1.3 Add `DetermineAction(double minEdge)` method to `EdgeCalculation` returning `TradeAction` with symmetric thresholds (BUY/SELL/HOLD)
- [x] 1.4 Update `EdgeCalculation.ToString()` to include target price info when present
- [x] 1.5 Write unit tests for `EdgeCalculation`: target-price edge, fallback edge, DetermineAction for BUY/SELL/HOLD, and edge cases (zero current price, null properties)

## 2. Application Layer — ModelEvaluation DTO

- [x] 2.1 Create `ModelEvaluation` record in `Application/DTOs/` with `ModelProbability`, `TargetPrice`, and `CurrentAssetPrice` properties
- [x] 2.2 Update `IProbabilityModelService.CalculateProbabilityAsync` return type from `double?` to `ModelEvaluation?`
- [x] 2.3 Update `ProbabilityModelService` to return `ModelEvaluation` from `CalculatePriceTargetProbabilityAsync` (with target price and current asset price) and `CalculateDirectionalProbabilityAsync` (with current asset price, null target price)

## 3. Application Layer — MarketScanResult Action Enum

- [x] 3.1 Change `MarketScanResult.Action` from `string` to `TradeAction` enum, update default from `"HOLD"` to `TradeAction.Hold`
- [x] 3.2 Update `ScanOrchestrationService.ScanAndEvaluateAsync` to use `ModelEvaluation`, construct `EdgeCalculation` with target price context, and use `DetermineAction` for the action field
- [x] 3.3 Update `ScanOrchestrationService.ScanEvaluateAndAutoTradeAsync` to pass `ModelEvaluation` context through to order evaluation

## 4. Application Layer — OrderService & BacktestService

- [x] 4.1 Update `OrderService.EvaluateAndTrade` to accept `ModelEvaluation` (or target/current price context) and construct `EdgeCalculation` with target price awareness
- [x] 4.2 Add SELL trade path in `OrderService.EvaluateAndTrade` — when `DetermineAction` returns `Sell`, create a Trade with `TradeAction.Sell` and `EntryPrice = market.NoPrice`
- [x] 4.3 Update `BacktestService` edge calculation to use `ModelEvaluation` and `EdgeCalculation` with target price context, matching the live scanner formula
- [x] 4.4 Update `IOrderService.EvaluateAndTrade` signature if needed to accept additional context

## 5. Console Layer — Display Updates

- [x] 5.1 Update `MarketTableSource` Action column rendering to use `TradeAction.ToString().ToUpperInvariant()` instead of raw string
- [x] 5.2 Update `MarketTableViewModel.ExecuteTradeAsync` edge check to use enum comparison (and handle SELL edge check as absolute value)

## 6. Test Updates

- [x] 6.1 Update `MarketTableViewModelTests` to use `TradeAction` enum instead of `Action = "BUY"` / `"HOLD"` strings
- [x] 6.2 Update `DashboardViewModelTests` to use `TradeAction` enum
- [x] 6.3 Add tests for SELL signal generation in `OrderService` (negative edge exceeding threshold)
- [x] 6.4 Add tests for `ScanOrchestrationService` with target-price-aware edge (mock `ModelEvaluation` with price context)
- [x] 6.5 Verify all existing tests pass with updated signatures and enum types
