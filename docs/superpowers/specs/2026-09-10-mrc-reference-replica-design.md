# MRC v0.0.15 — Reference Replica Design

**Date:** 2026-09-10  
**Status:** DESIGN LOCKED BY OWNER, PRE-IMPLEMENTATION  
**Owner:** Doonchy  
**Authority:** `Auth/0017_V0015_ReferenceReplica.md`  
**Baseline:** installed/published `v0.0.14` pre-cert build  
**Next implementation version:** `v0.0.15`  
**Final product target remains:** `v0.1.0`

## 1. Goal

Reconstruct MRC's primary Windows GUI to match the Owner-authored Runner Control reference as faithfully as practical in native WPF, while preserving the already-verified MRC runtime, lifecycle, ownership, update, and safety architecture.

The target is no longer "reference-inspired." At the canonical 2048x1222 viewport, the primary screen must reproduce the reference's composition, density, card proportions, spacing rhythm, hero hierarchy, three-button control rail, luminous state language, and dark/amber atmosphere closely enough that it clearly reads as the same visual design.

MRC-specific capabilities absent from the reference remain available through secondary surfaces so they do not deform the primary runner-control composition.

## 2. Version and release boundary

This reconstruction is `v0.0.15`, not a same-version replacement of `v0.0.14`.

Main-PC already has `0.0.14`. MRC's updater compares semantic versions, so replacing `v0.0.14` bytes would still result in `UP TO DATE` and would make the new GUI invisible to the normal update path.

Implementation SHALL begin on `precert-v0.0.15`, created from the final approved design/spec baseline. `v0.0.14` remains frozen as live evidence.

## 3. Architecture choice

### Chosen approach: Native WPF reference replica

The GUI remains native WPF. Use WPF layout, templates, brushes, gradients, effects, bindings, and focused controls/resources.

Rejected:

- **WebView2/HTML/CSS:** unnecessary browser dependency and second UI runtime.
- **Custom Direct2D surface:** unnecessary implementation and maintenance cost for this control application.

Native WPF is sufficient for the reference fidelity required while preserving the current executable and test architecture.

## 4. Authority precedence

For v0.0.15 GUI work:

1. hard runtime/safety authority;
2. `Auth/0017_V0015_ReferenceReplica.md`;
3. Owner-authored reference image;
4. non-conflicting prior visual authority;
5. implementation convenience.

If visual fidelity conflicts with runtime truth or safety, runtime truth/safety wins and the visual adaptation must stay as close to the reference as possible without lying.

## 5. Canonical reference geometry

Canonical reference image: `2048x1222`.

Reference-derived large-screen targets:

| Measure | Target |
| --- | ---: |
| runner columns | 4 |
| left card-field inset | ~41 px |
| card width | ~469 px |
| card height | ~146–154 px |
| column gap | ~22 px |
| row gap | ~22 px |
| horizontal inner card padding | ~18–19 px |
| enabled START control | ~137 x 46 px |
| primary action slots | 3 |
| card aspect | ~3.1:1 |

Small WPF/DPI/anti-aliasing variance is acceptable. Structural drift is not.

Primary anti-regression rule: **more power, less height**. Cards gain strength through density, contrast, state lighting, and control scale, not empty vertical area.

## 6. Primary screen composition

Always visible:

- reference-style menu button;
- exact eyebrow text: `MAIN PC • LOCAL-FIRST CONTROL`;
- large `MAIN RUNNER CONTROL` hero title;
- upper-right live capsule;
- compact truth/refresh metadata beneath the capsule;
- dense runner grid;
- minimal bottom system/status affordance.

The six counters, filters, search, bulk controls, manual refresh, and detailed findings SHALL NOT remain as permanent full-width rows above the runner grid.

## 7. Exact hero/header content

### Left

- square rounded menu button with amber three-line glyph;
- eyebrow: `MAIN PC • LOCAL-FIRST CONTROL`;
- hero: `MAIN RUNNER CONTROL`.

### Right

Live capsule text:

`LOCAL INVENTORY LIVE • {TotalCount} MANAGED`

This uses the managed-runner count only and must not include external/unattributed processes.

Truth line beneath capsule:

`local truth • {HH:mm:ss} • 3s refresh • 20 FPS UI`

The time is local machine time. Version/build identity remains secondary technical metadata and must not compete with the hero title.

## 8. Visual component decomposition

Implementation SHALL decompose the presentation into focused units/resources rather than permanently expanding `MainWindow.xaml` into a monolith.

Planned structure:

- `src/MRC.Gui/Themes/ReferenceReplica.xaml`
- `src/MRC.Gui/Controls/AmbientBackground.xaml`
- `src/MRC.Gui/Controls/HeroHeader.xaml`
- `src/MRC.Gui/Controls/RunnerControlCard.xaml`
- `src/MRC.Gui/Controls/StateOrb.xaml`
- `src/MRC.Gui/Controls/ControlDrawer.xaml`
- `src/MRC.Gui/Controls/SystemDrawer.xaml`

`MainWindow.xaml` composes these units and owns the main runner-grid container. Exact code-behind class files accompany controls only where WPF requires them; lifecycle logic remains outside the controls.

### AmbientBackground

Owns only visual atmosphere:

- deep near-black/navy upper field;
- subtle cool haze;
- lower/right amber bloom;
- edge vignette;
- no hit testing;
- no runner state logic.

### HeroHeader

Owns only header presentation and drawer-open request surface. It consumes boundary/count/refresh presentation state but performs no lifecycle mutation.

### RunnerControlCard

Owns one managed-runner visual module. It binds state/capabilities and routes actions to the existing MainWindow/operations boundary. It does not call processes or `RunnerEngine` directly.

### StateOrb

Owns the reference-style orb visuals. It consumes existing state and shared animation intensity. It creates no timer.

### ControlDrawer

Owns search, filters, counters, bulk controls, and manual refresh.

### SystemDrawer

Owns system/external/unattributed findings and detailed refresh/status evidence. It is read-only with respect to external/unattributed runtimes.

## 9. Runner-card anatomy

Each card contains exactly:

1. luminous state orb at upper-left;
2. dominant runner name;
3. secondary technical line: `REPO  <repository-name>` in monospace;
4. small DETAILS affordance;
5. state badge at upper-right;
6. three-slot lifecycle rail.

Full paths, PIDs, session data, agent IDs, and verbose diagnostics do not appear on the default card.

### DETAILS affordance

DETAILS is a quiet `28x28` header control placed immediately to the left of the state badge. It opens the existing diagnostic/details path. It is never a fourth primary lifecycle button.

## 10. Three-slot lifecycle rail

The three-button anatomy is authoritative.

| State | Slot 1 | Slot 2 | Slot 3 |
| --- | --- | --- | --- |
| OFF | START enabled | STOP disabled | RESTART disabled |
| IDLE | START disabled | STOP enabled | RESTART enabled |
| BUSY | FORCE STOP enabled | STOP disabled | RESTART disabled |
| STARTING | START disabled | STOP disabled | RESTART disabled |
| STOPPING | START disabled | STOP disabled | RESTART disabled |
| ERROR | START disabled | STOP disabled | RESTART disabled |

FORCE STOP replacing START in the first visual slot does not merge semantics. It invokes only the existing distinct FORCE STOP path and must still require explicit confirmation that an active GitHub Actions job may be interrupted.

At large reference scale, all three primary slots target ~46 px height and equal visual width.

## 11. State orb and card-state treatment

### OFF

- cool neutral orb/housing;
- restrained gray border;
- OFF badge;
- amber START active.

### IDLE

- bright green center and halo;
- clear green card edge/glow;
- IDLE badge;
- STOP/RESTART active;
- START disabled.

### BUSY

- amber/yellow orb and halo;
- amber state edge/presence;
- BUSY badge;
- first slot becomes FORCE STOP;
- normal STOP/RESTART remain protected.

### STARTING

- blue orb/state treatment;
- shared animation intensity active;
- rail disabled.

### STOPPING

- purple orb/state treatment;
- shared animation intensity active;
- rail disabled.

### ERROR

- orange/red orb and urgency treatment;
- lifecycle mutation remains fail-closed;
- DETAILS remains available.

The existing shared 20 FPS animation architecture remains the only timing authority. No per-card timers.

## 12. Card visual treatment

At canonical large-screen scale:

- dark blue-charcoal raised card surface;
- subtle cool gray border;
- rounded corners matched to reference appearance;
- soft black depth shadow;
- IDLE uses visible green border/glow;
- BUSY uses amber/yellow presence;
- OFF uses neutral cool treatment;
- hover raises border brightness/elevation slightly without changing state meaning.

Enabled START SHALL be a bright filled warm amber/yellow button with dark text at reference-level priority. It should be one of the most visually obvious elements on an OFF card.

Enabled STOP and RESTART SHALL use substantial equal-scale outlined controls. Disabled controls remain visibly control-shaped but strongly subdued.

## 13. Background atmosphere

The background is a layered composition, not one flat brush:

1. near-black/navy base;
2. subtle cool haze in upper/middle field;
3. warm amber radial bloom biased toward lower-right;
4. restrained secondary amber warmth beneath card rows;
5. outer vignette.

No wallpaper or network asset dependency is required. Native WPF gradients/effects are preferred.

Final color tokens SHALL be derived from the Owner reference and tuned using rendered comparison. Initial sampled anchors from the reference may be used as starting evidence, including approximately:

- upper field near `RGB(9,14,15)`;
- normal card interior near `RGB(18,24,28)`;
- enabled START center near `RGB(252,195,76)`;
- lower-right amber field near `RGB(82,46,17)`;
- hero white near `RGB(245,247,249)`.

These sampled values are starting anchors, not substitutes for visual review.

## 14. Responsive model

Layout is derived from usable runner-surface width and reference-safe card width, bounded to 1–4 columns.

Required canonical outcomes:

- `2048x1222` → 4 columns;
- approximately 1600 px → normally 3 columns;
- `1200x760` → 2 columns;
- `1180x760` → 2 columns;
- `900x560` → 1 column.

Reduce columns before shrinking the three-slot control rail. At 2048, the current eight Main-PC managed runners appear as two compact rows of four when unfiltered.

## 15. Control Drawer

The reference-style menu button opens a **left overlay drawer**. It does not push or resize the runner grid.

Large/desktop target width: `400 px`. At smaller windows, drawer width is `min(400 px, window width - 32 px)`.

Drawer contains:

- compact truthful counters;
- search;
- ALL / IDLE / BUSY / OFF / ERROR filters;
- TURN ALL ON;
- TURN ALL OFF;
- manual refresh.

Keyboard behavior:

- `/` opens the drawer if closed, focuses Search, and selects existing search text;
- `Escape` closes the Control Drawer when open and returns keyboard focus to the runner surface;
- modal safety confirmations retain normal Windows dialog behavior and are not closed through drawer handling.

Bulk safety semantics remain unchanged.

## 16. System Drawer

System findings use a bottom anchored secondary surface.

Collapsed state: compact one-line summary/indicator that does not consume the card field.

Expanded state: overlays upward without resizing the primary runner grid and may use up to 35% of available window height.

It shows external/unattributed evidence and refresh/status detail. External/unattributed entries never gain lifecycle controls.

## 17. CLI Help micro-design

The existing three-column Help table remains structurally approved.

Interactive command-cell grammar becomes:

- literal `MRC` → YELLOW/warm command token;
- option token such as `--help`, `--check`, `--update`, `--version`, `--doctor`, `--diagnose` → CYAN;
- aliases → GRAY;
- descriptions → WHITE.

Heading is exact semantic composition:

- `MRC` → YELLOW;
- ` // COMMAND REFERENCE` → CYAN.

Canonical command cells must be segmented, not given one tone for the whole string.

Redirected output stays deterministic plain text without ANSI/control characters.

## 18. Data flow

No new runtime authority is introduced.

### Refresh

`RunnerEngine.RefreshReport()` → existing dashboard/presentation model → runner-grid/card bindings → orb/badge/action eligibility.

### Per-runner lifecycle

Card action → existing GUI action boundary → `RunnerOperationsService` → existing verified lifecycle implementation.

No visual control may call runner processes or bypass `RunnerOperationsService`.

### Bulk lifecycle

Control Drawer action → existing bulk action boundary → existing operations service/shared lifecycle.

### System evidence

Runtime report system findings → System Drawer only. External/unattributed evidence remains display-only.

## 19. Error handling

- rendering failure never weakens environment fencing or control eligibility;
- ERROR remains fail-closed;
- BUSY FORCE STOP retains confirmation;
- drawer/animation state never changes lifecycle capability truth;
- if a decorative effect cannot render, controls should degrade to readable static presentation rather than become unusable.

## 20. Expected implementation files

Expected presentation work:

- `src/MRC.Gui/MainWindow.xaml`;
- `src/MRC.Gui/MainWindow.xaml.cs` only for composition/focus/drawer wiring;
- `src/MRC.Gui/Themes/ReferenceReplica.xaml`;
- `src/MRC.Gui/Controls/AmbientBackground.xaml(.cs if required)`;
- `src/MRC.Gui/Controls/HeroHeader.xaml(.cs if required)`;
- `src/MRC.Gui/Controls/RunnerControlCard.xaml(.cs if required)`;
- `src/MRC.Gui/Controls/StateOrb.xaml(.cs if required)`;
- `src/MRC.Gui/Controls/ControlDrawer.xaml(.cs if required)`;
- `src/MRC.Gui/Controls/SystemDrawer.xaml(.cs if required)`;
- `src/MRC.Gui/Presentation/RunnerDashboardViewModel.cs` only for bounded presentation state if needed;
- central CLI presentation/table code for Help token segmentation;
- preview/render tooling;
- v0.0.15 identity/package/release workflow files after visual acceptance.

Not redesign targets:

- runner discovery/control core;
- Windows ownership evidence;
- `RunnerOperationsService` semantics;
- updater verification/activation/rollback;
- Doctor/Diagnose;
- environment fence;
- release selection semantics;
- managed/external/unattributed ownership model.

## 21. TDD slicing strategy

The future implementation plan SHALL decompose work into RED/GREEN slices rather than one giant XAML rewrite:

1. create/freeze `precert-v0.0.15` and establish 0.0.15 identity RED;
2. reference theme/geometry tokens and responsive layout;
3. compact runner-card anatomy and three-slot rail;
4. state orb and state-specific card treatment;
5. hero/header reconstruction;
6. ambient background reconstruction;
7. Control Drawer while preserving search/filter/counter/bulk behavior;
8. System Drawer while preserving external isolation;
9. Help command-token segmentation;
10. reference comparison/evidence harness;
11. full visual convergence passes;
12. v0.0.15 package/release provenance;
13. Main-PC update and live gauntlet.

Each slice follows RED → correct failure proof → minimal GREEN → focused verification. Full inherited regression is mandatory at integration gates and before any release trigger.

## 22. Visual evidence package

Mandatory renders:

- 2048x1222 primary screen;
- 1200x760 primary screen;
- 1180x760 primary screen;
- 900x560 primary screen;
- 900x560 BUSY-focused frame if BUSY FORCE STOP is not visible in ordinary minimum view;
- Control Drawer open at a representative desktop viewport;
- System Drawer expanded at a representative desktop viewport;
- mixed OFF/IDLE/BUSY large-screen state frame.

For the canonical 2048 review, evidence package SHALL include:

- Owner reference;
- candidate render;
- side-by-side comparison;
- 50% opacity normalized overlay where practical;
- difference visualization where practical.

Overlay/difference images are diagnostic evidence. Human rendered review remains authoritative for aesthetic fidelity.

## 23. Visual acceptance matrix

Every applicable category must individually receive `10.0/10`:

- hero/title scale;
- menu-button role/geometry;
- live-pill role/geometry;
- primary field margins;
- four-column geometry;
- card width/height/aspect;
- column/row spacing;
- runner-name hierarchy;
- technical-line hierarchy;
- orb size/luminosity;
- state-badge geometry;
- three-slot control dimensions;
- enabled START amber strength;
- STOP/RESTART visual balance;
- disabled-state clarity;
- IDLE glow fidelity;
- BUSY state clarity;
- background amber bloom composition;
- overall density;
- absence of dead card space;
- no clipping/overlap/tiny text;
- secondary instrumentation does not deform primary composition.

Any category below 10.0/10 means HOLD. No average score can override an individual miss.

## 24. Functional acceptance matrix

Must preserve/prove:

- START from verified OFF;
- IDLE STOP;
- RESTART completes verified OFF before START;
- repeated START/STOP/RESTART without duplicate sessions;
- BUSY ordinary mutation protection;
- explicit FORCE STOP confirmation;
- TURN ALL ON targets only verified OFF managed runners;
- TURN ALL OFF targets only verified IDLE and skips BUSY;
- closing GUI leaves runners running;
- external Lotto remains display-only and non-blocking when ownership is proven;
- genuinely unattributed unsafe evidence remains fail-closed;
- search/filter/counters remain truthful;
- single-instance open/focus behavior remains truthful and consistent;
- Help colors are correct interactively;
- redirected Help remains deterministic plain text.

## 25. Release acceptance

After implementation and visual/function acceptance:

- build identity `0.0.15` everywhere;
- package `MRC-v0.0.15-win-x64.zip`;
- manifest version/channel/stage/final-target coherence;
- SHA256SUMS matches exact package bytes;
- packaged CLI reports 0.0.15;
- Git tag and release target match exact candidate HEAD;
- release remains prerelease/pre-certification;
- Main-PC `MRC --check` discovers 0.0.15 from installed 0.0.14;
- `MRC --update` verifies/activates 0.0.15 and retains rollback;
- live Main-PC GUI and lifecycle evidence passes.

## 26. Non-goals

Not authorized:

- runner registration/unregistration;
- Windows runner service management;
- multi-machine fleet control;
- external/unattributed control;
- WebView2/browser rewrite;
- new updater semantics;
- new runner lifecycle semantics;
- safety-confirmation removal for visual fidelity;
- final v0.1.0 publication before full certification.

## 27. Design-complete gate

This design becomes implementation-plan input only after the Owner reviews and approves this written spec. No implementation begins before that approval.
