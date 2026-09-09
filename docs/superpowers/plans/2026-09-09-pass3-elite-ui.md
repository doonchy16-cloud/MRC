# PASS 3 Elite UI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the locked MRC v0.1 dense WPF operations dashboard on top of the verified PASS 2 `RunnerEngine`, with deterministic presentation logic, one shared animation timer, stable refresh behavior, and rendered-preview verification.

**Architecture:** `MRC.Core` remains the only runtime-truth/control authority. `MRC.Gui` adds a presentation layer that maps `RunnerSnapshot` values into stable row view-models, performs search/filter/counting without mutating engine truth, and drives glyph animation from one shared timer. `MainWindow` binds to the presentation layer and refreshes approximately every three seconds without recreating rows or reordering them by state.

**Tech Stack:** .NET 10.0.401, WPF, C#, GitHub Actions Windows runner.

**Spec:** `Auth/0006_UI.md`, `Auth/0007_Anim.md`, `Auth/0010_Passes.md`, with PASS 2 engine contract in `src/MRC.Core/RunnerEngine.cs`.

## Global Constraints

- Default window approximately `1180 x 760`; minimum approximately `900 x 560`.
- Canonical columns: STATUS, RUNNER NAME, REPOSITORY, STATE, CONTROL.
- Runner rows target ~42 px high and long identity text ellipsizes.
- Stable default sort is repository, then runner name; refresh never sorts by runtime state.
- Search matches runner name, repository, or runner folder.
- Canonical filters: ALL, IDLE, BUSY, OFF, ERROR.
- Palette values in `Auth/0006_UI.md` are exact.
- State is communicated through color + glyph/motion + text.
- Spinner sequence is `/ - \\ |` using one shared timer; no timer per runner.
- PASS 3 does not implement bulk operations, force-stop flow, updater, doctor, or other PASS 4 scope.
- Closing the GUI must not stop runners.

---

### Task 1: Deterministic dashboard presentation model

**Files:**
- Create: `src/MRC.Gui/Presentation/RunnerRowViewModel.cs`
- Create: `src/MRC.Gui/Presentation/RunnerDashboardViewModel.cs`
- Create: `src/MRC.Gui/Presentation/RunnerFilter.cs`
- Create: `tests/MRC.Pass3.Tests/MRC.Pass3.Tests.csproj`
- Create: `tests/MRC.Pass3.Tests/Program.cs`
- Create: `scripts/verify-pass3.ps1`

**Interfaces:**
- Consumes: `RunnerSnapshot`, `RunnerState`, `RunnerDescriptor` from `MRC.Core`.
- Produces: stable `Rows`, `VisibleRows`, `SearchText`, `SelectedFilter`, and exact state counters.

- [ ] **Step 1: Write failing presentation tests**

Test construction with snapshots for two repositories and all runtime states. Assert sort order is repository then runner name; search matches name/repository/folder; filters expose only canonical states; counters reflect all current rows; calling `ApplySnapshots` updates existing row objects in place for identical runner paths.

- [ ] **Step 2: Run RED**

Run: `pwsh ./scripts/verify-pass3.ps1`
Expected: compile failure because PASS 3 presentation types do not yet exist.

- [ ] **Step 3: Implement minimal presentation types**

`RunnerRowViewModel` retains one row object per runner path and exposes immutable identity plus mutable `State`, `Error`, and `Glyph`. `RunnerDashboardViewModel.ApplySnapshots(IReadOnlyList<RunnerSnapshot>)` reuses rows by normalized path, removes vanished rows, sorts only by repository/name, recalculates counters, and rebuilds filtered visibility without state-based reordering.

- [ ] **Step 4: Run GREEN**

Run: `pwsh ./scripts/verify-pass3.ps1`
Expected: presentation tests pass.

- [ ] **Step 5: Commit**

Commit message: `feat: add PASS 3 dashboard presentation model`.

---

### Task 2: Shared truthful animation model

**Files:**
- Create: `src/MRC.Gui/Presentation/RunnerAnimationClock.cs`
- Extend: `tests/MRC.Pass3.Tests/Program.cs`

**Interfaces:**
- Consumes: each row's `RunnerState`.
- Produces: glyph frames only; never changes state.

- [ ] **Step 1: Write failing animation tests**

Assert OFF is frozen `-`; ERROR is frozen `!`; IDLE advances at 550 ms; BUSY at 110 ms; STARTING at 180 ms; STOPPING at 250 ms; sequence is exactly `/ - \\ |`; one clock updates any number of row objects.

- [ ] **Step 2: Run RED**

Run: `pwsh ./scripts/verify-pass3.ps1`
Expected: failure because `RunnerAnimationClock` is missing.

- [ ] **Step 3: Implement minimal shared clock**

Use one `DispatcherTimer` in runtime construction and a deterministic `Tick(DateTimeOffset now, IEnumerable<RunnerRowViewModel> rows)` method for tests. Track per-row last frame time/index; do not create timers inside rows.

- [ ] **Step 4: Run GREEN**

Run: `pwsh ./scripts/verify-pass3.ps1`
Expected: animation tests pass with prior tests still green.

- [ ] **Step 5: Commit**

Commit message: `feat: add PASS 3 shared runner animation clock`.

---

### Task 3: Elite WPF dashboard shell

**Files:**
- Replace: `src/MRC.Gui/MainWindow.xaml`
- Replace: `src/MRC.Gui/MainWindow.xaml.cs`
- Create: `src/MRC.Gui/Presentation/StateVisuals.cs`
- Extend: `tests/MRC.Pass3.Tests/Program.cs`

**Interfaces:**
- Consumes: `RunnerDashboardViewModel` and `RunnerEngine`.
- Produces: the locked dashboard layout and approximately three-second refresh loop.

- [ ] **Step 1: Write failing XAML/behavior tests**

Assert exact default/minimum window dimensions, exact palette hex values, canonical column labels, search box, five canonical filter controls, counters, row height `42`, ellipsis settings, state stripe, glyph column, bottom status line, and absence of PASS 4 bulk controls. Assert source contains one shared `DispatcherTimer` for animation and one refresh timer around three seconds, not timer creation per row.

- [ ] **Step 2: Run RED**

Run: `pwsh ./scripts/verify-pass3.ps1`
Expected: current PASS 1 placeholder shell fails layout requirements.

- [ ] **Step 3: Implement WPF shell**

Use the exact authority palette. Use a top identity/header region, counter strip, search/filter toolbar, dense `ListView`/grid-view row template, and bottom refresh/status bar. Preserve state stripe independently of hover/selection. Buttons are dark with semantic outline/text rather than saturated fills.

- [ ] **Step 4: Wire runtime refresh safely**

On authorized Main-PC, construct `RunnerEngine`; otherwise keep the dashboard visible in blocked/error mode without runner-control access. Refresh every ~3 seconds and on manual refresh. Apply snapshots through the view-model so row instances/order remain stable. PASS 3 per-row control may invoke already-existing PASS 2 `Start`/`StopIdle`; do not add bulk or force-stop behavior.

- [ ] **Step 5: Run GREEN**

Run: `pwsh ./scripts/verify-pass3.ps1`
Expected: all PASS 3 static/presentation/animation tests pass plus PASS 1 and PASS 2 regressions.

- [ ] **Step 6: Commit**

Commit message: `feat: build PASS 3 elite operations dashboard`.

---

### Task 4: Rendered-preview visual gate

**Files:**
- Create: `tests/MRC.Pass3.Preview/MRC.Pass3.Preview.csproj`
- Create: `tests/MRC.Pass3.Preview/Program.cs`
- Modify: `.github/workflows/pass3.yml`

**Interfaces:**
- Consumes: actual `MainWindow` XAML/resources.
- Produces: `artifacts/pass3/MRC-PASS3-preview.png` rendered at 1180x760 with deterministic sample rows representing OFF, IDLE, BUSY, STARTING, STOPPING, and ERROR.

- [ ] **Step 1: Add preview harness and workflow**

Run the preview executable in STA on Windows, instantiate the same dashboard visual tree with sample presentation rows, call `Measure`, `Arrange`, `UpdateLayout`, and render using `RenderTargetBitmap`/`PngBitmapEncoder`.

- [ ] **Step 2: Upload preview artifact**

GitHub Actions uploads the PNG even when later visual review is pending.

- [ ] **Step 3: Inspect the actual PNG**

Review at realistic scale for hierarchy, crowding, clipping, column balance, color discipline, long-name ellipsis, counter/search/filter readability, row density, and semantic-state visibility. Any visual defect is HOLD, regardless of static test success.

- [ ] **Step 4: Fix and rerender until visual review is 10.0/10**

Every visual change reruns PASS 1, PASS 2, PASS 3 tests and produces a fresh preview.

- [ ] **Step 5: Commit**

Commit message: `test: add PASS 3 rendered visual gate`.

---

### Task 5: PASS 3 elite certification gate

**Files:**
- Modify: `.github/workflows/pass3.yml` only if required by evidence gaps.

**Interfaces:**
- Consumes: all PASS 1/PASS 2/PASS 3 suites plus rendered visual review.
- Produces: PASS 3 10.0/10 evidence; only then may PASS 4 begin.

- [ ] **Step 1: Fresh full CI on latest `main`**

Require PASS 1 regression suite, PASS 2 elite + engine suite, PASS 3 presentation/animation/layout suite, GUI Release build, and preview generation all green on the same head SHA.

- [ ] **Step 2: Elite category review**

Score separately: visual design, state truthfulness, interaction model, scalability/stability, accessibility, performance/animation architecture, regression integrity, and rendered-preview quality. Every category must be exactly 10.0/10; any unresolved or unverified requirement is HOLD.

- [ ] **Step 3: Open PASS 4 only after 10.0/10**

Do not add PASS 4 implementation before this gate closes.
