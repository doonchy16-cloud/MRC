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

The primary success criterion is no longer "modern dark dashboard" or "reference-inspired." The primary screen must read as the same visual product family as the supplied reference at first glance: same density, same card proportions, same warm/cool atmosphere, same hero hierarchy, same three-button control rhythm, same luminous state language, and same four-column large-screen composition.

MRC-specific capabilities that do not exist in the reference remain available through secondary instrumentation surfaces so they do not deform the primary runner-control composition.

## 2. Version and release boundary

The reconstruction is `v0.0.15`, not a same-version replacement of `v0.0.14`.

Reason: the live updater compares semantic versions. Main-PC already has `0.0.14`; replacing `v0.0.14` bytes would still produce `UP TO DATE` and prevent the normal update path from discovering the rebuilt UI.

Implementation SHALL therefore begin on a new `precert-v0.0.15` branch created from the final approved design/spec baseline. `v0.0.14` remains frozen as live evidence.

## 3. Architecture choice

### Chosen approach: Native WPF reference replica

The GUI remains native WPF. The reconstruction uses WPF layout, templates, brushes, gradients, effects, user controls, bindings, and the existing presentation models.

Rejected alternatives:

- **WebView2/HTML/CSS:** excellent styling flexibility but adds an unnecessary browser dependency, a second UI runtime, and a larger security/packaging surface.
- **Custom Direct2D/drawn surface:** can achieve exact pixels but creates excessive implementation and maintenance cost for a control application.

Native WPF gives sufficient fidelity while preserving the existing application architecture and testability.

## 4. Authority precedence

For v0.0.15 GUI work, precedence is:

1. hard runtime/safety authorities;
2. `Auth/0017_V0015_ReferenceReplica.md`;
3. Owner-authored visual reference image;
4. non-conflicting prior v0.0.14 visual authorities;
5. implementation convenience.

Where prior visual guidance conflicts with the new reference replica, the new reference wins. Where visual fidelity conflicts with safety or runtime truth, safety/runtime truth wins and the visual adaptation must preserve the reference as closely as possible without lying.

## 5. Reference geometry model

The canonical reference image is 2048x1222.

Reference-derived large-screen target bands:

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

These values are reconstruction targets. The renderer may vary by a few pixels because WPF text measurement, DPI, and anti-aliasing differ from the browser reference. However, visible structural drift is a defect.

The primary anti-regression rule is: **more power, less height**. Cards must gain strength through density, contrast, state lighting, and control scale, not empty vertical area.

## 6. Primary screen composition

The primary screen is intentionally sparse in types of widgets but rich in visual hierarchy.

### Primary always-visible elements

- reference-style menu/control button;
- machine/local-control eyebrow;
- large `MAIN RUNNER CONTROL` hero title;
- large live/control-ready status capsule;
- small local-truth/refresh metadata;
- compact runner grid;
- minimal system/status affordance.

### Secondary elements

The following no longer consume permanent top-level dashboard rows:

- counters;
- filters;
- search;
- TURN ALL ON;
- TURN ALL OFF;
- manual refresh;
- detailed external/system findings;
- deep diagnostics.

They move into compact drawers or secondary surfaces.

## 7. Visual component decomposition

The design SHOULD decompose the presentation into focused units. Exact filenames can be adjusted during implementation if existing project conventions suggest a better grouping.

### `AmbientBackground`

Purpose: render the layered dark/warm environment behind the control surface.

Responsibilities:

- near-black/navy upper field;
- subtle cool haze;
- lower/right amber bloom;
- edge vignette;
- no hit testing;
- no runtime state logic.

Dependencies: WPF brushes/effects only.

### `HeroHeader`

Purpose: reproduce the reference hero hierarchy.

Contains:

- menu/control button;
- eyebrow (`MAIN PC • LOCAL-FIRST CONTROL` or equivalent exact MRC wording);
- `MAIN RUNNER CONTROL` title;
- live inventory/control-ready pill;
- compact local truth / refresh metadata.

Dependencies: dashboard counts/boundary state, no lifecycle mutation.

### `RunnerGrid`

Purpose: lay out managed runner cards using reference-safe geometry.

Responsibilities:

- adaptive column count from usable width;
- stable ordering;
- scrolling;
- no global scale transform;
- 4 columns at canonical large viewport.

### `RunnerControlCard`

Purpose: render one managed runner as a compact reference-style control module.

Contains:

- `StateOrb`;
- runner name;
- secondary technical identity line;
- state badge;
- secondary details affordance;
- `ThreeSlotActionRail`.

The card must not own runner lifecycle logic. It binds capabilities and routes events/commands to the existing operations boundary.

### `StateOrb`

Purpose: replace the tiny square/dot visual with the reference's luminous state object.

Responsibilities:

- dark outer housing;
- luminous center;
- soft radial state halo;
- intensity binding from shared animation state;
- state-specific color.

No timer is created here. The existing shared 20 FPS animation clock remains the only animation timing authority.

### `ThreeSlotActionRail`

Purpose: preserve the reference's three equal primary controls.

State mapping:

| Runner state | Slot 1 | Slot 2 | Slot 3 |
| --- | --- | --- | --- |
| OFF | START enabled | STOP disabled | RESTART disabled |
| IDLE | START disabled | STOP enabled | RESTART enabled |
| BUSY | FORCE STOP enabled through confirmation path | STOP disabled/protected | RESTART disabled/protected |
| STARTING | disabled | disabled | disabled |
| STOPPING | disabled | disabled | disabled |
| ERROR | disabled/fail-closed | disabled/fail-closed | disabled/fail-closed |

FORCE STOP replacing START visually in BUSY state does not merge their semantics. FORCE STOP still invokes its distinct existing confirmed path.

### `ControlDrawer`

Purpose: house MRC capabilities that are useful but not part of the reference's primary composition.

Contains:

- search;
- state filters;
- counters;
- TURN ALL ON;
- TURN ALL OFF;
- manual refresh.

The drawer should open from the reference-style menu button. Search shortcut `/` should open/focus the drawer and search box when necessary.

### `SystemDrawer`

Purpose: preserve visibility of external/unattributed/system evidence without contaminating the managed runner grid.

Contains:

- system findings summary;
- external/unattributed evidence;
- detailed status/refresh text where appropriate.

It must remain read-only with respect to external/unattributed processes.

## 8. Header design

At 2048 reference scale, the header should recreate the reference proportions rather than the compact v0.0.14 utility header.

### Left side

- square rounded menu button with amber glyph treatment;
- eyebrow in small monospace uppercase;
- very large white hero title below.

### Right side

- large rounded green live/control-ready capsule;
- bright green indicator dot;
- concise inventory/control-ready text;
- small monospace local truth / clock / refresh metadata underneath.

The version string remains technical metadata and should not compete with the main title.

## 9. Runner-card visual anatomy

### Top row

- luminous orb, approximately reference scale;
- runner name large, bold, white;
- state badge upper-right.

### Secondary row

Use one compact monospace identity line. MRC may use repository name and/or a short managed identity instead of the reference's Windows ID, but it should preserve the same visual role and density.

Full directory paths, PIDs, session details, and verbose diagnostics stay out of the default card.

### Action rail

- three equal primary slots;
- rail sits close to the bottom of the compact card;
- enabled START is filled amber/yellow with dark text;
- enabled STOP is high-contrast outlined/light-on-dark;
- enabled RESTART is high-contrast outlined/light-on-dark;
- disabled controls are dark/subdued but remain legible as controls;
- target reference height near 46 px at large scale;
- no fourth primary DETAILS button.

### Details affordance

A small secondary icon/text affordance may sit near the identity area. It opens the existing diagnostic/details path and must not visually compete with lifecycle controls.

## 10. State visual system

### OFF

- neutral gray/cool orb;
- restrained cool border;
- OFF badge;
- amber START active.

### IDLE

- green luminous orb;
- clear green border/outer glow;
- IDLE badge;
- STOP and RESTART active;
- START disabled.

### BUSY

- amber/yellow luminous orb;
- amber/yellow card-state presence;
- BUSY badge;
- first slot is FORCE STOP;
- normal STOP and RESTART remain disabled/protected;
- confirmation dialog remains mandatory.

### STARTING / STOPPING

- blue / purple state semantics respectively;
- animated intensity via existing shared animation model;
- lifecycle controls disabled during transition.

### ERROR

- orange/red urgency;
- no speculative lifecycle mutation;
- diagnostic affordance remains available.

## 11. Color and atmosphere

The candidate should visually match the reference rather than merely reuse the current v0.0.14 token palette.

Implementation preparation should derive final WPF color tokens by sampling the Owner reference at representative locations and then tune them through rendered comparison.

Required visual families:

- near-black/navy background;
- dark blue-charcoal card surface;
- cool gray card border;
- bright warm amber/yellow primary action;
- green live/IDLE glow;
- cool gray OFF orb/badge;
- white hero/runner text;
- muted gray technical text.

The background must include a visible but restrained amber atmospheric bloom in the same lower/right compositional region as the reference.

## 12. Responsive model

Responsive layout should be based primarily on available runner-surface width divided by a reference-safe card slot width.

Expected outcomes:

- 2048x1222: 4 columns;
- around 1600: normally 3 columns;
- 1200x760: 2 columns;
- 1180x760: 2 columns;
- 900x560: 1 column.

Controls should retain useful size. Reduce columns before shrinking the primary rail.

The current eight Main-PC runners should appear as two rows of four at the canonical large viewport.

## 13. Secondary instrumentation interaction

### Menu button

Opens/closes `ControlDrawer`.

### Search

- remains live filtering;
- `/` keyboard shortcut opens/focuses search if drawer is closed;
- Escape may return focus/close drawer if this can be added without conflicting with existing behavior.

### Filters

Remain ALL / IDLE / BUSY / OFF / ERROR unless runtime authority later requires additional filter exposure.

### Counters

Remain truthful dashboard counts but appear compactly inside the drawer rather than as six large permanent cards.

### Bulk actions

TURN ALL ON and TURN ALL OFF remain explicit controls. Existing safety semantics are unchanged.

## 14. CLI Help micro-design

The three-column Help table remains structurally unchanged.

Interactive semantic color grammar becomes:

- literal `MRC`: yellow/warm command token;
- option token (`--help`, `--doctor`, `--diagnose`, `--update`, `--check`, `--version`): cyan;
- alias column: gray;
- description: white;
- heading may render `MRC` yellow and `// COMMAND REFERENCE` cyan;
- redirected output remains plain deterministic text with no ANSI/control characters.

The formatter should segment canonical command cells rather than applying one tone to the entire command string.

## 15. Data flow

No new runtime authority is introduced.

### Refresh flow

`RunnerEngine.RefreshReport()` → existing dashboard/presentation model → runner grid/card bindings → state orb/badge/action eligibility.

### Lifecycle flow

Card action → existing GUI event/command boundary → `RunnerOperationsService` → existing verified lifecycle implementation.

No visual component may call runner processes directly or bypass `RunnerOperationsService`.

### Bulk flow

Control Drawer bulk action → existing bulk handler/service → existing shared lifecycle operations.

### System evidence flow

Runtime report system findings → System Drawer only. External/unattributed entries never become managed cards and never gain control actions.

## 16. Error handling

- GUI rendering errors must not weaken environment fencing or lifecycle eligibility.
- ERROR runner state remains fail-closed for mutation.
- BUSY FORCE STOP keeps the explicit warning/confirmation dialog.
- drawer state or visual animation failures must not alter control eligibility.
- secondary visual failures should degrade to readable static controls where feasible rather than making the application unusable.

## 17. Implementation boundaries

### Expected presentation files

Likely touched/created:

- `src/MRC.Gui/MainWindow.xaml`;
- `src/MRC.Gui/MainWindow.xaml.cs` only for presentation composition/drawer/focus wiring;
- `src/MRC.Gui/Presentation/RunnerDashboardViewModel.cs` only if drawer/presentation state needs a bounded property;
- new WPF control/resource files for hero, cards, orb, action rail, drawers, and theme tokens;
- `src/MRC.Cli/CliPresentation.cs` and/or central CLI table segmentation code for Help token coloring;
- visual preview tooling/scripts/tests;
- v0.0.15 identity/release workflow files after visual implementation is accepted.

### Files/layers not to redesign

- runner process discovery/control core;
- Windows process ownership evidence;
- `RunnerOperationsService` semantics;
- update verification/atomic activation/rollback;
- Doctor/Diagnose behavior;
- exact machine/root fence;
- release selection semantics;
- managed/external/unattributed ownership model.

## 18. TDD strategy

Implementation must proceed through small RED/GREEN slices rather than one giant XAML rewrite.

Recommended test slices for the future implementation plan:

1. v0.0.15 identity/version boundary;
2. reference geometry tokens and responsive card count;
3. compact three-slot card anatomy;
4. luminous state orb/state styling;
5. hero/header composition;
6. ambient background layers;
7. Control Drawer and preserved search/filter/bulk behavior;
8. System Drawer and external isolation;
9. Help command-token segmentation;
10. visual preview/reference comparison harness;
11. release packaging/provenance;
12. live Main-PC gauntlet.

Each slice: RED → correct failure proof → minimal GREEN → focused tests → full inherited regression at appropriate gates.

## 19. Visual evidence strategy

The visual gate must be stronger than the v0.0.14 gate.

### Mandatory candidate renders

- 2048x1222 primary screen;
- 1200x760 primary screen;
- 1180x760 primary screen;
- 900x560 primary screen;
- 900x560 BUSY-focused frame if BUSY FORCE STOP is not visible in the ordinary minimum viewport;
- drawer-open evidence at one representative desktop viewport;
- BUSY/IDLE/OFF mixed-state evidence at large viewport.

### Reference comparison

For the 2048 candidate, generate a comparison package containing:

- Owner reference image;
- candidate screenshot;
- side-by-side presentation;
- optional 50% opacity overlay or normalized difference visualization where practical.

The overlay is diagnostic evidence, not a numeric certification shortcut. Human review remains authoritative for aesthetic fidelity.

## 20. Visual acceptance criteria

A large-screen visual PASS requires all of the following:

- four-column runner field;
- card proportions visibly match the reference;
- no giant vertical card interiors;
- hero title has comparable scale and dominance;
- menu button occupies comparable visual role;
- upper-right live pill has comparable role/weight;
- runner names and metadata follow reference hierarchy;
- state orb geometry and luminosity feel equivalent;
- IDLE green edge/glow is clearly visible but controlled;
- enabled START has reference-level amber brightness and prominence;
- STOP/RESTART controls have comparable scale and alignment;
- inter-card gaps and field margins are visually close;
- lower/right amber atmosphere is present in comparable composition;
- MRC-specific secondary instrumentation does not deform the primary layout;
- no clipping, overlap, tiny text, misleading disabled state, or fake terminal layout remains.

No average score is sufficient. Any applicable visual category below 10.0/10 means HOLD.

## 21. Functional acceptance criteria

The visual reconstruction is not accepted merely because it resembles the reference.

Must preserve/prove:

- individual START;
- individual IDLE STOP;
- RESTART waits for verified OFF;
- repeated START/STOP/RESTART without duplicate sessions;
- BUSY normal lifecycle protection;
- explicit FORCE STOP confirmation;
- TURN ALL ON only affects verified OFF managed runners;
- TURN ALL OFF skips BUSY and never force-stops;
- closing GUI leaves runners running;
- external Lotto service remains display-only and non-blocking when ownership is proven;
- genuinely unattributed unsafe evidence remains fail-closed;
- search/filter/counters remain truthful;
- single-instance GUI open/focus behavior remains truthful and consistent;
- CLI Help colors work interactively and redirected output remains deterministic.

## 22. Release acceptance

After implementation and visual/function certification:

- build identity must be 0.0.15 everywhere;
- package name `MRC-v0.0.15-win-x64.zip`;
- manifest version/channel/stage/final target coherent;
- SHA256SUMS authority matches package bytes;
- packaged CLI reports 0.0.15;
- Git tag and release target exact-head match;
- prerelease remains clearly pre-certification;
- Main-PC `MRC --check` must discover 0.0.15 from installed 0.0.14;
- `MRC --update` must activate 0.0.15 and retain rollback;
- live Main-PC evidence must be collected before any final certification claim.

## 23. Non-goals

This design does not authorize:

- runner registration/unregistration;
- Windows service management;
- multi-machine fleet control;
- control of external/unattributed runners;
- a WebView2/browser rewrite;
- a new updater model;
- a new runner lifecycle implementation;
- removing safety confirmations for aesthetic reasons;
- final `v0.1.0` release before the full certification gate.

## 24. Definition of design-complete

The design is complete when the Owner confirms this written spec correctly captures the locked architecture. Only then should a detailed implementation plan be written. No implementation work should begin before that approval.
