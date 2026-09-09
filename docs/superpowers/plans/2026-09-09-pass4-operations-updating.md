# PASS 4 Operations + Updating Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task. Every production behavior follows RED -> GREEN -> regression verification.

**Goal:** Complete MRC v0.1's operational command surface without weakening the verified PASS 1-3 safety, runtime-truth, UI, or distribution boundaries.

**Architecture:** PASS 2 remains the low-level runner process/state authority. PASS 4 adds explicit orchestration for individual/bulk operations and force-stop confirmation, then adds CLI diagnostic and updater services behind injectable interfaces. GUI controls call only the PASS 4 orchestration surface. Update code is isolated from `D:\Git_Runners_Main` and uses immutable version directories plus an atomic `current` activation pointer. No release publication occurs in PASS 4; PASS 5 owns real Main-PC certification and release.

**Tech Stack:** .NET 10.0.401, C#, WPF, PowerShell packaging, GitHub Releases API, GitHub Actions Windows runner.

**Spec:** `Auth/0002_CLI.md`, `Auth/0005_Safety.md`, `Auth/0008_Update.md`, `Auth/0009_Test.md`, `Auth/0010_Passes.md`.

## Global Constraints

- BUSY is protected from every normal stop path.
- Force-stop BUSY is a secondary action requiring explicit confirmation and exact ownership re-verification.
- TURN ALL OFF never force-stops and reports idle-stop count plus BUSY-skip count.
- TURN ALL ON starts only OFF runners and staggers attempted launches by approximately 150 ms.
- Closing MRC never stops runners.
- No default Administrator requirement or silent elevation.
- `MRC -doctor` and `MRC --doctor` are equivalent.
- `MRC -update` and `MRC --update` are equivalent.
- Update flow: installed version -> latest allowed release -> staging download -> SHA-256 verification -> version-specific extraction -> payload validation -> atomic activation -> launch/version verification -> previous-version retention.
- Any update failure leaves the active version unchanged and produces a clear error.
- Updater must never mutate runner configuration, runner packages, or `D:\Git_Runners_Main`.
- PASS 4 does not tag/publish `v0.1.0`; PASS 5 owns release certification/publication.
- Every successful PASS 4 gate reruns PASS 3, PASS 2, and PASS 1 verification.

---

### Task 1: Operations policy core

**Files:**
- Create: `tests/MRC.Pass4.Tests/MRC.Pass4.Tests.csproj`
- Create: `tests/MRC.Pass4.Tests/OperationsAcceptance.cs`
- Create: `scripts/verify-pass4.ps1`
- Create: `.github/workflows/pass4.yml`
- Create/extend: `src/MRC.Core/Control/*`
- Extend: `src/MRC.Core/RunnerEngine.cs`

**Acceptance:**
- individual OFF -> STARTING uses existing safe `Start`;
- individual IDLE -> STOPPING uses existing safe `StopIdle`;
- individual BUSY normal OFF remains blocked;
- BUSY force stop requires an explicit confirmation token/boolean before any termination attempt;
- confirmed force-stop still re-verifies immediate-child signature, listener identity/path, and exact runner association before kill;
- TURN ALL ON targets only OFF runners and delays approximately 150 ms between launch attempts;
- TURN ALL OFF targets only IDLE runners, skips BUSY, and returns exact attempted/succeeded/skipped/error counts;
- controller close has no runner lifecycle side effect.

- [ ] Write failing operations tests.
- [ ] Run PASS 4 RED; earlier passes stay green.
- [ ] Implement minimum operations orchestration and force terminator.
- [ ] Run PASS 4 GREEN plus PASS 1-3 regression.

---

### Task 2: GUI operational controls + confirmation UX

**Files:**
- Extend: `src/MRC.Gui/MainWindow.xaml`
- Extend: `src/MRC.Gui/MainWindow.xaml.cs`
- Extend: `src/MRC.Gui/Presentation/RunnerRowViewModel.cs`
- Extend PASS 4 UI acceptance tests.

**Acceptance:**
- TURN ALL ON and TURN ALL OFF are visible primary operations;
- TURN ALL OFF confirmation states exact IDLE stop count and BUSY remain-running count;
- per-runner OFF/ON controls invoke the orchestration layer only when state permits;
- BUSY row exposes force-stop only as secondary/ellipsis action;
- force-stop warning explicitly communicates active-job interruption and requires confirmation;
- clear error feedback is visible without row reordering/jumping;
- preview mode remains non-operational;
- visual regression produces fresh 1180x760 and 900x560 rendered previews.

- [ ] Write failing GUI operations tests.
- [ ] Run RED.
- [ ] Implement GUI operations and confirmation dialogs.
- [ ] Render/inspect both preview geometries and fix every visual defect.
- [ ] Run GREEN plus PASS 1-3 regression.

---

### Task 3: Doctor diagnostics

**Files:**
- Create: `src/MRC.Core/Diagnostics/*`
- Extend: `src/MRC.Cli/CliDispatcher.cs`
- Extend PASS 4 tests.

**Acceptance:**
`MRC -doctor` / `--doctor` reports structured PASS/WARN/FAIL diagnostics for at minimum:
- machine identity;
- authorized runner-root existence;
- PATH installation;
- installed-version authority;
- discovered runner count;
- configured-runner signature completeness;
- update/release endpoint reachability when applicable.

Doctor is read-only and never starts/stops runners or mutates install state.

- [ ] Write failing doctor/CLI tests.
- [ ] Run RED.
- [ ] Implement injectable doctor checks and concise formatter.
- [ ] Run GREEN plus prior regressions.

---

### Task 4: Safe release resolution + checksum verification

**Files:**
- Create: `src/MRC.Core/Update/*`
- Extend PASS 4 tests.

**Acceptance:**
- resolve latest allowed GitHub Release metadata without activating anything;
- select the expected `MRC-v<version>-win-x64.zip` and `SHA256SUMS.txt` assets;
- reject missing/ambiguous assets;
- parse checksum authority safely;
- SHA-256 compare is case-insensitive and exact;
- checksum mismatch aborts before extraction/activation.

- [ ] Write failing resolver/hash tests.
- [ ] Run RED.
- [ ] Implement resolver/download abstractions and checksum verifier.
- [ ] Run GREEN plus prior regressions.

---

### Task 5: Staged install + atomic activation + rollback retention

**Files:**
- Extend: `src/MRC.Core/Update/*`
- Extend PASS 4 tests.

**Acceptance:**
- download only to staging;
- extract only to a new version-specific directory;
- reject path traversal and invalid payloads;
- validate required CLI/GUI payload files before activation;
- update an indirection/pointer atomically rather than overwriting active files;
- preserve previous active version metadata/path;
- failed candidate leaves the previous active version usable;
- activation verification failure restores/preserves previous version;
- no path under `D:\Git_Runners_Main` can be an update target.

- [ ] Write failure-injection tests for each stage.
- [ ] Run RED.
- [ ] Implement staged installer/activator.
- [ ] Run GREEN plus prior regressions.

---

### Task 6: `MRC -update` CLI workflow

**Files:**
- Extend: `src/MRC.Cli/CliDispatcher.cs`
- Add/update CLI dependencies and PASS 4 tests.

**Acceptance:**
- `-update` and `--update` equivalent;
- concise progress: current version, latest version, download, verification, install/activation, success/failure;
- no-op success when already current;
- all errors are explicit and nonzero;
- no partial activation.

- [ ] Write failing CLI update tests.
- [ ] Run RED.
- [ ] Wire updater service into dispatcher.
- [ ] Run GREEN plus prior regressions.

---

### Task 7: PASS 4 elite certification gate

**Evidence required before PASS 4 can score 10.0/10:**
- all PASS 4 operations tests green;
- all GUI operation/confirmation tests green;
- both fresh rendered GUI previews inspected at realistic size;
- all doctor tests green;
- release resolver and SHA-256 tests green;
- failure-injection update/atomicity/rollback tests green;
- CLI doctor/update tests green;
- PASS 1, PASS 2, and PASS 3 workflows green on the same final PASS 4 SHA;
- no unresolved safety, boundary, interaction, update, or visual finding.

PASS 4 remains HOLD if any one category is below 10.0/10 or any material behavior is unverified. PASS 5 is not opened until this gate closes.
