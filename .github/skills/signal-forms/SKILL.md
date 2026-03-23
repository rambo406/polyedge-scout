---
name: signal-forms
description: 'Implement and migrate Angular Signal Forms in this repo. Use when asked to "migrate Reactive Forms to Signal Forms", "replace formControl/formGroup with [formField]", "add schema validators (required/min/max)", "wire submit()", "integrate z-icon-input/custom CVA controls", or "debug disabled/touched/dirty validation behavior" in standalone Angular components.'
---

# Signal Forms

Implementation-focused guide for Angular 21 Signal Forms in this repo’s standalone-component frontend.

**Primary source:** <https://angular.dev/essentials/signal-forms>

> ⚠️ Signal Forms are experimental in Angular. Verify API changes when upgrading Angular.

## When to use this skill

- Building new forms in standalone components (`imports: [FormField, ...]`).
- Migrating existing `ReactiveFormsModule`/`FormGroup` forms to signal-based forms.
- Wiring custom input controls (for example `z-icon-input`) to `[formField]`.
- Fixing form-state bugs around `disabled()`, `touched()`, `dirty()`, errors, and submit flow.

## Core APIs to use

- `form(modelSignal, schemaOrSchemaFn)` → creates `FieldTree`
- `schema((path) => { ... })` → reusable schema definition
- `required(path.field)`, `min(path.field, n)`, `max(path.field, n)` → built-in validators
- `submit(fieldTree, action)` → async submit with server-side validation errors
- `[formField]="myForm.fieldName"` → template binding for controls

### Minimal standalone pattern

```ts
import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { FormField, form, schema, required, min, max, submit } from '@angular/forms/signals';
import { IconInputComponent } from '@shared/components/primitives/form-inputs/icon-input/icon-input.component';

interface PackageFormModel {
  commissionPercent: number;
  priceMarkup: number;
}

@Component({
  selector: 'app-package-form',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormField, IconInputComponent],
  templateUrl: './package-form.component.html',
})
export class PackageFormComponent {
  private readonly model = signal<PackageFormModel>({ commissionPercent: 0, priceMarkup: 0 });

  private readonly packageSchema = schema<PackageFormModel>((path) => {
    required(path.commissionPercent);
    min(path.commissionPercent, 0);
    max(path.commissionPercent, 100);
    required(path.priceMarkup);
    min(path.priceMarkup, 0);
  });

  readonly packageForm = form(this.model, this.packageSchema);

  protected async onSubmit(event: Event): Promise<void> {
    event.preventDefault();

    await submit(this.packageForm, async (f) => {
      // Use f().value() for latest payload
      // Return validation errors from server when needed, else undefined
      await Promise.resolve(f().value());
      return undefined;
    });
  }
}
```

```html
<form (submit)="onSubmit($event)" class="space-y-4">
  <z-icon-input [formField]="packageForm.commissionPercent" type="number" suffixIcon="percent" />

  @if (packageForm.commissionPercent().touched() && !packageForm.commissionPercent().valid()) {
    @for (error of packageForm.commissionPercent().errors(); track error.kind) {
      <p class="text-destructive text-xs">{{ error.message }}</p>
    }
  }

  <button z-button type="submit" [disabled]="!packageForm().valid()">Save</button>
</form>
```

## Migration workflow (Reactive Forms → Signal Forms)

1. **Model first**
   - Convert `FormGroup` shape into a typed signal model (`signal<MyFormModel>(...)`).
2. **Replace form construction**
   - Move `new FormGroup(...)` / `fb.group(...)` to `form(model, schemaOrFn)`.
3. **Move validators**
   - Translate validators to schema rules (`required`, `min`, `max`, etc.).
4. **Template rebinding**
   - Replace `formControl`, `formControlName`, `formGroup` with `[formField]` bindings.
5. **State reads**
   - Replace `.value`, `.valid`, `.errors`, `.touched`, `.dirty` with signal reads:
     - `field().value()`
     - `field().valid()` / `field().errors()`
     - `field().touched()` / `field().dirty()`
6. **Submit path**
   - Use `(submit)` + `submit(form, async action)` or explicit `if (!form().valid()) return` guard.
7. **Conditional disabling**
   - Move imperative `control.disable()` logic to schema rules with `disabled(path.field, () => condition())`.
8. **Tests**
   - Update component tests to assert `[formField]` bindings and signal-form validity behavior.

## Validation and template patterns

- Show validation errors only after interaction (`touched()` or `dirty()`), not immediately on load.
- Prefer `field().errors()` rendering with `@for` so server/client errors share one display path.
- Keep submit button rule simple: disabled when `!form().valid()` or when async save is running.

## CVA + custom control troubleshooting (`z-icon-input` style)

1. **Control is not updating form value**
   - Ensure CVA calls `onChange(value)` on every user input.
2. **Touched never becomes true**
   - Ensure blur events call `onTouched()` in CVA wrapper/inner input chain.
3. **Disabled state not reflected in UI**
   - Ensure CVA implements `setDisabledState()` and forwards disabled to actual input element.
4. **Dirty/touched feels inconsistent**
   - Remember `dirty()` tracks user edits; prefill programmatically during init before relying on dirty-based UX.
5. **Number fields become strings**
   - If CVA emits string values for numeric fields, normalize before submit or in the CVA implementation.
6. **Double-binding conflicts**
   - Avoid mixing manual `[value]`/`(input)` with `[formField]` on the same element/component.

## Anti-patterns to avoid

- **Mixing form APIs in one template** (`formGroup`/`formControlName` plus `[formField]`) → use one API per component and complete the migration to Signal Forms.
- **Keeping NgModule-era form imports in standalone components** → import `FormField` (and only required dependencies) in `imports: [...]` and remove unused forms modules.
- **Skipping `event.preventDefault()` in native form submit handlers** → always prevent default and funnel submission through `await submit(...)`.
- **Bypassing `submit()` with manual save calls** → use `submit(form, async (f) => ...)` so pending state + validation error mapping stay consistent.
- **Showing validation errors on initial render** → render errors only after `touched()`/`dirty()` and iterate `field().errors()` for a single error path.
- **Partial CVA implementations in custom controls** (missing `onTouched`/`setDisabledState`) → implement full CVA contract and forward blur/disabled to the real input.
- **Letting numeric fields travel as strings** (common in custom inputs) → normalize in the CVA (or before submit) so schema validators run on correct types.
- **Using `[value]`/`(input)` together with `[formField]`** → make `[formField]` the single source of truth for read/write state.
- **Porting imperative `control.disable()` patterns from Reactive Forms** → express disable rules declaratively in schema (`disabled(path.field, () => condition())`).

## PR validation checklist (frontend)

- `bun run test -- <changed-spec-or-feature-pattern>`
- `bun run postdev:check`
- `bun run typecheck:assertions`
- `bun run lint` (when template/component logic changed broadly)

## References

- `references/angular-signal-forms-api-patterns.md`
- <https://angular.dev/essentials/signal-forms>
- <https://angular.dev/api/forms/signals/form>
- <https://angular.dev/api/forms/signals/schema>
- <https://angular.dev/api/forms/signals/submit>
