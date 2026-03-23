---
description: 'Angular-specific coding standards and best practices'
applyTo: '**/*.ts, **/*.html, **/*.scss, **/*.css'
---

# Angular Development Instructions

Instructions for generating high-quality Angular applications with TypeScript, using Angular Signals for state management, adhering to Angular best practices as outlined at https://angular.dev.

## Project Context
- Latest Angular version (use standalone components by default)
- TypeScript for type safety
- Angular CLI for project setup and scaffolding
- Follow Angular Style Guide (https://angular.dev/style-guide)
- Use Angular Material or other modern UI libraries for consistent styling (if specified)

## ⚠️ MANDATORY: Use Bun Package Manager

**This is a hard requirement, not optional.** All package management and script execution MUST use `bun` instead of `npm`.

### Command Mappings

| ❌ Do NOT use | ✅ Use instead |
|---------------|----------------|
| `npm install` | `bun install` |
| `npm install <pkg>` | `bun add <pkg>` |
| `npm install -D <pkg>` | `bun add -d <pkg>` |
| `npm uninstall <pkg>` | `bun remove <pkg>` |
| `npm run <script>` | `bun run <script>` |
| `npm test` | `bun test` |
| `npm ci` | `bun install --frozen-lockfile` |
| `npx <command>` | `bunx <command>` |

### Angular CLI with Bun

```bash
# Starting dev server
bun run start
bun run ng serve

# Building the application
bun run build
bun run ng build

# Running tests
bun test
bun run ng test

# Generating components/services
bunx ng generate component my-component
bunx ng g s my-service

# Adding packages
bun add @angular/material
bun add -d @types/some-lib
```

### Why Bun?
- Significantly faster package installation
- Native TypeScript execution
- Compatible with npm packages and lockfiles
- Better developer experience with unified tooling

## Development Standards

### Architecture
- Use standalone components unless modules are explicitly required
- Organize code by standalone feature modules or domains for scalability
- Implement lazy loading for feature modules to optimize performance
- Use Angular's built-in dependency injection system effectively
- Structure components with a clear separation of concerns (smart vs. presentational components)

### TypeScript
- Enable strict mode in `tsconfig.json` for type safety
- Define clear interfaces and types for components, services, and models
- Use type guards and union types for robust type checking
- Implement proper error handling with RxJS operators (e.g., `catchError`)
- Use typed forms (e.g., `FormGroup`, `FormControl`) for reactive forms
- Never use strings as enum values—always use proper TypeScript enums

### ⚠️ MANDATORY: Prefer `undefined` over `null`

**`null` is banned in frontend code.** Always use `undefined` for absent values.

#### Rules
- Do NOT use `null` as a value — use `undefined` instead
- Do NOT use `T | null` in type annotations — use `T | undefined` instead
- Convert external APIs that return `null` at the boundary (e.g., `localStorage.getItem(key) ?? undefined`)

#### ❌ BAD
```typescript
const errorMessage = signal<string | null>(null);
errorMessage.set(null);

if (value === null) { ... }

private timer: ReturnType<typeof setTimeout> | null = null;
```

#### ✅ GOOD
```typescript
const errorMessage = signal<string | undefined>(undefined);
errorMessage.set(undefined);

if (value === undefined) { ... }

private timer: ReturnType<typeof setTimeout> | undefined = undefined;

// Convert external null APIs at the boundary
const stored = localStorage.getItem('key') ?? undefined;
```

### ⚠️ MANDATORY: No `as` type assertions

**Type assertions using `as` are banned.** They bypass TypeScript's type system and can hide bugs. Instead, use proper type transformations.

#### ❌ BAD: Using `as` casting
```typescript
// ❌ Type assertion - dangerous, bypasses type checking
const providers = store.selectedProviders() as LotteryProviderType[];

// ❌ API response casting - loses type safety
const data = response.items as ItemDto[];

// ❌ Form value casting - can cause runtime errors
const formData = this.form.value as CreateAccountRequest;
```

#### ✅ GOOD: Use transformer methods
```typescript
// ✅ Validate and transform with type guards
const providers = store.selectedProviders()
    .filter((p): p is LotteryProviderType => isLotteryProviderType(p));

// ✅ Use transformer in facade layer
// In transformer.ts:
toIntakeLimits(dtos: IntakeLimitDto[]): IntakeLimitEntry[] {
    return dtos.map(dto => ({
        betType: dto.betType, // Already typed from API
        providerType: dto.providerType,
        enabled: dto.enabled,
        maxAmount: dto.maxAmount
    }));
}

// ✅ Use Zod schema for form validation
const result = CreateAccountSchema.safeParse(this.form.value);
if (result.success) {
    const formData = result.data; // Properly typed
}
```

#### When Type Assertion is Acceptable
- After a type guard that narrows the type
- For well-typed test mocks (test files are exempt from this rule)
- When the API contract is guaranteed (with clear documentation)

### ⚠️ MANDATORY: Strong Typing — No `any`

**`any` is banned in application code.** Always use proper types, generics, or `unknown` with type guards.

```typescript
// ❌ BAD
function process(data: any) { ... }
const items: any[] = response.data;

// ✅ GOOD
function process(data: unknown) { ... }
function process<T extends Record<string, unknown>>(data: T) { ... }
const items: ItemDto[] = response.data;
```

> ⚡ **Key principle:** If you need `as`, ask yourself: "Can I use a transformer function instead?"

### Component Design
- Follow Angular's component lifecycle hooks best practices
- When using Angular >= 19, Use `input()` `output()`, `viewChild()`, `viewChildren()`, `contentChild()` and `contentChildren()` functions instead of decorators; otherwise use decorators
- Leverage Angular's change detection strategy (default or `OnPush` for performance)
- Keep templates clean and logic in component classes or services
- Use Angular directives and pipes for reusable functionality

### Self-Contained Atomic Components

**Core Principle:** Components that require data from APIs should be self-loading. The component is responsible for fetching its own data rather than receiving pre-loaded data from parent components.

#### Why Self-Contained?
- **Reusability**: Drop the component anywhere—it just works
- **Encapsulation**: Data loading logic stays with the component that needs it
- **Simpler Parents**: Parent components focus on composition, not data orchestration
- **Testability**: Component's data layer is co-located and easily mockable

#### Pattern: Component + Store

Each self-loading component pairs with a dedicated signal store that manages:
- Data fetching via facade layer
- Loading/error states
- Local state mutations

```typescript
// prize-packages-form.store.ts
export const PrizePackagesFormStore = signalStore(
    withState<PrizePackagesFormState>(initialState),
    withComputed((store) => ({ /* derived state */ })),
    withMethods((store, facade = inject(AccountFacade)) => ({
        async loadPackages(accountId: string): Promise<void> {
            patchState(store, { loading: true, error: null });
            const result = await facade.getPrizePackages(accountId);
            // handle result...
        }
    }))
);

// prize-packages-form.component.ts
@Component({
    providers: [PrizePackagesFormStore], // Per-instance store
    // ...
})
export class PrizePackagesFormComponent implements OnInit {
    readonly accountId = input<string | null>(null);
    protected readonly store = inject(PrizePackagesFormStore);

    ngOnInit(): void {
        const id = this.accountId();
        if (id) {
            this.store.loadPackages(id); // Self-loading on init
        }
    }
}
```

#### When to Use
- ✅ Form components that need reference data (dropdowns, selectors)
- ✅ Detail views that load entity data by ID
- ✅ Embedded sub-forms within larger forms
- ✅ Reusable widgets that appear in multiple contexts

#### Implementation Checklist
1. Create a dedicated store file: `feature.store.ts`
2. Provide store at component level (not root): `providers: [FeatureStore]`
3. Load data in `ngOnInit()` based on input signals
4. Expose loading/error states from store to template
5. Emit results via `output()` for parent coordination

> 📖 **Reference implementation:** `prize-packages-form.component.ts` and `prize-packages-form.store.ts` in `features/account/prize-packages/`

### Styling
- Use Angular's component-level CSS encapsulation (default: ViewEncapsulation.Emulated)
- Prefer SCSS for styling with consistent theming
- Implement responsive design using CSS Grid, Flexbox, or Angular CDK Layout utilities
- Follow Angular Material's theming guidelines if used
- Maintain accessibility (a11y) with ARIA attributes and semantic HTML

### State Management
- Use Angular Signals for reactive state management in components and services
- Leverage `signal()`, `computed()`, and `effect()` for reactive state updates
- Use writable signals for mutable state and computed signals for derived state
- Handle loading and error states with signals and proper UI feedback
- Use Angular's `AsyncPipe` to handle observables in templates when combining signals with RxJS

### Data Fetching & Facade Architecture
- **All API calls go through the facade layer** - never call generated API services directly from components
- Use Angular's `inject()` function for dependency injection in standalone components
- Implement caching strategies (e.g., `shareReplay` for observables)
- Store API response data in signals for reactive updates
- Handle API errors with global interceptors for consistent error handling

The facade layer is the **single point of entry** for all API interactions. It provides DTO→Domain Model conversion, Zod validation, localized display names, and branded types for type safety.

> 📖 **For detailed implementation patterns, code examples, and directory structure, see the [Facade Architecture Guide](../../docs/frontend/facade-architecture-guide.md).**

### Security
- Sanitize user inputs using Angular's built-in sanitization
- Implement route guards for authentication and authorization
- Use Angular's `HttpInterceptor` for CSRF protection and API authentication headers
- Validate form inputs with Angular's reactive forms and custom validators
- Follow Angular's security best practices (e.g., avoid direct DOM manipulation)

### Performance
- Enable production builds with `bun run build` (or `bun run ng build --configuration=production`) for optimization
- Use lazy loading for routes to reduce initial bundle size
- Optimize change detection with `OnPush` strategy and signals for fine-grained reactivity
- Use trackBy in `ngFor` loops to improve rendering performance
- Implement server-side rendering (SSR) or static site generation (SSG) with Angular Universal (if specified)

### Testing
- Write unit tests for components, services, and pipes using Jasmine and Karma
- Use Angular's `TestBed` for component testing with mocked dependencies
- Test signal-based state updates using Angular's testing utilities
- Write end-to-end tests with Cypress or Playwright (if specified)
- Mock HTTP requests using `provideHttpClientTesting`
- Ensure high test coverage for critical functionality

## Implementation Process
1. Plan project structure and feature modules
2. Define TypeScript interfaces and models
3. Scaffold components, services, and pipes using Angular CLI
4. Implement data services and API integrations with signal-based state
5. Build reusable components with clear inputs and outputs
6. Add reactive forms and validation
7. Apply styling with SCSS and responsive design
8. Implement lazy-loaded routes and guards
9. Add error handling and loading states using signals
10. Write unit and end-to-end tests
11. Optimize performance and bundle size

## Pre-Commit Quality Checks

Before considering any task complete, verify:

1. **TypeScript compilation** — `bunx tsc --noEmit` must introduce zero new errors
2. **Linting** — `bun run lint` (or `bunx ng lint`) must pass with no errors or warnings
3. If either check reveals issues caused by your changes, fix them before finishing

## Additional Guidelines
- Follow the Angular Style Guide for file naming conventions (see https://angular.dev/style-guide), e.g., use `feature.ts` for components and `feature-service.ts` for services. For legacy codebases, maintain consistency with existing pattern.
- Use Angular CLI commands for generating boilerplate code
- Document components and services with clear JSDoc comments
- Ensure accessibility compliance (WCAG 2.1) where applicable
- Use Angular's built-in i18n for internationalization (if specified)
- Keep code DRY by creating reusable utilities and shared modules
- Use signals consistently for state management to ensure reactive updates