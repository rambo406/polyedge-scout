---
description: 'Guidelines for working with RxJS Observables and Promises in the frontend codebase'
applyTo: 'frontend/src/app/**/*.ts'
---

# Observable and Promise Usage Patterns

## Core Principle

**Services return Observables. Consumers convert to Promises only when needed.**

The feature services layer (and most API wrappers) return `Observable<T>` types to support reactive programming patterns. This enables powerful composition, cancellation, and subscription management.

## Service Layer: Return Observables

✅ **DO: Return Observable from services**

```typescript
@Injectable({ providedIn: 'root' })
export class BetsService {
    private readonly api = inject(BetsApiService);

    parseBetText(command: ParseBetTextCommand): Observable<ParseResult> {
        return this.api.parseBetText({
            input: command.input,
            mode: command.mode ?? 'Express',
        }).pipe(
            map(dto => transformParseResult(dto))
        );
    }
}
```

✅ **DO: Return Observable from API services**

```typescript
@Injectable({ providedIn: 'root' })
export class AuthApiService extends BaseApiService {
    protected override readonly basePath = '/api/auth';

    login(request: LoginRequest): Observable<LoginResponse> {
        return this.post<LoginResponse>('/login', request);
    }

    getCurrentUser(): Observable<CurrentUserResponse> {
        return this.get<CurrentUserResponse>('/me');
    }
}
```

❌ **DON'T: Return Promise from services**

```typescript
// WRONG - services should not return Promise
parseBetText(command: ParseBetTextCommand): Promise<ParseResult> {
    return firstValueFrom(this.api.parseBetText(...));
}

// WRONG - API services should not return Promise
login(request: LoginRequest): Promise<LoginResponse> {
    return this.post<LoginResponse>('/login', request);
}
```

**Rationale:** Observables provide:
- Lazy evaluation (don't execute until subscribed)
- Cancellation support (unsubscribe)
- Composition operators (map, filter, switchMap, etc.)
- Multiple subscribers
- Integration with Angular's async pipe

## Consumer Layer: Convert When Needed

### Components with async/await

When using `async/await` in component methods, convert Observable to Promise with `firstValueFrom`:

✅ **DO: Use firstValueFrom for await**

```typescript
async loadSchedule(): Promise<void> {
    this.loading.set(true);
    try {
        const schedule = await firstValueFrom(this.service.getSchedule(this.accountId()));
        // Use schedule...
    } catch (err) {
        this.error.set(this.extractError(err));
    } finally {
        this.loading.set(false);
    }
}
```

❌ **DON'T: Use await directly on Observable**

```typescript
// WRONG - await on Observable doesn't work as expected
const schedule = await this.service.getSchedule(this.accountId());
```

### Components with Reactive Patterns

When possible, prefer reactive patterns over async/await:

✅ **DO: Use async pipe in templates**

```typescript
// Component
schedule$ = this.service.getSchedule(this.accountId());

// Template
<div *ngIf="schedule$ | async as schedule">
    {{ schedule.frequency }}
</div>
```

✅ **DO: Use RxJS operators for transformations**

```typescript
schedule$ = this.service.getSchedule(this.accountId()).pipe(
    map(schedule => this.transformSchedule(schedule)),
    catchError(err => {
        this.error.set(this.extractError(err));
        return of(null);
    })
);
```

### Stores with rxMethod

When using NgRx Signal Store's `rxMethod`, work with Observables directly:

✅ **DO: Use switchMap with Observables**

```typescript
initiateTransfer: rxMethod<InitiateBbfTransferCommand>(
    pipe(
        tap(() => patchState(store, { loading: true })),
        switchMap((command) =>
            facade.initiateTransfer(command).pipe(  // No from() wrapper needed
                switchMap(() => {
                    const loadAll$ = facade.getTransfers({...});
                    const loadPending$ = facade.getPendingTransfers({...});
                    
                    return forkJoin([loadAll$, loadPending$]).pipe(
                        tap(([allResult, pendingResult]) => {
                            patchState(store, { ... });
                        })
                    );
                })
            )
        )
    )
)
```

❌ **DON'T: Wrap Observables in from()**

```typescript
// WRONG - from() is for Promises/arrays, not Observables
switchMap((command) =>
    from(facade.initiateTransfer(command)).pipe(...)
)
```

❌ **DON'T: Use Promise.all with Observables**

```typescript
// WRONG - use forkJoin instead
return from(Promise.all([loadAll$, loadPending$]));
```

### Resolvers

Angular Route Resolvers can return Observable or Promise. Prefer Observable:

✅ **DO: Return Observable from resolver**

```typescript
export const prizePackagesResolver: ResolveFn<PrizePackage[]> = (route) => {
    const facade = inject(AccountFacade);
    const accountId = route.paramMap.get('id');
    
    if (!accountId) return of([]);
    
    return facade.getPrizePackages(accountId);
};
```

✅ **ACCEPTABLE: Convert to Promise if needed**

```typescript
export const prizePackagesResolver: ResolveFn<PrizePackage[]> = async (route) => {
    const facade = inject(AccountFacade);
    const accountId = route.paramMap.get('id');
    
    if (!accountId) return [];
    
    try {
        return await firstValueFrom(facade.getPrizePackages(accountId));
    } catch (err) {
        console.error('Failed to load prize packages:', err);
        return [];
    }
};
```

## Common Patterns

### Multiple Concurrent Requests

Use `forkJoin` for concurrent Observables (like `Promise.all`):

```typescript
forkJoin([
    this.service.getAccounts(),
    this.service.getSettings(),
    this.service.getPackages()
]).subscribe(([accounts, settings, packages]) => {
    // All three completed
});
```

### Sequential Requests

Use `switchMap` or `concatMap` for sequential dependencies:

```typescript
this.service.createAccount(data).pipe(
    switchMap(account => this.service.loadPermissions(account.id)),
    tap(permissions => console.log('Account created and permissions loaded'))
).subscribe();
```

### Error Handling

Handle errors with `catchError` operator:

```typescript
this.service.getData().pipe(
    catchError(err => {
        console.error('Failed to load data:', err);
        return of(null);  // Return fallback value
    })
).subscribe();
```

## Type Safety

### Handling Nullable Types

API types often have nullable fields. Handle them appropriately:

```typescript
// API returns: { displayName: string | null }
const user = await firstValueFrom(this.api.getCurrentUser());

// Handle null explicitly
const name: string = user.displayName ?? '';

// Or use optional chaining
if (user.displayName) {
    this.form.patchValue({ name: user.displayName });
}
```

### DTO to Domain Model Transformation

Always transform DTOs to domain models in services:

```typescript
// Service layer
getUsers(): Observable<User[]> {
    return this.api.getUsers().pipe(
        map(dtos => User.fromDtoArray(dtos))
    );
}

// Consumer gets domain models
const users = await firstValueFrom(this.service.getUsers());
// users is User[], not UserDto[]
```

## Import Statements

Always import RxJS operators and functions explicitly:

```typescript
import { firstValueFrom, forkJoin, Observable } from 'rxjs';
import { map, switchMap, tap, catchError } from 'rxjs/operators';
```

## Migration from Promise to Observable

If you encounter a service that returns Promise but should return Observable:

1. **Check if it wraps an Observable-returning API**
   - If yes, remove the Promise wrapper and return the Observable directly

2. **Update consumers to use firstValueFrom if they need async/await**
   - Add `await firstValueFrom(service.method())` instead of `await service.method()`

3. **Update tests**
   - Change from `.then()` to `.subscribe()` or use Jasmine's `done()` callback

## Script for Automatic Fixes

A Python script is available to help migrate incorrectly used Observables:

```bash
python scripts/fix-observable-consumers.py
```

This script:
- Wraps `await service.method()` with `firstValueFrom()`
- Replaces `Promise.all` with `forkJoin` when appropriate
- Adds necessary imports

Manual review is still recommended after running the script.
