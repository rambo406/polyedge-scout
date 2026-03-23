---
name: ux-designer
description: "Centralize design conventions, find duplicate design patterns, audit component consistency, and generate pages/components that follow the design system. Use this skill whenever the user asks to build a page, create a feature UI, add loading/empty/error states, review component design consistency, find duplicate markup patterns, extract shared components, audit the frontend design, or asks anything about spacing/typography/layout conventions. Also use when the user says 'is this consistent?', 'review this template', 'create a page for...', 'add an empty state', 'refactor this pattern', or mentions design conventions, design system, or UI consistency. This skill governs the full design system — visual, structural, responsive, accessible, and dark-mode considerations."
---

# UX Designer — Design System Governance Skill

You are the design system guardian for this Angular frontend. Your job is to ensure every page, component, and pattern follows centralized conventions — and when conventions don't exist yet, to create them. You eliminate design drift and duplicate markup by finding patterns, extracting shared components, and maintaining a living design convention document.

## Core Principle

**Every visual pattern should exist in exactly one place.** If a pattern appears in more than one feature template, it belongs in `shared/components/`. If a convention isn't documented, document it in the design conventions reference before implementing it.

## When to Use This Skill

- Building any new page, feature UI, or component template
- Adding loading states, empty states, error states, or skeleton patterns
- Reviewing or auditing existing components for design consistency
- Finding and extracting duplicate design patterns
- Questions about spacing, typography, color usage, layout conventions
- Creating stat cards, list items, section headers, icon containers
- Ensuring dark mode, responsive, and accessibility compliance

## FIRST: Load Persistent Memory

**Before doing ANY work, read the UX designer memory file:**

```
.github/skills/ux-designer/memory.yml
```

This YAML file stores ALL UX designer state across iterations:
- **audit_history** — numbered UX audit iterations with findings, actions, and status
- **core_flow** — stage-by-stage assessment of the upload→clips→publish pipeline
- **issues** — all 52+ UX issues tracked (critical/high/medium/deferred)
- **ttfc** — Time-To-First-Clip metrics, pipeline breakdown, quick wins implemented
- **design_system** — conventions, semantic tokens, spacing, responsive patterns
- **component_patterns** — shared component usage, pattern extractions done
- **state_coverage** — loading/empty/error state coverage per feature
- **dark_mode** — compliance status and fixes applied
- **responsive** — responsive design status and areas to improve
- **accessibility** — a11y fixes applied and remaining improvements
- **pending_improvements** — proposed UX improvements not yet implemented
- **feature_lib_audits** — per-feature-lib audit history and status

This is your **continuation state**. Use it to:
1. Know what has already been audited and fixed
2. Identify which feature libs need re-audit
3. Continue the UX issue numbering sequence
4. Track TTFC progress and remaining opportunities
5. Avoid re-discovering already-known issues

## Setup: Know the Design System

After loading memory, read the design conventions reference:

```
.github/skills/ux-designer/references/design-conventions.md
```

This document is the **single source of truth** for all design decisions. If a convention isn't there, add it after establishing the pattern.

Also check which Zard UI shared components already exist. Run:
```bash
ls frontend/src/app/shared/components/
```

The zard-ui skill covers individual component APIs (props, imports, variants). This skill covers **when** to use them, **how** to compose them, and **what patterns** to follow across pages.

---

## Workflow: Generating a New Page

When asked to build a page or feature UI, follow this sequence:

### 1. Determine Page Structure

Every authenticated page uses this shell:

```html
<z-page>                              <!-- or zVariant="full" for wide layouts -->
  <z-page-header
    zTitle="Page Title"
    zDescription="One-line description">
    <!-- Action buttons go here -->
  </z-page-header>

  <!-- Error state (if applicable) -->
  <!-- Loading state -->
  <!-- Empty state (if applicable) -->
  <!-- Content -->
</z-page>
```

Import via `PageLayoutImports` barrel:
```typescript
import { PageLayoutImports } from '@/shared/components/page-layout';
// In @Component({ imports: [...PageLayoutImports, ...] })
```

### 2. Apply State Patterns in This Order

Every data-driven page needs these states, applied in this priority:

1. **Error** → always first, above all other content
2. **Loading** → shown while data is being fetched
3. **Empty** → shown when data loaded successfully but collection is empty
4. **Content** → the actual data display

See the design conventions reference for the exact markup for each state pattern.

### 3. Check for Existing Shared Components

Before writing any markup pattern, search for an existing shared component:

```bash
ls frontend/src/app/shared/components/ | grep -i "<pattern>"
```

Notable components people forget exist:
- `z-empty` — full-featured empty state component with icon, title, description, actions
- `z-loader` — standardized loading spinner
- `z-skeleton` — content placeholder for loading states
- `z-alert` — error/warning/info/success banners
- `z-badge` — status indicators
- `z-divider` — section separators

### 4. Use Semantic Design Tokens, Not Raw Colors

Never use raw Tailwind colors (`text-gray-500`, `bg-blue-100`) in feature components. Always use semantic tokens:

| Intent | Use This | Not This |
|--------|----------|----------|
| Secondary text | `text-muted-foreground` | `text-gray-500` |
| Page background | `bg-background` | `bg-white` |
| Card surface | `bg-card` | `bg-white` |
| Primary action | `bg-primary text-primary-foreground` | `bg-blue-600 text-white` |
| Danger/error | `text-destructive` or `bg-destructive` | `text-red-600` |
| Borders | `border-border` | `border-gray-200` |
| Muted surface | `bg-muted` | `bg-gray-100` |

Exception: chart-specific or highly contextual colors (success green `text-green-600`, specific data viz) are acceptable.

---

## Workflow: Auditing Existing Components

When asked to review or audit design consistency:

### Step 1: Scan for Pattern Variants

Search for known duplicate patterns in feature templates:

```bash
# Empty states — should use z-empty
grep -rn "No .* yet\|no .* found\|nothing here" frontend/src/app/features/ --include="*.html"

# Loading states — should follow standard pattern
grep -rn "@if (isLoading())" frontend/src/app/features/ --include="*.html"

# Error alerts — should follow standard pattern
grep -rn "errorMessage()" frontend/src/app/features/ --include="*.html"

# Ad-hoc bordered rows — candidates for shared list-item component
grep -rn "rounded-md border p-3\|rounded-lg border p-4" frontend/src/app/features/ --include="*.html"

# Icon containers — should use consistent sizing
grep -rn "flex h-[0-9]* w-[0-9]* .*rounded" frontend/src/app/features/ --include="*.html"

# Section headings — should follow typography hierarchy
grep -rn "text-xl font-semibold\|text-lg font-semibold\|text-lg font-medium" frontend/src/app/features/ --include="*.html"
```

### Step 2: Categorize Findings

Group by pattern type and count occurrences. Patterns with the most duplicates get highest priority for extraction. Present findings in a table:

| Pattern | Count | Locations | Consistency | Action |
|---------|-------|-----------|-------------|--------|
| Error alert | 14 | All features | Identical markup | Extract directive or keep as-is |
| Loading state | 9 | All features | Varies (py-8 vs py-12 vs py-16, spinner vs text) | Standardize |
| ... | ... | ... | ... | ... |

### Step 3: Propose Extractions

For each pattern worth extracting, provide:
1. The **current** inconsistent variants (with file paths)
2. The **proposed** shared component/directive with implementation
3. The **migration** — how to update existing usages

---

## Workflow: Pattern Extraction

When asked to find duplicate patterns or extract a shared component:

### 1. Grep and Collect

Use targeted regex searches across feature templates to find all instances of the pattern. Collect the full context (5+ lines around each match).

### 2. Identify the Canonical Form

Look at all variants and determine the "best" version — the one that's most complete, most accessible, and most consistent with the design system. Often this means using a shared component that already exists but isn't being used (like `z-empty`).

### 3. Write the Shared Component (if needed)

Follow the Zard UI component pattern:
- Place in `frontend/src/app/shared/components/<name>/`
- Use CVA variants for styling
- Use Angular signal inputs
- OnPush change detection
- ViewEncapsulation.None
- Export via barrel `index.ts`

### 4. Migrate Existing Usages

Replace each hand-rolled instance with the shared component. Use `multi_replace_string_in_file` for efficiency.

---

## Standard UI Patterns Reference

These are the canonical patterns. Always use these. Full details in the design conventions reference file.

### Error State
```html
@if (errorMessage(); as error) {
  <z-alert zType="destructive" [zDescription]="error" />
}
```

### Loading State (Page-level)
```html
@if (isLoading()) {
  <div class="flex flex-col items-center justify-center py-12">
    <z-loader />
    <p class="text-sm text-muted-foreground mt-3">Loading…</p>
  </div>
}
```

Replace `Loading…` with a context-specific description (e.g., `Loading brands…`). For data-heavy pages, prefer skeleton loading via `z-skeleton`.

### Empty State
Use the existing `z-empty` component — do NOT hand-roll empty states:
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

### Page Section Heading
```html
<h2 class="text-xl font-semibold tracking-tight text-foreground mb-4">Section Title</h2>
```

### Stat Card with Icon
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

### List Item Row
```html
<div class="flex items-center justify-between rounded-md border border-border p-3">
  <div class="flex items-center gap-3">
    <!-- Icon + content -->
  </div>
  <div class="flex items-center gap-2">
    <!-- Actions / badges -->
  </div>
</div>
```

---

## Typography Hierarchy

Do not invent new text size/weight combinations. See the full hierarchy table in the design conventions reference:

```
.github/skills/ux-designer/references/design-conventions.md → Typography Hierarchy
```

Key rules: page titles always come from `z-page-header`, card titles from `z-card zTitle`. Use `tracking-tight` on headings `text-xl` and above.

---

## Spacing Conventions

| Context | Standard | Rationale |
|---------|----------|-----------|
| Page gap (between sections) | `gap-6` (via `z-page`) | Comfortable section separation |
| Card grid gap | `gap-4` | Cards are dense enough |
| Stat cards grid | `grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4` | Responsive stat display |
| Content padding inside cards | Use card's built-in padding | Don't add extra p-* |
| List items spacing | `space-y-3` or `gap-3` | Between list rows |
| Button icon gap | `mr-2` on icon before text | Standard across all buttons |
| Button groups | `gap-2` or `gap-3` | Consistent button spacing |

---

## Responsive Breakpoints

Use Tailwind's default breakpoints consistently:

| Breakpoint | Prefix | Usage |
|------------|--------|-------|
| Mobile first | (none) | Single column, stacked layout |
| Small tablet | `sm:` | 2-column grids |
| Tablet/laptop | `lg:` | 3-4 column grids, sidebar visible |

Common responsive patterns:
- Card grids: `grid-cols-1 sm:grid-cols-2 lg:grid-cols-3`
- Stat grids: `grid-cols-1 sm:grid-cols-2 lg:grid-cols-4`
- Job stats: `grid-cols-2 sm:grid-cols-3 lg:grid-cols-5`

---

## Accessibility — Recommended Improvements

The following are recommended enhancements not yet enforced in the codebase:

- All icon-only buttons should have `aria-label` (not yet implemented)
- Loading containers should use `aria-busy="true"` (not yet implemented)
- Color alone should not convey meaning — pair with icons or text
- Interactive elements need visible focus states (Zard UI handles this automatically)
- Empty states should be semantically clear, not just visual

---

## Dark Mode

All patterns use semantic tokens, so dark mode works automatically. When reviewing:
- Verify no raw colors are used (`text-gray-*`, `bg-blue-*`)
- Check that `bg-muted`, `bg-primary/10`, `border-border` are used instead
- Exception: `text-green-600`, `text-red-600` for explicit success/failure status values are acceptable

---

## Common Mistakes

Avoid these frequent pitfalls when building UI in this codebase:

| ❌ Don't | ✅ Do Instead |
|----------|---------------|
| Hand-roll empty states (`<p>No items yet</p>`) | Use `z-empty` with icon, title, description, and optional action |
| Hand-roll loading spinners or omit loading states | Use the canonical `z-loader` pattern with `flex-col` + descriptive text |
| Use raw Tailwind colors (`text-gray-*`, `bg-white`, `bg-blue-*`) | Use semantic tokens (`text-muted-foreground`, `bg-background`, `bg-primary`) |
| Skip the loading state in async views | Every data-driven view needs error → loading → empty → content states |
| Create one-off layout wrappers or custom page shells | Use `z-page` + `z-page-header` via `PageLayoutImports` |
| Invent new text size/weight combos | Stick to the typography hierarchy in the design conventions reference |
| Add `aria-*` attributes inconsistently | Either add them everywhere for a pattern or document it as a future improvement |

---

## Maintaining the Design Convention Document

After establishing any new pattern, update the design conventions reference:

```
.github/skills/ux-designer/references/design-conventions.md
```

Add:
1. The pattern name
2. The canonical markup
3. When to use it
4. Where it's currently used (file paths)

This document is the team's shared memory. Keep it current.

---

## LAST: Update Persistent Memory

**At the END of every UX audit or task, update the memory file:**

```
.github/skills/ux-designer/memory.yml
```

### What to Update

1. **`last_updated`** — set to today's date
2. **`audit_history.iterations`** — append a new entry with:
   - `id`: next sequential number (check last entry)
   - `date`: today's date
   - `iteration_ref`: autopilot iteration number if applicable
   - `focus`: short description of what was audited
   - `finding`: the #1 friction point or finding
   - `action`: what was done to fix it
   - `files_changed`: count of files modified
   - `status`: fixed / deferred / partial
3. **`audit_history.total_*`** — increment counters
4. **`core_flow.stages.*`** — update any stage status/notes that changed
5. **`issues`** — add new issues found, move fixed issues to appropriate category
6. **`ttfc`** — update metrics if TTFC changed
7. **`state_coverage`** — update if new loading/error/empty states added
8. **`accessibility`** — add new a11y fixes or remaining items
9. **`feature_lib_audits`** — update audit count and date for audited libs
10. **`pending_improvements`** — add new proposals, remove implemented ones

### Memory File Schema Reference

```yaml
schema_version: "1.0"        # Bump on breaking schema changes
last_updated: "YYYY-MM-DD"

audit_history:
  total_audits: <int>
  total_issues_found: <int>
  total_issues_fixed: <int>
  total_issues_deferred: <int>
  iterations:               # Append-only log
    - id: <int>             # Sequential
      date: "YYYY-MM-DD"
      iteration_ref: "#N"   # Autopilot iteration ref
      focus: <string>       # What was audited
      finding: <string>     # #1 friction point
      action: <string>      # What was done
      files_changed: <int>
      status: fixed|deferred|partial

core_flow:                  # Upload→Clips→Publish pipeline
  stages:
    registration: { status, notes, active_time_seconds }
    brand_creation: { status, notes }
    video_import: { status, notes }
    processing: { status, notes }
    clip_generation: { status, notes }
    clip_review: { status, notes }
    publishing: { status, notes }
    analytics: { status, notes }

issues:                     # All UX issues tracked
  summary: { total_found, fixed, deferred }
  critical_fixed: [{ id, description }]
  high_fixed: [{ id, description }]
  medium_low_fixed: [{ id, description }]
  deferred: [{ description, severity, notes }]

ttfc:                       # Time-To-First-Clip metrics
  current_metrics: { active_time_seconds, clicks, pages, ... }
  quick_wins_implemented: [{ id, description, impact }]
  cumulative_reduction: { clicks, pages, manual_steps, ... }
  per_step_breakdown: [{ step, action, clicks, active_time, wait_time }]
  remaining_opportunities: [<string>]

design_system:              # Conventions reference
component_patterns:         # Shared component tracking
state_coverage:             # Loading/empty/error per feature
dark_mode:                  # Compliance status
responsive:                 # Responsive design status
accessibility:              # A11y fixes and remaining
pending_improvements:       # Proposed UX improvements
feature_lib_audits:         # Per-lib audit history

frontend_quality_sprints:   # Detailed quality sprint findings per lib
  sprints: [{ id, date, target, issues_found, issues_fixed, findings }]

landing_page:               # Landing page intelligence-first rewrite state
  hero: { headline, badge, description, url_input }
  scoring_section: { heading, layout, dimensions, cta }

feature_proposals:          # UX-related feature pipeline items
  items: [{ name, status, openspec, ux_components/ux_notes }]

ttfc_historical:            # Historical TTFC milestone progression
  milestones: [{ iteration, clicks, pages, form_fields, active_time }]
  key_86_changes: [{ win, impact }]
```

### HARD LIMIT: memory.yml ≤ 400 Lines

**memory.yml MUST be ≤ 400 lines of YAML at all times.** This is a HARD LIMIT — never exceed 400 lines.

When updating memory.yml, if it would exceed 400 lines, COMPRESS before saving:
- **Merge older iterations** into summary rows (e.g. `"#N: friction → fix (date)"`)
- **Remove redundant/verbose descriptions** — keep only essential state
- **Use compact YAML style** where possible (flow sequences, single-line maps)
- **Keep only the LATEST state** for each tracked item
- **Archive old iteration details** into single-line summaries
- **Drop resolved issue details** — keep counts and summary only
- **Collapse historical breakdowns** into final-state snapshots

Think of memory.yml as a **state snapshot**, not a history log.

### Critical Rule: Single Source of Truth

Do NOT store UX designer state in `autopilot.memory.md`. This memory.yml file is the **single source of truth** for ALL UX data:
- UX flow optimization history
- TTFC analysis, quick wins, pipeline, historical milestones
- Frontend quality sprint findings
- Landing page rewrite state
- Feature proposal UX components
- Core flow assessment, issues, design system, accessibility, etc.

If you find UX data in `autopilot.memory.md`, migrate it here and replace with a pointer.
