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
