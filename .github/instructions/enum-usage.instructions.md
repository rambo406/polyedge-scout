---
description: 'Critical rule: DO NOT USE STRING for enum types. Always use proper enum types for type safety and code quality.'
applyTo: '**/*.cs, **/*.ts'
---

# Enum Usage: NO STRINGS for Enum Types

## Critical Rule

**NEVER use `string` where an enum type should be used.** This is a foundational principle for type safety, code quality, and maintainability.

## Backend (C#)

### Enum Types in DTOs and Models

ALWAYS use the actual enum type, never `string`:

```csharp
// ✅ CORRECT - Use the enum
public sealed record DrawResultDto
{
    public LotteryProviderType ProviderType { get; init; }
    public Currency Currency { get; init; }
}

// ❌ WRONG - Never use string for enum values
public sealed record DrawResultDto
{
    public string ProviderCode { get; init; }  // NO!
    public string Currency { get; init; }      // NO!
}
```

### Common Enums

- `LotteryProviderType` - Use instead of `string providerCode` or `string provider`
- `Currency` - Use instead of `string currency` or `string currencyCode`
- `AccountStatus` - Use instead of `string status`
- `BetCategory` - Use instead of `string category`
- `DrawStatus` - Use instead of `string status`

### Mapping from Domain Objects

When mapping from domain objects to DTOs:

```csharp
// ✅ CORRECT - Convert LotteryProvider to enum
ProviderType = result.Provider.ToProviderType()

// ❌ WRONG - Using the string code
ProviderCode = result.Provider.Code

// ✅ CORRECT - Use Currency enum  directly
Currency = entity.Currency

// ❌ WRONG - Converting enum to string
Currency = entity.Currency.ToString()
```

### Read Models and Aggregates

Even in read models and database aggregations, use enums:

```csharp
// ✅ CORRECT
public sealed record ReceiptProviderGrouping(
    LotteryProviderType ProviderType,
    string ProviderName,
    int LineItemCount,
    decimal TotalAmount,
    decimal Percentage);

// ❌ WRONG
public sealed record ReceiptProviderGrouping(
    string ProviderCode,  // NO!
    string ProviderName,
    int LineItemCount,
    decimal TotalAmount,
    decimal Percentage);
```

### Configuration Classes

Configuration classes that deserialize from JSON/appsettings MAY use `string` for enum values since they need to parse from configuration:

```csharp
// ✅ ACCEPTABLE for config classes only
internal sealed class RootAccountConfig
{
    public string Currency { get; set; } = "MYR";  // OK - config deserialization
}

// Then parse to enum in the seeder/service
var currency = Enum.Parse<Currency>(config.Currency, true);
```

## Frontend (TypeScript/Angular)

### Generated API Types

The frontend API client auto-generates proper TypeScript union types from backend enums:

```typescript
// ✅ Auto-generated from backend
export type Currency = 'MYR' | 'SGD' | 'IDR';
export type LotteryProviderType = 'Magnum' | 'PMP' | 'Toto' | 'Singapore' | 'Sabah' | 'Sandakan' | 'Sarawak' | 'GD' | 'NineLotto';

// ✅ CORRECT - Use the generated type
interface AccountInfo {
  currency: Currency;
  providerType: LotteryProviderType;
}

// ❌ WRONG - Never use string for enum values
interface AccountInfo {
  currency: string;  // NO!
  providerType: string;  // NO!
}
```

### Component Properties and State

```typescript
// ✅ CORRECT
@Component({...})
export class MyComponent {
  selectedCurrency: Currency = 'MYR';
  provider: LotteryProviderType = 'Magnum';
}

// ❌ WRONG
@Component({...})
export class MyComponent {
  selectedCurrency: string = 'MYR';  // NO!
  provider: string = 'Magnum';       // NO!
}
```

### Store/Facade State

```typescript
// ✅ CORRECT
export interface FilterState {
  currency: Currency | null;
  providerCodes: LotteryProviderType[];
}

// ❌ WRONG
export interface FilterState {
  currency: string | null;   // NO!
  providerCodes: string[];   // NO!
}
```

### Zod Schema Validation

When using Zod for runtime validation, use `z.enum()` with const arrays that match the generated enum types:

```typescript
// ✅ CORRECT - Enum validation with Zod
const lotteryProviderTypes = ['Magnum', 'PMP', 'Toto', 'Singapore', 'Sabah', 'Sandakan', 'Sarawak', 'GD', 'NineLotto'] as const;

export const accountSchema = z.object({
  allowedProviders: z.array(z.enum(lotteryProviderTypes)).nullish().transform(v => v ?? []),
  // Inferred type is: LotteryProviderType[]
});

// ✅ CORRECT - Single enum value
export const intakeLimitSchema = z.object({
  betType: z.enum(apiBetTypeCodes),
  // Inferred type is: BetType
});

// ❌ WRONG - Using string array
export const accountSchema = z.object({
  allowedProviders: z.array(z.string()).nullish().transform(v => v ?? []),
  // NO! No type safety, accepts any string
});
```

**Pattern**: Define enum value arrays as const, then use `z.enum()` for validation. This ensures:
- Runtime validation rejects invalid values
- Inferred TypeScript types match the API enum types
- Type safety throughout the facade layer

## Why This Matters

1. **Type Safety**: Enums prevent invalid values at compile-time
2. **IntelliSense**: IDEs provide autocomplete for enum values
3. **Refactoring**: Renaming an enum value updates all usages
4. **Documentation**: Enum types self-document valid values
5. **Validation**: Automatic validation of enum values
6. **API Contract**: OpenAPI spec generates proper enum constraints

## Code Review Checklist

Before submitting code, verify:

- [ ] No `string` properties where an enum should be used
- [ ] All provider references use `LotteryProviderType`
- [ ] All currency references use `Currency` enum
- [ ] DTOs use enum types, not strings
- [ ] Query handlers map domain objects to enum types properly
- [ ] Frontend components use generated union types

## Exception: Display/Serialization

The ONLY acceptable string conversion is for display or final serialization:

```csharp
// ✅ CORRECT - Final display rendering
var displayText = currency.ToString();  // OK for UI display

// ✅ CORRECT - Internal processing with enum
public Currency Currency { get; init; }  // Must be enum type

// ❌ WRONG - Storing/passing as string
public string Currency { get; init; }  // NO!
```

## Enforcement

This rule is CRITICAL and non-negotiable:

- All code reviews MUST verify enum usage
- Backend MUST compile without enum-to-string conversions
- Frontend TypeScript MUST use generated union types
- Any violation of this rule MUST be fixed immediately

**Remember: Strings for enums are a code smell and indicate missing type safety.**
