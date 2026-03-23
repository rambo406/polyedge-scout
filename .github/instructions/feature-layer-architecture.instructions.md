---
description: 'Feature-layer architecture patterns for Angular services, models, and transformers with type-safe factory parsing'
applyTo: 'frontend/src/app/features/**/*.ts'
---

# Feature Layer Architecture

Guidelines for organizing Angular code into feature-based folders with services, models, transformers, and type-safe factory parsing patterns.

## Project Context

- Framework: Angular 18+
- Validation: Zod for runtime type validation
- Pattern: Feature-first organization over layer-first

## Feature Folder Structure

Each feature module follows this structure:

```
features/{feature}/
├── {feature}.routes.ts           # Route definitions
├── services/
│   ├── index.ts                  # Barrel export
│   └── {domain}.service.ts       # Domain services (not facades)
├── models/
│   ├── index.ts                  # Barrel export
│   ├── {entity}.model.ts         # Domain model (one per file)
│   ├── {entity}.schema.ts        # Zod schema (co-located)
│   ├── {entity}.parsers.ts       # Factory parsers (co-located)
│   └── {domain}-commands.ts      # Command/request interfaces
├── transformers/
│   ├── index.ts                  # Barrel export
│   └── {domain}.transformer.ts   # DTO→Model transformers
├── stores/                       # Signal stores
├── components/                   # Feature components
└── utils/                        # Feature utilities
```

## Naming Conventions

### Files

| Pattern | Example |
|---------|---------|
| Service | `account.service.ts` |
| Model | `account.model.ts` |
| Schema | `account.schema.ts` |
| Parsers | `account.parsers.ts` |
| Commands | `account-commands.ts` |
| Transformer | `account.transformer.ts` |

### Classes

| Pattern | Example |
|---------|---------|
| Service | `AccountService` |
| Model | `Account` (class or interface) |
| Transformer | `AccountTransformer` |

### Avoid

- `*Facade` suffix - Use `*Service` instead
- Multi-type model files - One model per file
- `*.models.ts` - Use `*-commands.ts` for command types

## Service Pattern

Services wrap generated API clients and provide domain-specific operations:

```typescript
// Good: Feature service
@Injectable({ providedIn: 'root' })
export class AccountService {
  private readonly api = inject(AccountsApiService);
  private readonly transformer = inject(AccountTransformer);

  async getAccount(id: string): Promise<Account> {
    const dto = await this.api.getAccount({ id });
    return this.transformer.toModel(dto);
  }
}
```

## Model Organization

### One Model Per File

```typescript
// Good: models/account.model.ts
export interface AccountProps {
  readonly id: string;
  readonly loginId: string;
  readonly status: AccountStatus;
}

export class Account {
  constructor(private readonly props: AccountProps) {}
  // ...methods
}
```

### Command Types Separate

```typescript
// Good: models/account-commands.ts
export interface CreateAccountCommand {
  loginId: string;
  password: string;
  fullName: string;
}

export interface UpdateAccountCommand {
  fullName?: string;
  email?: string;
}
```

## Factory Parsing Pattern

Replace TypeScript `as` casts with Zod-based factory parsers for type safety.

### Parser File Structure

```typescript
// models/{entity}.parsers.ts
import { z } from 'zod';

// 1. Define schema from literal values
export const currencySchema = z.enum(['MYR', 'SGD', 'THB', 'IDR']);
export type Currency = z.infer<typeof currencySchema>;

// 2. Strict parser (throws ZodError on invalid)
export function parseCurrency(value: unknown): Currency {
  return currencySchema.parse(value);
}

// 3. Safe parser with fallback
export function parseCurrencySafe(value: unknown, fallback: Currency = 'MYR'): Currency {
  const result = currencySchema.safeParse(value);
  return result.success ? result.data : fallback;
}

// 4. Type guard
export function isCurrency(value: unknown): value is Currency {
  return currencySchema.safeParse(value).success;
}
```

### Usage in Transformers

```typescript
// Bad: Unsafe cast
const currency = dto.currency as Currency;

// Good: Validated parsing
const currency = parseCurrency(dto.currency);

// Good: Safe parsing with fallback
const currency = parseCurrencySafe(dto.currency, 'MYR');

// Good: Type guard for conditional logic
if (isCurrency(dto.currency)) {
  // dto.currency is now typed as Currency
}
```

### When to Use Each Parser

| Function | Use When |
|----------|----------|
| `parse*()` | Data must be valid, throw on invalid |
| `parse*Safe()` | Need fallback for optional/defaultable fields |
| `is*()` | Conditional branching based on type |

## Transformer Pattern

Transformers convert API DTOs to domain models:

```typescript
@Injectable({ providedIn: 'root' })
export class AccountTransformer {
  toModel(dto: AccountDto): Account {
    // Use Zod schema for null coalescing and validation
    const parsed = accountDtoSchema.parse(dto);
    
    return new Account({
      id: parsed.id,
      loginId: parsed.loginId,
      status: parseAccountStatus(parsed.status), // Factory parser
      currency: parseCurrency(parsed.currency),   // Factory parser
    });
  }

  toModels(dtos: AccountDto[]): Account[] {
    return dtos.map(dto => this.toModel(dto));
  }
}
```

## Import Paths

Use path aliases for clean imports:

```typescript
// Good: Feature imports
import { AccountService } from '@features/account/services';
import { Account, AccountStatus } from '@features/account/models';
import { AccountTransformer } from '@features/account/transformers';

// Good: Core imports
import { BaseService } from '@core/services';
import { Currency, parseCurrency } from '@core/reference/models';

// Bad: Relative paths crossing feature boundaries
import { SomeService } from '../../../other-feature/services';
```

## Barrel Exports

Each folder needs an `index.ts` barrel export:

```typescript
// services/index.ts
export { AccountService } from './account.service';
export { SubUserService } from './sub-user.service';

// models/index.ts
export * from './account.model';
export * from './account.schema';
export * from './account.parsers';
export * from './account-commands';
```

## Common Patterns

### Enum-like Types with Parsers

```typescript
// models/account-status.model.ts
export const ACCOUNT_STATUS_CODES = ['Active', 'Suspended', 'Closed'] as const;
export type AccountStatusCode = typeof ACCOUNT_STATUS_CODES[number];

// models/account-status.parsers.ts
import { z } from 'zod';
import { ACCOUNT_STATUS_CODES, AccountStatusCode } from './account-status.model';

export const accountStatusSchema = z.enum(ACCOUNT_STATUS_CODES);

export function parseAccountStatus(value: unknown): AccountStatusCode {
  return accountStatusSchema.parse(value);
}

export function parseAccountStatusSafe(
  value: unknown, 
  fallback: AccountStatusCode = 'Active'
): AccountStatusCode {
  const result = accountStatusSchema.safeParse(value);
  return result.success ? result.data : fallback;
}
```

### Co-located Schema with Model

```typescript
// models/account.schema.ts
import { z } from 'zod';
import { stringOrEmpty, numberOrZero } from '@core/schemas/primitives';
import { accountStatusSchema } from './account-status.parsers';

export const accountDtoSchema = z.object({
  id: stringOrEmpty,
  loginId: stringOrEmpty,
  fullName: stringOrEmpty,
  status: accountStatusSchema,
  creditLimit: numberOrZero,
});

export type ParsedAccountDto = z.infer<typeof accountDtoSchema>;
```

### Form Domain Model Pattern

For complex forms, create a form-focused domain model that bridges API data and Angular FormGroup:

```typescript
// models/{feature}-form.model.ts

// 1. Supporting value interfaces
export interface PositionTakingFormValues {
  readonly regular: { fourD3D: number; fiveD6D: number; twoD: number };
  readonly grandDragon: { fourD3D: number; fiveD6D: number; twoD: number };
}

// 2. Props interface for construction
export interface AccountFormModelProps {
  readonly mode: 'create' | 'edit';
  readonly accountId: string | null;
  readonly loginId: string;
  readonly fullName: string;
  readonly currency: Currency;
  readonly positionTaking: PositionTakingFormValues;
  // ... other editable values
}

// 3. Immutable domain model
export class AccountFormModel {
  readonly mode: 'create' | 'edit';
  readonly loginId: string;
  // ... readonly properties

  constructor(props: AccountFormModelProps) {
    this.mode = props.mode;
    this.loginId = props.loginId;
    // ... freeze nested objects for immutability
    this.positionTaking = Object.freeze({...props.positionTaking});
  }

  // 4. Form value extraction methods
  toFormValues(): AccountFormValues {
    return {
      loginId: this.loginId,
      fullName: this.fullName,
      // Maps model to FormGroup-compatible values
    };
  }

  toStakePercentagesFormValues(): StakePercentagesFormValues {
    return {
      regular: {
        fourD3D: this.positionTaking.regular.fourD3D.toString(),
        // Convert numbers to strings for form inputs
      }
    };
  }

  // 5. Immutable update methods
  withPositionTaking(values: PositionTakingFormValues): AccountFormModel {
    return new AccountFormModel({
      ...this.toProps(),
      positionTaking: values
    });
  }

  // 6. Factory methods
  static createMode(): AccountFormModel {
    return new AccountFormModel({ mode: 'create', /* defaults */ });
  }
}
```

#### Usage in Store

Add a computed signal that derives the form model from loaded API data:

```typescript
// store.ts
withComputed((store) => ({
  formModel: computed((): AccountFormModel | null => {
    const data = store.loadedData();
    if (!data) return null;
    return new AccountFormModel({
      mode: store.mode(),
      accountId: data.id,
      loginId: data.loginId,
      // ... map from API data to form model
    });
  }),
}))
```

#### Usage in Component

```typescript
// component.ts
protected readonly formModel = computed(() => this.store.formModel());

constructor() {
  effect(() => {
    const model = this.formModel();
    if (model) {
      this.accountForm.patchValue(model.toFormValues());
      this.stakePercentagesForm.patchValue(model.toStakePercentagesFormValues());
    }
  });
}
```

#### When to Use

- Complex forms with multiple sections
- Forms that need to map from rich API responses
- Forms stored in signal state
- When separation between API data and form state is needed

## Validation

- Build: `ng build --configuration production`
- Lint: `ng lint`
- Test: `ng test`
- Type check: `npx tsc --noEmit`
