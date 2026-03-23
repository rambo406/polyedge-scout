# Angular Signal Forms API Patterns (Concise)

Source baseline: <https://angular.dev/essentials/signal-forms>

## 1) Form model + field tree

```ts
import { signal } from '@angular/core';
import { form } from '@angular/forms/signals';

const model = signal({ email: '', password: '' });
const loginForm = form(model);
```

- `loginForm.email()` returns `FieldState`
- `loginForm.email().value()` returns current field value
- `loginForm().value()` returns full form value object

## 2) Schema validators

```ts
import { form, schema, required, min, max } from '@angular/forms/signals';

const accountSchema = schema<{ commissionPercent: number; markup: number }>((path) => {
  required(path.commissionPercent, { message: 'Commission is required' });
  min(path.commissionPercent, 0);
  max(path.commissionPercent, 100);
  required(path.markup);
  min(path.markup, 0);
});

const accountForm = form(signal({ commissionPercent: 0, markup: 0 }), accountSchema);
```

## 3) Template binding with `[formField]`

```html
<input type="email" [formField]="loginForm.email" />
<input type="password" [formField]="loginForm.password" />

<z-icon-input type="number" [formField]="accountForm.commissionPercent" suffixIcon="percent" />
```

- `[formField]` handles value + form state sync
- For custom controls, CVA behavior must correctly propagate value/blur/disabled

## 4) Field state checks

```ts
loginForm.email().valid();
loginForm.email().touched();
loginForm.email().dirty();
loginForm.email().disabled();
loginForm.email().errors();
```

Use `touched()` / `dirty()` gates before displaying validation UI.

## 5) Submit workflow

```ts
import { submit } from '@angular/forms/signals';

async function onSubmit(event: Event): Promise<void> {
  event.preventDefault();

  await submit(loginForm, async (f) => {
    const payload = f().value();
    await api.login(payload);
    return undefined; // or return server validation errors
  });
}
```

`submit()` applies returned server validation errors back into the relevant field(s).

## 6) Migration quick diff

- `FormGroup`/`FormControl` → `signal(model)` + `form(model, schema)`
- `Validators.required/min/max` → `required/min/max`
- `formControlName` / `[formControl]` → `[formField]`
- `control.value` / `control.valid` → `field().value()` / `field().valid()`
- imperative disable/enable → schema logic (`disabled(path.field, () => condition())`)
