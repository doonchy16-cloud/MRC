# MRC v0.0.14 — Reference-First GUI Recovery Amendment

**Status:** OWNER-APPROVED  
**Owner source:** live Main-PC v0.0.14 review, 2026-09-10  
**Branch:** `precert-v0.0.14`  
**Supersedes:** only conflicting GUI visual/layout details in `Auth/0014_V0014.md`  
**Preserves:** all v0.0.14 control/runtime/CLI/safety behavior and all v0.0.13 historical evidence

## 1. Owner finding
The live v0.0.14 Command Center is functionally improved but visually drifted too far from the supplied Runner Control reference. It is too flat, too sparse inside cards, too restrained in control scale, and too close to a generic admin dashboard. The terminal/CLI is good and must remain unchanged by this amendment.

## 2. Visual authority
The supplied Runner Control screenshot becomes the primary **visual reference** for the v0.0.14 GUI recovery. It is not a pixel copy and its product identity must not be copied. MRC must adopt the useful interaction/visual traits while retaining MRC naming, semantics, colors, safety, runtime truth, and diagnostics.

Required traits:
- strong application hierarchy, not fake-terminal chrome;
- dimensional dark control-room background rather than a single flat navy field;
- clearly separated runner control modules;
- large runner identity and obvious state badge/indicator;
- large, obvious START / STOP / RESTART controls;
- clear enabled/disabled action states;
- healthy/active runner surfaces may use restrained green edge/glow treatment;
- warm amber/gold becomes the primary START/action emphasis;
- cyan is reserved mainly for information, technical identity, restart/action-secondary use;
- system findings remain visually separate from managed runner cards;
- technical paths/build/PID/session details may retain monospace styling;
- the GUI must feel like a purpose-built local runner controller, not a terminal table and not a generic consumer dashboard.

## 3. Responsive card doctrine
The prior 3-column-large-screen guidance is superseded.

Target layout:
- large desktop / approximately 1600 px and wider: **4 columns**;
- default desktop / approximately 1100–1599 px: **3 columns**;
- compact / approximately 800–1099 px: **2 columns**;
- narrower than approximately 800 px usable card surface: **1 column**;
- the 1200x760, 1180x760, 900x560, and 2048x1222 acceptance viewports remain mandatory;
- cards must size to their content/controlled target height rather than stretching into giant empty vertical panels;
- scrolling is allowed when needed;
- all eight current Main-PC managed runners should present as two strong rows at large 4-column width when all are visible.

## 4. Header / hierarchy
The top area must read as a real application hero/header:
- strong `MAIN RUNNER CONTROL` title;
- visible local-control context and machine/root metadata;
- authorization/control-ready pill or badge is prominent but not oversized;
- version/build identity remains secondary technical metadata;
- counters remain distinct visual modules rather than terminal text separated by pipes;
- more breathing room between header, counters, controls, and runner surface.

## 5. Runner card anatomy
Each runner card must contain:
- state orb/indicator;
- runner name as dominant text;
- repository as secondary technical identity;
- state badge on the card header;
- optional technical metadata area only when useful;
- bottom action rail with explicit START / STOP / RESTART and secondary DETAILS;
- actions must be visually large enough to feel like real controls rather than footer links;
- action rail must not float at the top of a tall empty card;
- cards must not be excessively tall on 2048x1222.

## 6. Action visual semantics
- START: primary warm amber/gold action when enabled;
- STOP: red/danger action when enabled;
- RESTART: cyan/blue secondary lifecycle action when enabled;
- DETAILS: quiet secondary action;
- disabled actions: unmistakably disabled with reduced contrast and no misleading active glow;
- BUSY protection and FORCE STOP confirmation semantics remain unchanged;
- enabled visual state must continue to match executable core authority.

## 7. Preservation boundary
This amendment must not modify:
- `MRC --help`, `--check`, `--update`, progress bars, or other CLI presentation unless separately authorized;
- start/stop/restart lifecycle semantics;
- bulk-control safety;
- BUSY force confirmation;
- managed/external/unattributed ownership rules;
- exact-path authority;
- duplicate-session protections;
- Doctor / Diagnose safety boundaries;
- updater hash/manifest/atomic activation/rollback behavior;
- 20 FPS shared animation architecture.

## 8. Acceptance
No GUI visual PASS until all are true:
- automated visual-structure contract is GREEN;
- complete inherited regression ladder is GREEN;
- fresh 2048x1222, 1200x760, 1180x760, and 900x560 renders are generated;
- rendered previews are inspected at realistic size;
- large screen visibly uses 4 columns and avoids giant empty cards;
- default/compact layouts remain readable and unclipped;
- all enabled lifecycle controls remain functional in automated architecture tests;
- live Main-PC GUI review confirms the reference-first direction;
- live Main-PC repeated START / STOP / RESTART and bulk operations still pass.

Global certification rule remains unchanged: every applicable category must be exactly 10.0/10 or certification remains HOLD.
