---
name: acceptance-test-driven-development
description: "Drive feature development from the outside in using Gherkin acceptance tests as the outer loop around TDD. Use this skill when the user mentions ATDD, acceptance test driven development, BDD, behavior driven development, double loop TDD, outside-in development, writing E2E tests first, writing Gherkin scenarios before implementing, feature-driven development with E2E tests, or wants to write acceptance criteria as executable specifications. Also use when the user says 'write scenarios before implementing', 'E2E first then implement', 'let's define the behavior first', or asks to collaborate on acceptance criteria. This skill complements the test-driven-development skill — ATDD is the outer loop, TDD is the inner loop."
---

# Acceptance Test-Driven Development (ATDD)

## Overview

Start from what the user should experience. Write a failing Gherkin scenario. Then use TDD to build the internals until the scenario passes.

**Core principle:** The acceptance test is the contract between what we agreed to build and what we actually built. It fails until the feature is complete, proving that everything — backend, frontend, integration — works together.

**Relationship to TDD:** ATDD is the outer loop. TDD is the inner loop. They're not alternatives — they're concentric. The acceptance test tells you *what* to build. TDD tells you *how* to build each piece.

```
┌─────────────────────────────────────────────────────────┐
│  OUTER LOOP — ATDD (Acceptance Test)                    │
│                                                         │
│  1. DISCUSS  → Agree on Gherkin scenarios               │
│  2. DISTILL  → Write failing E2E tests                  │
│                                                         │
│  ┌───────────────────────────────────────────────────┐  │
│  │  INNER LOOP — TDD (Unit / Integration Tests)     │  │
│  │                                                   │  │
│  │  RED → GREEN → REFACTOR → repeat                  │  │
│  │  (build backend, frontend, services piece by      │  │
│  │   piece until the outer acceptance test passes)   │  │
│  └───────────────────────────────────────────────────┘  │
│                                                         │
│  3. DEVELOP  → Inner TDD loop fills in the feature      │
│  4. DEMO     → Acceptance test passes → feature done    │
└─────────────────────────────────────────────────────────┘
```

## When to Use

**Always:**
- New user-facing features
- Behavior changes visible to the user
- Bug fixes that should be verifiable through the UI
- Cross-cutting features touching backend + frontend

**Exceptions (use TDD alone instead):**
- Internal refactoring with no behavior change
- Pure backend utility/library code
- Infrastructure/configuration changes
- Performance optimizations (use benchmarks, not E2E)

## The Four Phases

### Phase 1: DISCUSS — Agree on What "Done" Looks Like

Before writing any code or test, collaborate with the human to define the acceptance criteria as concrete Gherkin scenarios. This isn't a formality — it's where misunderstandings get caught before they become bugs.

**How to run the discussion:**

1. Ask the user to describe the feature in their own words
2. Identify the key behaviors — what should users be able to do?
3. Surface edge cases and error scenarios by asking "what happens when...?"
4. Draft 2-4 Gherkin scenarios covering the happy path, key edge cases, and at least one error path
5. Present the scenarios and iterate until the user confirms they capture the intended behavior

**Writing good scenarios:**

<Good>
```gherkin
@workflows
Feature: Workflow Scheduling

  Scenario: Schedule a workflow for future execution
    Given I am logged in and on the workflows page
    When I create a workflow named "Weekly Upload"
    And I schedule it to run every Monday at 9am
    Then I should see the workflow listed with status "Scheduled"
    And the next run date should be the upcoming Monday
```
Describes observable behavior from the user's perspective. Uses domain language. Verifiable.
</Good>

<Bad>
```gherkin
  Scenario: Test workflow creation
    Given the database has a workflows table
    When I POST to /api/workflows with JSON body
    Then the response status should be 201
    And a row should exist in the database
```
Tests implementation details, not user behavior. Brittle. Meaningless to a non-developer.
</Bad>

**Scenario quality checklist:**
- Written in domain language (not code/API jargon)
- Describes what the user sees and does
- Each scenario tests one coherent behavior
- Given sets up context, When triggers action, Then asserts observable outcome
- Independent — each scenario works in isolation

### Phase 2: DISTILL — Write Failing Acceptance Tests

Turn the agreed scenarios into executable tests. In this project, that means `.feature` files + step definitions using `playwright-bdd`.

**Project E2E structure:**
```
e2e/
├── features/          ← Gherkin .feature files (one per domain)
│   ├── auth.feature
│   ├── brands.feature
│   └── ...
├── steps/             ← Step definitions (one per feature)
│   ├── auth.steps.ts
│   ├── brands.steps.ts
│   ├── fixtures.ts    ← Re-exports Given/When/Then from base fixture
│   └── ...
├── page-objects/      ← Page Object classes (grouped by domain)
│   ├── auth/
│   ├── brands/
│   └── index.ts       ← Barrel file
├── fixtures/
│   └── base.fixture.ts ← Custom Playwright fixtures (api, authenticatedPage)
├── helpers/
│   └── api.helper.ts  ← Programmatic test data setup
├── data/
│   └── test-data.ts   ← Shared constants
└── playwright.config.ts
```

**Step-by-step:**

1. **Write the `.feature` file** (or add scenarios to an existing one)
   - Use `@tag` annotations matching the domain
   - One Feature per file, multiple Scenarios allowed
   - Reuse existing step phrasing when possible (check `e2e/steps/` for existing Given/When/Then)

2. **Create or extend step definitions** in `e2e/steps/<domain>.steps.ts`
   - Import `Given`, `When`, `Then`, `expect` from `./fixtures`
   - Steps should delegate to Page Objects for UI interaction
   - Keep step bodies thin — orchestration, not implementation

3. **Create or extend Page Objects** in `e2e/page-objects/<domain>/`
   - One Page Object per page/view
   - Use semantic locators: `getByRole`, `getByLabel`, `getByTestId`
   - Export from `e2e/page-objects/index.ts`

4. **Run the test and watch it fail:**
   ```bash
   cd e2e && npm test
   ```
   The tests should fail because the feature doesn't exist yet — that's the point. If they fail for other reasons (missing page, typo in step definition), fix those first until you get a *meaningful* failure.

**Example — new feature file:**
```gherkin
@videos
Feature: Video Import

  Scenario: Import a YouTube video and see it in the list
    Given I am logged in and have a brand
    When I navigate to the YouTube import page
    And I import the video "https://www.youtube.com/watch?v=jNQXAC9IVRw"
    Then I should see the video in the brand's video list
    And the video status should eventually be "Ready"
```

**Example — step definitions delegating to Page Objects:**
```typescript
import { YouTubeImportPage, VideoListPage } from '../page-objects';
import { expect, Given, When, Then } from './fixtures';

When('I navigate to the YouTube import page', async ({ page }) => {
    const importPage = new YouTubeImportPage(page);
    await importPage.goto();
});

When('I import the video {string}', async ({ page }, url: string) => {
    const importPage = new YouTubeImportPage(page);
    await importPage.importVideo(url);
});
```

### Phase 3: DEVELOP — Inner TDD Loop

Now the outer test is failing. Time to build the feature piece by piece using TDD (invoke the `test-driven-development` skill for the inner loop).

**Work outside-in:**

1. **Start from the edge closest to the user:**
   - Frontend component/page (Angular) — write unit tests, implement
   - Backend API endpoint — write unit tests, implement
   - Domain logic — write unit tests, implement
   - Infrastructure/persistence — write unit tests, implement

2. **Each piece follows the TDD cycle:**
   ```
   RED → GREEN → REFACTOR
   ```
   Write a failing unit test for the smallest piece needed. Implement minimally. Refactor.

3. **Periodically run the outer E2E test** to check progress:
   ```bash
   cd e2e && npm test -- --grep "@your-tag"
   ```
   As implementation progresses, the E2E test should fail at later and later steps until it eventually passes entirely.

4. **The outer test is the finish line.** When the acceptance test goes green, the feature is functionally complete. Don't keep adding code once it passes.

**When to create Page Objects vs reuse existing ones:**
- Navigating to a new page/route → new Page Object
- Interacting with an existing page in a new way → extend existing Page Object
- Shared UI patterns (modals, toasts) → shared helper or fixture

### Phase 4: DEMO — Verify the Feature is Complete

The acceptance test passing is the primary proof of completion, but don't stop there.

**Verification checklist:**
- [ ] All new Gherkin scenarios pass (`cd e2e && npm test`)
- [ ] All existing E2E tests still pass (no regressions)
- [ ] All unit tests pass (frontend: `bunx nx test frontend`, backend: `dotnet test`)
- [ ] New Page Objects are exported from `e2e/page-objects/index.ts`
- [ ] Step definitions reuse existing steps where possible
- [ ] Feature file uses appropriate `@tag` annotations
- [ ] No hardcoded test data — use `api.helper.ts` for programmatic setup

**Report results:** Summarize which scenarios pass, any remaining issues, and confirm that the behavior matches what was agreed in Phase 1.

## E2E Tests vs Unit Tests — When to Write What

| Question | E2E (ATDD) | Unit (TDD) |
|----------|-----------|------------|
| **Who is the audience?** | User / stakeholder | Developer |
| **What does it test?** | Observable behavior through the UI | Single unit in isolation |
| **When does it fail?** | Feature is missing or broken end-to-end | Single component/function is wrong |
| **How many?** | Few (2-5 per feature) | Many (dozens per feature) |
| **Speed** | Slow (seconds) | Fast (milliseconds) |
| **Flakiness risk** | Higher (network, timing) | Very low |

**Rule of thumb:** E2E tests cover *what* the user can do. Unit tests cover *how* each piece works. You need both. You write E2E first (this skill) and unit tests inside the develop phase (TDD skill).

## Anti-Patterns

### Writing E2E tests after implementation

Same problem as writing unit tests after — the tests are biased by what you built, not what was required. You'll verify the code you wrote rather than discovering what's missing. The acceptance test should be a failing contract *before* you start building.

### Skipping the DISCUSS phase

Jumping straight to writing `.feature` files without collaborating on the scenarios means you're encoding *your* assumptions, not the agreed behavior. This is how features pass all tests but miss the actual requirement.

### Testing implementation through E2E

E2E tests that assert on database state, API response shapes, or internal class behavior are brittle and unhelpful. The user doesn't see the database. Test what the user sees.

### Too many E2E scenarios

If you have 30 Gherkin scenarios for one feature, most of them should be unit tests instead. E2E tests should cover the critical paths. Edge cases and boundary conditions belong in unit tests where they're fast and focused.

### Scenario steps that do everything

Steps like `When I do the entire checkout flow` hide complexity. Each step should be one user action or one observable assertion. If a step needs a 40-line implementation, it's doing too much.

### No Page Objects

Writing Playwright selectors directly in step definitions makes them impossible to maintain. When the UI changes, you update Page Objects once instead of hunting through every step file.

## Quick Reference

**Commands:**
```bash
# Run all E2E tests
cd e2e && npm test

# Run by tag
cd e2e && npm test -- --grep "@brands"

# Run headed (see the browser)
cd e2e && npm run test:headed

# Run with Playwright UI
cd e2e && npm run test:ui

# View test report
cd e2e && npm run test:report
```

**File naming conventions:**
- Feature: `e2e/features/<domain>.feature`
- Steps: `e2e/steps/<domain>.steps.ts`
- Page Object: `e2e/page-objects/<domain>/<page-name>.page.ts`
- Barrel export: add to `e2e/page-objects/index.ts`

**New feature checklist:**
1. [ ] DISCUSS — scenarios agreed with human
2. [ ] DISTILL — `.feature` file written, steps defined, Page Objects created
3. [ ] Acceptance test runs and fails meaningfully
4. [ ] DEVELOP — inner TDD loop builds the feature
5. [ ] DEMO — acceptance test passes, all tests green
