# Design Conventions — ViralVector Frontend

> **Living document.** Updated whenever a new pattern is established or an existing pattern is consolidated.
> Last updated: 2026-03-05

## Table of Contents

1. [Design Token System](#design-token-system)
2. [Typography Hierarchy](#typography-hierarchy)
3. [Spacing & Layout](#spacing--layout)
4. [Color Usage Rules](#color-usage-rules)
5. [Page Structure](#page-structure)
6. [State Patterns](#state-patterns)
7. [Common UI Patterns](#common-ui-patterns)
8. [Responsive Grid Patterns](#responsive-grid-patterns)
9. [Pattern Duplication Tracker](#pattern-duplication-tracker)

---

## Design Token System

All colors are oklch-based CSS custom properties defined in `frontend/src/styles.css`.

### Core Tokens

| Token | Light | Usage |
|-------|-------|-------|
| `--background` | `oklch(1 0 0)` | Page background |
| `--foreground` | `oklch(0.129 0.042 264.695)` | Primary text |
| `--card` / `--card-foreground` | white / dark | Card surfaces |
| `--primary` / `--primary-foreground` | dark blue / white | Primary actions, accents |
| `--secondary` / `--secondary-foreground` | light gray / dark | Secondary buttons, backgrounds |
| `--muted` / `--muted-foreground` | gray / medium gray | Subtle backgrounds, secondary text |
| `--accent` / `--accent-foreground` | gray / dark | Hover states, active items |
| `--destructive` | red | Delete, error actions |
| `--border` | light gray | All borders |
| `--input` | light gray | Form input borders |
| `--ring` | blue-gray | Focus rings |
| `--radius` | `0.625rem` | Base border-radius |

### Sidebar Tokens

| Token | Usage |
|-------|-------|
| `--sidebar` | Sidebar background |
| `--sidebar-foreground` | Sidebar text |
| `--sidebar-primary` / `--sidebar-primary-foreground` | Active nav items |
| `--sidebar-accent` / `--sidebar-accent-foreground` | Hover/active state |
| `--sidebar-border` | Sidebar borders |
| `--sidebar-ring` | Sidebar focus |

### Chart Tokens

`--chart-1` through `--chart-5` — use for data visualization only.

### Tailwind Mapping

Tokens are mapped via `@theme inline` in `styles.css`:

```css
--color-background: var(--background);
--color-primary: var(--primary);
/* etc. */
```

Use Tailwind classes that reference these: `bg-background`, `text-foreground`, `border-border`, etc.

---

## Typography Hierarchy

| Level | Tailwind Classes | Context |
|-------|-----------------|---------|
| Page title | `text-2xl font-bold tracking-tight text-foreground` | Handled by `z-page-header` |
| Page description | `text-sm text-muted-foreground` | Handled by `z-page-header` |
| Section heading | `text-xl font-semibold tracking-tight text-foreground mb-4` | Major sections within a page |
| Subsection heading | `text-lg font-semibold text-foreground mb-3` | Smaller groupings |
| Card title | Via `z-card zTitle` prop | Card headers |
| Body text | `text-sm text-foreground` | Default readable content |
| Label / secondary | `text-sm text-muted-foreground` | Field labels, stat labels |
| Caption / metadata | `text-xs text-muted-foreground` | Timestamps, file sizes, counts |
| Large value | `text-2xl font-bold text-foreground` | Stat card numbers |

### Rules

- Never combine `font-bold` with `text-sm` for headings — it's not in the hierarchy
- Page titles always come from `z-page-header`, not custom `<h1>` tags (exception: detail pages with back-navigation)
- Use `tracking-tight` on headings `text-xl` and above

---

## Spacing & Layout

### Page-Level Spacing

| Element | Value | Source |
|---------|-------|--------|
| Gap between page sections | `gap-6` | Built into `z-page` component |
| Page max-width (default) | `max-w-5xl` | `z-page` with `zVariant="default"` |
| Page full-width | No max-width | `z-page` with `zVariant="full"` |
| Content area padding | `p-6` | Built into `z-content` component |

### Component Spacing

| Context | Value |
|---------|-------|
| Card grid gap | `gap-4` |
| List items spacing | `space-y-3` or `gap-3` |
| Button groups | `gap-2` (tight) or `gap-3` (normal) |
| Icon before button text | `mr-2` on the icon |
| Icon before text in labels | `gap-3` on the flex container |
| Section heading margin-bottom | `mb-4` (xl headings) or `mb-3` (lg headings) |
| Action row above content | `pt-2` (inside cards) |

### Standard Padding Inside List Items

| Pattern | Value |
|---------|-------|
| Bordered list row | `p-3` with `rounded-md border border-border` |
| Card-based list item | Use `z-card` default padding |
| Dense stat card | `p-2` internal div |

---

## Color Usage Rules

### Allowed

| Intent | Classes |
|--------|---------|
| Primary text | `text-foreground` |
| Secondary text | `text-muted-foreground` |
| Primary action | `bg-primary text-primary-foreground` |
| Destructive action | `bg-destructive text-destructive-foreground` |
| Muted background | `bg-muted` |
| Accent background | `bg-accent` |
| Icon accent | `bg-primary/10 text-primary` |
| Borders | `border-border` |
| Success status text | `text-green-600` ✅ (exception for status) |
| Failure status text | `text-red-600` ✅ (exception for status) |

### Forbidden in Feature Components

| Avoid | Reason |
|-------|--------|
| `text-gray-*` | Use `text-muted-foreground` |
| `bg-white` / `bg-black` | Use `bg-background` / `bg-foreground` |
| `bg-blue-*`, `bg-red-*` | Use `bg-primary`, `bg-destructive` |
| `border-gray-*` | Use `border-border` |
| Raw hex/rgb values | Use CSS custom properties |

---

## Page Structure

### Authenticated Pages (inside member-layout)

```
z-layout
├── z-sidebar [zWidth]="240"
│   ├── Brand logo + nav items
│   └── User info + logout
└── z-content
    └── <router-outlet /> → Feature page
        └── z-page [zVariant]
            ├── z-page-header [zTitle] [zDescription]
            │   └── <ng-content> action buttons
            ├── Error state
            ├── Loading state / Empty state
            └── Content sections
```

### Page Variant Decision

| Use `default` when | Use `full` when |
|--------------------|-----------------|
| Form-heavy pages | Dashboard with many cards |
| Detail views | Table-heavy pages |
| Settings pages | Job monitoring with grids |

---

## State Patterns

### Error State (Standard)

**Location**: First child inside `z-page`, before any content.

```html
@if (errorMessage(); as error) {
  <z-alert zType="destructive" [zDescription]="error" />
}
```

**Import**: `ZardAlertComponent` from `@/shared/components/alert`

**Current usage**: 14 instances across all features. Pattern is already consistent.

---

### Loading State (Page-level)

**Canonical pattern**:

```html
@if (isLoading()) {
  <div class="flex flex-col items-center justify-center py-12">
    <z-loader />
    <p class="text-sm text-muted-foreground mt-3">Loading…</p>
  </div>
}
```

Replace the `Loading…` text with a context-specific description (e.g., `Loading brands…`, `Loading videos…`).

**Import**: `ZardLoaderComponent` from `@/shared/components/loader`

**When to use skeleton loading instead**:

- When the page has a known layout structure (grids, cards)
- Dashboard stat cards use skeleton:

```html
@for (i of [1, 2, 3, 4]; track i) {
  <z-card>
    <div class="flex items-center gap-4">
      <z-skeleton class="h-11 w-11 rounded-lg" />
      <div class="flex-1 space-y-2">
        <z-skeleton class="h-4 w-20" />
        <z-skeleton class="h-7 w-12" />
      </div>
    </div>
  </z-card>
}
```

---

### Empty State

**USE `z-empty`** — do not hand-roll empty states.

```html
<z-empty
  zIcon="square-library"
  zTitle="No brands yet"
  zDescription="Create your first brand to start managing content."
>
  <button z-button (click)="onCreate()">
    <z-icon zType="plus" zSize="sm" class="mr-2" />
    Create Brand
  </button>
</z-empty>
```

**Import**: `ZardEmptyComponent` from `@/shared/components/empty`

**Variants**:

- With icon: `zIcon="icon-name"`
- With image: `zImage="path/to/image"`
- With actions: Add buttons as `<ng-content>`
- Without actions: Omit child content

---

## Common UI Patterns

### Stat Card (with Icon)

```html
<z-card>
  <div class="flex items-center gap-4">
    <div class="flex h-11 w-11 shrink-0 items-center justify-center rounded-lg bg-primary/10 text-primary">
      <z-icon [zType]="icon" />
    </div>
    <div>
      <p class="text-sm text-muted-foreground">{{ label }}</p>
      <p class="text-2xl font-bold text-foreground">{{ value }}</p>
    </div>
  </div>
</z-card>
```

**Grid**: `grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4`

**Used in**: Dashboard page (stats)

---

### Stat Card (Simple, No Icon)

```html
<z-card>
  <div class="flex flex-col gap-1 p-2">
    <span class="text-sm text-muted-foreground">{{ label }}</span>
    <span class="text-2xl font-bold">{{ value }}</span>
  </div>
</z-card>
```

**Used in**: Job dashboard (processing/enqueued/scheduled/succeeded/failed)

---

### Bordered List Row

```html
<div class="flex items-center justify-between rounded-md border border-border p-3">
  <div class="flex items-center gap-3">
    <z-icon [zType]="icon" class="text-muted-foreground" />
    <div>
      <p class="text-sm font-medium text-foreground">{{ title }}</p>
      <p class="text-xs text-muted-foreground">{{ subtitle }}</p>
    </div>
  </div>
  <div class="flex items-center gap-2">
    <z-badge [zType]="statusVariant">{{ status }}</z-badge>
    <!-- optional action buttons -->
  </div>
</div>
```

**Used in**: Video list, series list, platform integrations, clip rating table

---

### Card-Based List Row

```html
<z-card>
  <div class="flex items-center justify-between">
    <div class="flex items-center gap-3 min-w-0">
      <div class="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-muted text-muted-foreground">
        <z-icon [zType]="icon" zSize="sm" />
      </div>
      <div class="min-w-0">
        <p class="text-sm font-medium text-foreground truncate">{{ title }}</p>
        <p class="text-xs text-muted-foreground">{{ subtitle }}</p>
      </div>
    </div>
    <z-badge>{{ status }}</z-badge>
  </div>
</z-card>
```

**Used in**: Dashboard recent activity

---

### Section Heading

```html
<h2 class="text-xl font-semibold tracking-tight text-foreground mb-4">Section Title</h2>
```

---

### Icon Container Sizes

| Size | Classes | Usage |
|------|---------|-------|
| Small | `h-7 w-7 rounded-full` | Branding logos |
| Medium-small | `h-8 w-8 rounded-full` | Pipeline steps, inline actions |
| Medium | `h-9 w-9 rounded-lg bg-muted text-muted-foreground` | Activity list items |
| Standard | `h-11 w-11 rounded-lg bg-primary/10 text-primary` | Stat cards |
| Large | `h-14 w-14 rounded-full bg-muted` | Empty state hero icons |

Use the appropriate size for context. Do not invent new size combinations.

---

### Button with Icon (Standard)

```html
<button z-button>
  <z-icon zType="plus" zSize="sm" class="mr-2" />
  Create Item
</button>
```

For compact contexts, use `zSize="sm"` on the button and `class="mr-1"` on the icon.

---

### Detail Page Header (with Back Button)

When a page has a back-navigation context:

```html
<div class="flex items-center justify-between">
  <div class="flex items-center gap-3">
    <button z-button zType="ghost" zSize="icon" (click)="navigateBack()">
      <z-icon zType="chevron-left" />
    </button>
    <div>
      <h1 class="text-2xl font-bold text-foreground">{{ title }}</h1>
      @if (description) {
        <p class="text-sm text-muted-foreground">{{ description }}</p>
      }
    </div>
  </div>
  <div class="flex gap-2">
    <!-- Action buttons -->
  </div>
</div>
```

---

## Responsive Grid Patterns

| Content Type | Grid Classes |
|-------------|-------------|
| Feature cards (brands) | `grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4` |
| Stat cards (4) | `grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4` |
| Stat cards (5) | `grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-4` |
| Two-column form layout | `grid grid-cols-1 lg:grid-cols-3 gap-6` |
| Stacked list | `flex flex-col gap-3` or `space-y-3` |

---

## Pattern Duplication Tracker

Track known duplications and their resolution status.

| Pattern | Instances | Status | Resolution |
|---------|-----------|--------|------------|
| Error alert `@if (errorMessage())` | 14 | ✅ Consistent | Keep as-is — markup is identical everywhere |
| Loading state `@if (isLoading())` | 11 | ✅ Fixed | All standardized to `flex-col items-center py-12` + `z-loader` + descriptive text |
| Empty states (hand-rolled) | 4 | ⚠️ Contextual | 8 page-level instances migrated to `z-empty`. 4 inline card empty states remain (series-list, publish-audit-log, subtitle-editor, clip-rating-table) — contextual inline messages, different from page-level empty states |
| Icon containers | 7 | ✅ Documented | 5 standard sizes documented above. Each instance uses the right size for its context — no extraction needed |
| Bordered list rows | 7 | ✅ Documented | All use `rounded-md border p-3` pattern. Internal content varies too much for component extraction |
| Section headings | 4+ | ⚠️ Inconsistent | Standardize to `text-xl font-semibold tracking-tight text-foreground mb-4` |
| Stat cards | 2 variants | ✅ Intentional | Two valid variants documented: with-icon and simple |
