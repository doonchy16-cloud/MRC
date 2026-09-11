# MRC v0.0.15 — Post-Release Live Certification Handoff

**Status:** OWNER-AUTHORIZED CONTINUATION / DURABLE HANDOFF  
**Date:** 2026-09-11  
**Project:** Main Runner Control (MRC)  
**Final product target:** `v0.1.0`  
**Current pre-cert release:** `v0.0.15`  
**Implementation branch:** `precert-v0.0.15`

> This file supersedes only the implementation-state / next-step claims in `Auth/0017_V0015_ReferenceReplica.md` Section 17 where they conflict with this later verified state. All locked design, safety, scope, visual, lifecycle, and certification authority in `Auth/0000_MasterAuth.md` through `Auth/0017_V0015_ReferenceReplica.md` remains in force.
>
> Do not rewrite history to make this documentation commit the release candidate. The published `v0.0.15` prerelease is intentionally and immutably anchored to the exact release candidate identified below.

## 1. Exact current repository and release state

Repository: `doonchy16-cloud/MRC`

Working/continuation branch: `precert-v0.0.15`

Published exact release candidate and tag target:

`d8b1235ef6e0e10fbe659b8134a1b49b0d212a52`

Commit message:

`release: trigger exact-head v0.0.15 prerelease`

The branch may contain documentation-only commits after `d8b1235...`. Those later documentation commits are NOT the released candidate and MUST NOT cause the `v0.0.15` tag/release to move.

`v0.0.14` remains frozen historical/live evidence and MUST NEVER be mutated or republished under different bytes.

## 2. Task 12 is certified complete

Task 12, the v0.0.15 visual convergence/certification gate, completed before release authorization.

All thirteen applicable visual categories reached exactly `10.0/10`:

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

The exact release trigger states that Task 12 visual certification reached `13/13 categories at 10.0/10`.

Do NOT reopen or redo Tasks 1-12 unless live Main-PC evidence in Task 14 reveals a genuine defect. If live evidence does reveal a defect, reopen only that focused slice using RED -> exact failure -> minimal GREEN -> full verification -> fresh evidence.

## 3. Task 13 exact-head prerelease is complete

Task 13 has completed successfully.

Development workflow on the release-trigger head:

- workflow: `MRC v0.0.15 Pre-Cert Development`;
- run ID: `34583586214`;
- run number: `122`;
- exact head: `d8b1235ef6e0e10fbe659b8134a1b49b0d212a52`;
- result: SUCCESS.

Release workflow:

- workflow: `Pre-cert v0.0.15 Release`;
- run ID: `34583586540`;
- run number: `1`;
- exact head: `d8b1235ef6e0e10fbe659b8134a1b49b0d212a52`;
- result: SUCCESS.

Published GitHub release:

- release ID: `386915416`;
- tag: `v0.0.15`;
- target commitish: exactly `d8b1235ef6e0e10fbe659b8134a1b49b0d212a52`;
- name: `MRC v0.0.15 Reference Replica Pre-Cert Build`;
- prerelease: true;
- draft: false;
- published at: `2026-09-11T09:22:01Z`.

Published package:

- `MRC-v0.0.15-win-x64.zip`;
- release asset SHA-256 digest: `7393dd843ef7940c3cf5c2213a5732be3f77a9f8bb22bdb0e19cdfb7d4e9f860`;
- `SHA256SUMS.txt` is also published.

The release contains the eight mandatory rendered evidence frames:

- `MRC-v0.0.15-2048x1222.png`;
- `MRC-v0.0.15-1200x760.png`;
- `MRC-v0.0.15-1180x760.png`;
- `MRC-v0.0.15-900x560.png`;
- `MRC-v0.0.15-900x560-busy.png`;
- `MRC-v0.0.15-1200x760-drawer.png`;
- `MRC-v0.0.15-1200x760-system-drawer.png`;
- `MRC-v0.0.15-2048x1222-mixed.png`.

The release pipeline verifies the full inherited ladder, deterministic renders, package identity, checksum, manifest/version/channel/stage/final-target identity, packaged CLI/GUI/icon authority, exact-head non-force tag provenance, and frozen-v0.0.14 isolation.

## 4. Task 14 is the sole next task

No verified evidence in this handoff proves that the real Main-PC has completed the `v0.0.14 -> v0.0.15` update and live certification gauntlet.

Therefore final product certification remains:

`HOLD`

Task 14 is now the ONLY planned execution task remaining for v0.0.15.

Before doing anything, read the exact Task 14 section from:

`docs/superpowers/plans/2026-09-10-mrc-v0.0.15-reference-replica.md`

at the current branch HEAD. Do not reconstruct Task 14 from this handoff alone if the plan provides more exact instructions.

Expected pre-update live state on Main-PC, unless new live evidence proves otherwise:

- installed/current version: `0.0.14`;
- available version: `0.0.15`;
- updater status: `UPDATE AVAILABLE`.

Expected post-update identity:

- version: `0.0.15`;
- channel: `precert`;
- stage: `PRE-CERTIFICATION`;
- final target: `0.1.0`.

## 5. Task 14 live acceptance scope

The real Main-PC live gauntlet must verify, at minimum, the applicable locked behaviors from the plan and authority, including:

- updater discovers and installs `v0.0.15` through the normal verified SHA-256 / manifest / immutable-install / atomic-activation / rollback path;
- installed CLI reports exact v0.0.15 pre-cert identity;
- Help semantic colors are correct live, while redirected output remains deterministic plain text;
- bare `MRC` opens or focuses exactly one GUI instance and repeated invocation does not create a duplicate GUI;
- reference-quality live GUI remains visually acceptable on the real machine;
- managed runner discovery is truthful;
- EXTERNAL / UNATTRIBUTED findings remain separate and read-only;
- external Lotto runner remains external and untouched;
- START is allowed only from verified OFF;
- normal STOP protects BUSY runners;
- RESTART remains verified STOP -> OFF -> START;
- BUSY FORCE STOP remains explicitly confirmed and destructive;
- TURN ALL ON targets only eligible OFF runners;
- TURN ALL OFF stops only verified IDLE runners and skips BUSY runners;
- repeated START / STOP / RESTART sequences do not create duplicate listener sessions;
- GUI close/reopen is lifecycle-neutral;
- Doctor and Diagnose remain within their locked repair/read-only boundaries;
- system/control drawers, responsive layout, dark scrollbars, state orbs, animation, and accessibility remain healthy under live operation;
- all applicable live categories independently reach exactly `10.0/10`.

If any live category is below 10.0, or any requirement is unverified, final status remains HOLD.

## 6. Safety boundaries remain absolute

Do not weaken or bypass:

- exact machine identity `DOONCHYSCOMPUTI`;
- exact managed runner root `D:\Git_Runners_Main`;
- MANAGED / EXTERNAL / UNATTRIBUTED separation;
- external Lotto isolation;
- exact listener-path association;
- BUSY protection;
- explicit FORCE STOP confirmation;
- bulk safety;
- duplicate-session protections;
- Doctor / Diagnose boundaries;
- update checksum / manifest / atomic activation / rollback requirements;
- single-instance GUI behavior;
- shared 20 FPS animation architecture;
- deterministic redirected CLI output.

MRC must never register/unregister GitHub runners, edit runner credentials, globally stop unrelated runner processes, control runners outside `D:\Git_Runners_Main`, or mutate the external Lotto runner.

## 7. Live-tooling honesty

If the continuation runtime does not expose a trustworthy way to interact with the real Main-PC, do NOT invent Task 14 results and do NOT mark the live gauntlet PASS.

In that case:

- preserve `HOLD`;
- verify everything still possible through GitHub/release evidence;
- clearly identify the missing live capability;
- do not republish `v0.0.15` merely to compensate for missing live access;
- do not move or rewrite the `v0.0.15` tag;
- do not mutate `v0.0.14`.

If live evidence reveals a defect requiring code changes, use mandatory TDD and produce a new coherent pre-cert candidate/version rather than silently replacing already-published `v0.0.15` bytes under the same semantic version.

## 8. Execution/tooling truth

The Owner selected Subagent-Driven Development in the original plan. During the prior implementation runtime, no genuine subagent/task-dispatch tool was exposed. That limitation was disclosed rather than inventing implementation-worker/reviewer results.

If a future runtime has genuine subagents, follow the Owner-approved plan. If not, continue with exact-SHA review, narrow diffs, mandatory TDD, full Windows CI, rendered/live evidence, and explicit HOLD/PASS gates.

## 9. Exact continuation entrypoint

A fresh chat/agent should:

1. recover live GitHub state first and confirm this handoff is still the latest authority;
2. read `Auth/0000_MasterAuth.md`;
3. read this file, `Auth/0018_V0015_LiveHandoff.md`;
4. read `Auth/0017_V0015_ReferenceReplica.md` for locked visual/safety authority;
5. read the locked v0.0.15 spec and implementation plan;
6. read the exact Task 14 plan section at current HEAD;
7. confirm the published `v0.0.15` release still targets `d8b1235ef6e0e10fbe659b8134a1b49b0d212a52` and remains a prerelease;
8. do NOT redo Tasks 1-13;
9. begin Task 14 live Main-PC update/certification if real-machine tooling is available;
10. otherwise preserve HOLD and report the exact live-access blocker without fabricating evidence.

For live visual comparison, retrieve the Owner-authorized 2048x1222 Runner Control reference screenshot from the user's File Library rather than reconstructing it from prose.

Final product target remains `v0.1.0`.
