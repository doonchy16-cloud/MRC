# MRC v0.0.15 — Owner-Authored Reference Replica Authority

**Status:** OWNER-APPROVED / LOCKED  
**Owner source:** live Main-PC review and explicit lock, 2026-09-10  
**Implementation version:** `v0.0.15` pre-certification  
**Planned implementation branch:** `precert-v0.0.15`  
**Baseline:** installed and published `v0.0.14` live-evidence build  
**Final target remains:** `v0.1.0`

## 1. Why v0.0.15 is required

The Owner has already installed `v0.0.14` on Main-PC. MRC update selection compares semantic versions and reports UP TO DATE when installed and available versions are equal. Therefore the reference-replica rebuild MUST NOT be published by silently replacing `v0.0.14` bytes under the same semantic version. The next installable pre-cert candidate SHALL be `v0.0.15` so Main-PC can discover and activate it through the normal verified updater.

`v0.0.14` is frozen as the live baseline and evidence source for the redesign findings.

## 2. Visual-source authority

The Owner explicitly confirmed that the supplied `Runner Control` reference screenshot is the Owner's own work and authorized direct reproduction. Therefore:

- the reference is the primary visual authority for the v0.0.15 primary control screen;
- 1:1 reproduction is authorized wherever MRC functionality permits;
- prior language in `Auth/0015_V0014_ReferenceRecovery.md` stating that the reference is "not a pixel copy" is superseded;
- the goal is not merely to borrow traits, but to reproduce the reference's composition, card geometry, spacing, visual hierarchy, color atmosphere, control proportions, state presentation, and overall visual rhythm as faithfully as practical in native WPF;
- MRC-specific safety, data, commands, and state truth remain authoritative whenever functionality differs from the reference.

## 3. Primary-screen doctrine

The primary MRC screen SHALL visually prioritize the runner control surface in the same way as the reference.

The always-visible primary composition SHALL contain:

1. reference-style hero/header;
2. reference-style live/control-ready status capsule;
3. compact dense runner grid;
4. minimal status/system surface that does not displace the runner grid.

MRC instrumentation that does not exist in the reference SHALL move to compact secondary surfaces instead of permanently occupying a large horizontal slab above the cards.

The following remain available but secondary:

- TOTAL / IDLE / BUSY / OFF / ERROR / TRANSITION counters;
- ALL / IDLE / BUSY / OFF / ERROR filters;
- search;
- TURN ALL ON / TURN ALL OFF;
- detailed system findings;
- manual refresh;
- deep runner diagnostics/details.

A compact menu/control drawer is the preferred secondary home for counters, filters, search, and bulk controls. System/external findings remain separately identifiable and may use a bottom drawer/strip.

## 4. Canonical reference geometry

The Owner-supplied reference screenshot is `2048x1222`. Reference-derived target geometry for the large-screen primary control surface is:

- four runner columns;
- approximately 41 px left card-field inset;
- approximately 469 px card width;
- approximately 22 px inter-column gap;
- approximately 146–154 px card height;
- approximately 22 px inter-row gap;
- approximately 18–19 px inner horizontal card padding;
- approximately 137 x 46 px START button at reference scale;
- three equal primary action slots per card;
- wide, compact card aspect near 3.1:1.

These are reconstruction targets, not permission to break safety or accessibility. Small anti-aliasing/rendering differences are acceptable; structural visual drift is not.

## 5. Responsive doctrine

Reference geometry is the large-screen master. Smaller layouts SHALL preserve control size and legibility by reducing column count before materially shrinking controls.

Target responsive outcomes:

- sufficiently wide / approximately 1900 px client width: 4 columns;
- approximately 1400–1899 px: 3 columns;
- approximately 950–1399 px: 2 columns;
- below approximately 950 px usable surface: 1 column.

Implementation SHOULD calculate column count from available runner-surface width and a reference-safe card width rather than rely only on brittle viewport magic numbers.

Mandatory visual acceptance viewports remain:

- `2048x1222`;
- `1200x760`;
- `1180x760`;
- `900x560`.

At 2048, the current eight managed Main-PC runners SHALL naturally form two compact rows of four when unfiltered.

## 6. Runner-card anatomy

Each card SHALL reproduce the reference's compact control-module anatomy:

- luminous state orb/housing at upper-left;
- dominant runner name;
- compact secondary technical identity line;
- state badge at upper-right;
- optional small details affordance outside the primary action rail;
- three-slot primary lifecycle rail near the lower card edge.

The card SHALL NOT contain a large unused vertical interior. Strength comes from density, contrast, glow, and control scale rather than height.

### Primary action rail

The reference's three-button anatomy is authoritative.

- OFF: START active; STOP and RESTART disabled.
- IDLE: START disabled; STOP and RESTART active.
- BUSY: the first slot becomes explicit FORCE STOP; ordinary STOP and RESTART remain protected/disabled according to existing core authority.
- STARTING / STOPPING: unsafe controls disabled.
- ERROR: diagnostic/details path is emphasized; lifecycle mutation remains fail-closed.

`DETAILS` is secondary and SHALL NOT consume a fourth primary lifecycle slot on the default card.

FORCE STOP remains a distinct destructive operation and MUST retain the existing explicit confirmation warning. Reusing the first visual slot when BUSY does not merge FORCE STOP with START semantically.

## 7. State presentation

The small terminal-like square/dot treatment is superseded for the card UI. Each runner SHALL use a reference-style circular luminous state orb with:

- dark outer housing;
- visible central light;
- state-colored halo;
- restrained glow;
- state-appropriate animation/intensity.

State semantics remain:

- IDLE: green;
- BUSY: amber/yellow;
- OFF: cool gray/subdued;
- STARTING: blue;
- STOPPING: purple;
- ERROR: orange/red.

The existing shared 20 FPS animation architecture remains mandatory. No per-card timers.

## 8. Card visual treatment

At large-screen reference scale:

- card surface: dark blue-charcoal raised module;
- border: subtle cool gray edge;
- corners: reference-matched rounded treatment;
- shadow: soft black depth;
- IDLE: clearly visible green edge/glow;
- BUSY: amber/yellow state presence;
- OFF: cool neutral border and subdued orb;
- ERROR: orange/red urgency;
- hover: restrained elevation/border-brightness increase.

The enabled START control SHALL be a bright filled warm amber/yellow button with dark text and reference-level visual priority. It must look intentionally active, not merely technically amber.

STOP and RESTART SHALL be substantial equal-scale controls with reference-like outline treatment. Disabled controls SHALL be unmistakably disabled.

## 9. Header and atmosphere

The primary header SHALL reproduce the reference's hierarchy:

- small machine/local-control eyebrow;
- very large `MAIN RUNNER CONTROL` hero title;
- reference-style menu/control affordance at left;
- large live/control-ready capsule at upper-right;
- small monospace local-truth/refresh metadata beneath the capsule.

The background SHALL be a layered visual composition rather than a single flat color:

- deep near-black/navy upper field;
- subtle cool haze;
- warm amber illumination/bloom biased toward lower/right regions;
- restrained edge vignette;
- no external wallpaper dependency required.

Native WPF gradients, radial glows, effects, and vector surfaces are preferred.

## 10. Secondary instrumentation architecture

The Owner locked the reference-first primary screen. Therefore extra MRC instrumentation SHALL not permanently dominate the primary layout.

Preferred structure:

- reference-style menu button opens a compact Control Drawer;
- Control Drawer contains search, filters, counters, TURN ALL ON, TURN ALL OFF, and manual refresh;
- keyboard `/` continues to focus search or open/focus search in the drawer;
- system/external findings remain in a separate bottom System Drawer/strip;
- runner diagnostics open from a small details affordance or existing diagnostic dialog.

Bulk-action safety semantics remain unchanged. TURN ALL OFF never force-stops BUSY runners.

## 11. CLI Help color correction

The existing three-column Help table structure remains approved. Only the canonical command-token color grammar changes:

- literal `MRC` command token: YELLOW / warm command color;
- command option token such as `--help`, `--check`, `--update`: CYAN;
- aliases: GRAY / secondary;
- descriptions: WHITE;
- redirected output remains deterministic plain text.

Example semantic rendering:

`MRC --update`
- `MRC` => yellow;
- space => neutral;
- `--update` => cyan.

The heading may likewise visually distinguish the `MRC` product token from `// COMMAND REFERENCE`.

## 12. Implementation architecture

The reconstruction SHALL remain native WPF. Do not introduce WebView2, embedded browser UI, or a third-party UI framework solely to reproduce the reference.

The presentation layer SHOULD be decomposed into focused units rather than allowing `MainWindow.xaml` to become the permanent monolith. Intended conceptual units:

- AmbientBackground;
- HeroHeader;
- RunnerGrid;
- RunnerControlCard;
- StateOrb;
- ThreeSlotActionRail;
- ControlDrawer;
- SystemDrawer.

The underlying runner engine, operations service, updater, CLI dispatcher, release authority, ownership detection, and safety systems are not visual implementation targets and SHALL remain behaviorally unchanged except where a separately approved defect requires correction.

## 13. Visual-difference acceptance

The large-screen visual gate SHALL compare the v0.0.15 candidate directly against the Owner reference, not merely ask whether the candidate looks generally polished.

Review categories include:

- hero/title scale;
- card-field inset;
- card width and height;
- four-column composition;
- row/column gaps;
- orb geometry and luminosity;
- state badge geometry;
- action-button dimensions and balance;
- amber START strength;
- IDLE glow strength;
- background amber bloom placement;
- typography hierarchy;
- spacing rhythm;
- density and absence of dead card space.

A reference-overlay/difference artifact SHOULD be generated for large-screen review so drift is visible rather than left to memory.

## 14. Preservation boundary

This authority does not permit weakening or bypassing:

- exact machine/root authority;
- MANAGED / EXTERNAL / UNATTRIBUTED ownership separation;
- external Lotto isolation;
- START / STOP / RESTART lifecycle verification;
- BUSY protection;
- FORCE STOP confirmation;
- bulk safety;
- duplicate-session protections;
- Doctor / Diagnose boundaries;
- update SHA-256 / manifest / atomic activation / rollback;
- single-instance GUI behavior;
- 20 FPS shared animation architecture;
- deterministic redirected CLI output.

## 15. Certification gate

No visual or product PASS until:

- reference-replica automated contracts are GREEN;
- full inherited regression ladder is GREEN;
- fresh mandatory renders are produced;
- 2048 render is directly compared with the Owner reference;
- 1200, 1180, and 900 remain readable and unclipped;
- Help command-token colors are verified live;
- `v0.0.15` package/tag/manifest/checksum identity is coherent;
- Main-PC installs `v0.0.15` through the normal verified updater;
- live GUI visual review passes;
- repeated START / STOP / RESTART and bulk operation acceptance passes;
- no applicable review category is below exactly 10.0/10.

Until then: HOLD.

## 16. Owner lock and execution handoff — 2026-09-10

The Owner explicitly approved the written design with `LOCKED!`.

Durable design/spec:

- `docs/superpowers/specs/2026-09-10-mrc-reference-replica-design.md`

Durable implementation plan:

- `docs/superpowers/plans/2026-09-10-mrc-v0.0.15-reference-replica.md`

The implementation plan is complete and contains its own self-review. Do **not** recreate or replace it at handoff unless a new Owner requirement conflicts with it.

The Owner selected execution **Option 1: Subagent-Driven Development**. Execution SHALL use `superpowers:subagent-driven-development`, with a fresh implementation worker per task and review gates between tasks, following the committed plan.

Current handoff state:

- v0.0.14 remains the installed/published/frozen baseline;
- v0.0.15 design is locked;
- v0.0.15 implementation plan is complete;
- no v0.0.15 implementation task has begun;
- no product code should be changed on `precert-v0.0.14`;
- execution begins by creating/using `precert-v0.0.15` from this fully preserved planning baseline;
- TDD remains mandatory for every implementation slice;
- no visual PASS may be inferred from structure/tests without rendered evidence and direct comparison to the Owner reference.

Exact Owner visual reference retrieval requirement:

- reference image name: `Screenshot 2026-09-09 214130.png`;
- it is the Owner-authored Runner Control screenshot used to define this authority;
- a fresh chat/agent MUST retrieve and inspect that exact image from the user's File Library before implementing or reviewing visual tasks;
- do not reconstruct the reference from prose alone.

Handoff execution entrypoint: read this authority, the locked spec, and the committed implementation plan; retrieve the exact Owner reference image; create/use `precert-v0.0.15`; then begin Task 1 of the plan with Subagent-Driven Development. Do not skip directly to GUI implementation.

## 17. Superseding implementation checkpoint — 2026-09-11

This section supersedes the stale implementation-state bullets in Section 16. The design/authority portions above remain locked.

### Exact repository state

- repository: `doonchy16-cloud/MRC`;
- active implementation branch: `precert-v0.0.15`;
- exact implementation head before this documentation checkpoint: `157a52c2f39c7c2c9a93196efa9a592cc5a8cb5f`;
- commit message: `fix: make system drawer dismissible and visually isolated`;
- exact-head Windows workflow run: `34580892411` / run number `115`;
- run #115 result: SUCCESS across redesign, PASS4, PASS3, PASS2, PASS1, render matrix, evidence upload, and cleanup;
- run #115 GUI artifact: `MRC-v0.0.15-gui-previews`, artifact id `10191635708`, archive digest `sha256:334b9f8458ba53be8711903ef9c1d0070ea54dbd55edf6fde9550f2e383d3370`;
- v0.0.14 remains frozen historical/live evidence. Never mutate or republish v0.0.14 bytes.

After this authority checkpoint commit, the branch HEAD will naturally advance by one documentation-only commit. Implementation evidence remains anchored to `157a52c2...` until a later production/test change creates a new candidate.

### Execution/tooling truth

TDD remains mandatory: RED -> prove exact intended failure -> minimal GREEN -> exact-head Windows verification -> regression ladder -> rendered evidence -> visual review where applicable.

The requested subagent-driven workflow could not be literally satisfied in the active ChatGPT runtime because no genuine subagent/task-dispatch tool was exposed. This limitation was disclosed rather than inventing worker/reviewer results. Continue using exact-SHA review, narrow diffs, TDD, full CI, and explicit rendered evidence unless a future runtime exposes real subagents.

### Implemented plan state

Plan Tasks 1 through 11 are implemented and verified. Task 12, the visual convergence/certification gate, is in its final evidence phase. Task 13 exact-head prerelease and Task 14 Main-PC live update gauntlet have NOT begun and remain locked until Task 12 is fully certified.

Key locked production outcomes already implemented include:

- exact v0.0.15 identity and frozen v0.0.14 historical boundary;
- reference-responsive metrics and native WPF replica theme;
- compact extracted `RunnerControlCard` with three lifecycle slots;
- BUSY first-slot `FORCE STOP` with existing explicit destructive confirmation;
- 32x32 luminous `StateOrb` driven by shared `AnimationIntensity`, no per-card timers;
- exact header strings `MAIN PC • LOCAL-FIRST CONTROL` and `MAIN RUNNER CONTROL`;
- five-layer ambient background;
- left Control Drawer with search/filters/counters/bulk controls;
- read-only bottom System Drawer for EXTERNAL/UNATTRIBUTED evidence;
- Help token grammar and bare-MRC single-instance behavior from the committed plan;
- deterministic render matrix and reference-comparison tooling;
- one shared 20 FPS animation timer and one refresh timer;
- all lifecycle operations still route through `RunnerOperationsService`.

### Task 12 focused convergence findings closed before handoff

The following focused findings have been TDD-proved, implemented, fully regression-verified, and pixel-reviewed at the point stated:

- F-V15-004: 150 px visible card body + 22 px inter-card gap; outer card footprint is 172 px so 11 px top/bottom spacing does not steal from the visible body. CLOSED 10.0/10.
- F-V15-005: ambient depth; preserved proven lower-right amber anchor and lifted separate under-card warmth. CLOSED 10.0/10.
- F-V15-006: enabled secondary STOP/RESTART rails use restrained warm-gold outlines. CLOSED 10.0/10.
- F-V15-007: menu button changed from solid yellow tile to dark surface with amber glyph/rim and restrained halo. CLOSED 10.0/10.
- F-V15-008: removed the giant runner-field dashboard frame; cards float over ambient field while scrolling/containment remain intact. CLOSED 10.0/10.
- F-V15-009: card metadata legibility raised without crowding: repo 11 px, path 10 px, stronger path contrast. CLOSED 10.0/10.
- F-V15-010: runner-list scrollbar now truly renders with shared dark scrollbar chrome; deterministic preview now initializes real `App.xaml` resources. CLOSED 10.0/10.
- F-V15-011: deterministic canonical fixture now contains exactly 15 managed preview runners, producing the Owner-reference 4+4+4+3 wide-screen density while live runtime inventory remains truthful. CLOSED 10.0/10.
- F-V15-013: System Drawer findings scrollbar uses the shared dark scrollbar style and was pixel-reviewed dark at run #113. CLOSED 10.0/10.

### System Drawer evidence chain and current unresolved gate

F-V15-012 added the missing deterministic System Drawer evidence path: `--drawer system` plus `MRC-v0.0.15-1200x760-system-drawer.png`. The first successful System Drawer render exposed two genuine defects rather than earning an automatic PASS:

1. native/light findings scrollbar, fixed and pixel-verified by F-V15-013;
2. translucent expanded drawer allowed the collapsed summary strip to ghost through, and the expanded overlay had no deterministic close path because it covered the opening toggle and Escape only handled the Control Drawer.

F-V15-014 addressed the second defect at exact implementation head `157a52c2...`:

- expanded System Drawer outer surface is opaque `#0B1013`;
- compact custom `CLOSE` affordance added inside the drawer;
- `SystemDrawer.CloseRequested` is presentation-only and contains no `RunnerOperationsService`, `RunnerEngine`, or lifecycle authority;
- MainWindow routes `CloseRequested` to `SystemFindingsPanel.IsChecked = false`;
- Escape also dismisses the System Drawer;
- existing read-only evidence boundary, 35% max-height authority, runner grid layout, dark scrollbar, and runtime separation remain preserved;
- run #115 is fully GREEN, including fresh render generation.

**Important unresolved handoff action:** the run #115 artifact was downloaded, but the final fresh `MRC-v0.0.15-1200x760-system-drawer.png` from `157a52c2...` was NOT visually inspected before handoff mode began. Therefore F-V15-014 and the overall System Drawer category remain HOLD, not PASS, despite structural/CI green.

First action in the next chat MUST be to retrieve/download run #115 artifact `10191635708`, inspect `MRC-v0.0.15-1200x760-system-drawer.png` at realistic size, and verify all of:

- no collapsed-summary ghosting through the expanded drawer;
- dark custom scrollbar remains rendered;
- CLOSE control is legible, visually coherent, and not oversized;
- drawer remains a bottom overlay within the locked 35% cap;
- runner field is not resized/reflowed by opening it;
- read-only evidence content remains readable and unclipped.

Only if every item passes may F-V15-014 be scored 10.0/10 and the System Drawer category close. If any pixel defect remains, open a new focused RED and continue TDD.

### Task 12 final certification requirements after System Drawer closes

Do a fresh exact-head review of all required visual evidence, not just structural tests:

- `MRC-v0.0.15-2048x1222.png`;
- `MRC-v0.0.15-1200x760.png`;
- `MRC-v0.0.15-1180x760.png`;
- `MRC-v0.0.15-900x560.png`;
- `MRC-v0.0.15-900x560-busy.png`;
- `MRC-v0.0.15-1200x760-drawer.png`;
- `MRC-v0.0.15-1200x760-system-drawer.png`;
- `MRC-v0.0.15-2048x1222-mixed.png`.

Also generate/re-generate from the latest exact candidate and exact Owner reference:

- `reference-side-by-side.png`;
- `reference-overlay-50.png`.

The comparison tooling already exists at `scripts/build-v0.0.15-reference-comparison.ps1`; CI currently uploads candidate renders but does not automatically include those two comparison files, so do not claim they exist unless actually generated for the latest candidate.

Exact Owner reference: retrieve from File Library using the `Screenshot 2026-09-09 214130...` name and verify it is the Owner-authorized 2048x1222 Runner Control screenshot. In the implementation session the usable local file was `/mnt/data/Screenshot 2026-09-09 214130(1).png`; a fresh chat must retrieve it again rather than assuming that sandbox path persists.

Final Task 12 certification categories, each independently required to be exactly 10.0/10:

1. composition;
2. hero/header;
3. card geometry/density;
4. state orb;
5. border/glow treatment;
6. action rail;
7. enabled/disabled clarity;
8. ambient background;
9. typography hierarchy;
10. responsiveness;
11. Control Drawer;
12. System Drawer;
13. accessibility/readability.

Any category below 10.0/10 or any unresolved/unverified visual requirement means Task 12 remains HOLD.

### Release lock and next tasks

Do NOT begin Task 13 while Task 12 is HOLD.

After Task 12 genuinely passes, read the exact committed Task 13 plan section from `docs/superpowers/plans/2026-09-10-mrc-v0.0.15-reference-replica.md` at the then-current HEAD before changing release files. Task 13 must produce an exact-head v0.0.15 prerelease package with coherent version/channel/stage/final-target metadata, checksum/manifest identity, packaged CLI identity, required visual evidence, and exact tag/trigger/release-head coherence. Preserve the frozen v0.0.14 release.

Only after Task 13 passes may Task 14 perform the live Main-PC update gauntlet: installed v0.0.14 should discover available v0.0.15 as UPDATE AVAILABLE, update through the normal SHA/manifest/atomic-activation/rollback path, then undergo live GUI, CLI, repeated START/STOP/RESTART, BUSY protection, bulk-operation, single-instance, Doctor/Diagnose, and reference-quality acceptance. Final product target remains v0.1.0.

Until all applicable certification and live gates are complete: HOLD.
