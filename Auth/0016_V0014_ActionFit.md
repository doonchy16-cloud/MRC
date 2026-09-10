# MRC v0.0.14 — Action-Rail Fit Correction

**Status:** BOUNDED CORRECTION REQUIRED BY `Auth/0015_V0014_ReferenceRecovery.md` ACCEPTANCE  
**Evidence source:** rendered v0.0.14 preview set from workflow run `34451540142`, 2026-09-10  
**Branch:** `precert-v0.0.14`  
**Supersedes:** only the approximate responsive breakpoints in `Auth/0015_V0014_ReferenceRecovery.md` where they conflict with unclipped action controls  
**Preserves:** all runtime, lifecycle, safety, CLI, updater, Doctor/Diagnose, animation, and visual-semantic authority

## 1. Rendered evidence finding

The first fully GREEN automated reference-recovery candidate rendered correctly at 2048x1222 but failed visual acceptance at smaller mandatory viewports:

- **2048x1222:** four-column cards remain readable and BUSY `FORCE STOP` is fully visible.
- **1200x760:** three-column BUSY cards lose `FORCE STOP`; the action rail is too wide for the card.
- **1180x760:** three-column action rails visibly clip; `DETAILS` can be cut at the card edge and BUSY `FORCE STOP` is not fully available.
- **900x560:** two-column BUSY cards visibly clip `FORCE STOP` to a partial `FO...` control.

This is a hard visual/usability failure under Section 8 of the reference-recovery authority. Automated GREEN cannot override rendered clipping.

## 2. Corrected responsive doctrine

Column count must now be constrained by the minimum width required for the complete runner action rail, not by optimistic fixed viewport breakpoints.

Current v0.0.14 control geometry requires a **minimum target card slot width of 470 px** before allocating another column. The responsive calculation must remain bounded to **1 through 4 columns**.

For the mandatory acceptance viewports, the required result is:

- `2048x1222` → **4 columns**
- `1200x760` → **2 columns**
- `1180x760` → **2 columns**
- `900x560` → **1 column**

Intermediate widths may resolve dynamically from the same minimum-slot-width rule. Approximately 1600 px should naturally resolve to three columns with the current shell geometry.

## 3. Action-fit law

The complete enabled action set must remain visually available without horizontal clipping:

- START
- STOP
- RESTART
- DETAILS
- FORCE STOP when BUSY

Do **not** solve fit failures by shrinking the 44 px lifecycle-control height, materially reducing readable typography, or hiding FORCE STOP behind a generic menu. Prefer fewer columns and scrolling.

## 4. Preservation boundary

This correction must not change:

- control eligibility or `CanStart` / `CanStop` / `CanRestart` semantics;
- BUSY force-stop confirmation or safety behavior;
- managed/external/unattributed ownership rules;
- start/stop/restart/bulk lifecycle orchestration;
- CLI behavior or presentation;
- Doctor / Diagnose behavior;
- updater verification, atomic activation, or rollback;
- 20 FPS shared animation architecture;
- the 4-column maximum on sufficiently wide displays.

## 5. Acceptance

No visual PASS until fresh mandatory renders prove:

- no card action clips at any canonical viewport;
- BUSY `FORCE STOP` is fully visible at every canonical viewport;
- 44 px control height is preserved;
- 2048 remains four columns;
- 1200 and 1180 use two columns;
- 900 uses one column;
- full inherited regression ladder remains GREEN;
- live Main-PC review still passes.

Global certification remains unchanged: every applicable category must be exactly 10.0/10 or status remains HOLD.
