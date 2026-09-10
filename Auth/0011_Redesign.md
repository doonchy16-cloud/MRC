# MRC V0.0.12 — Root Redesign Authority

**Owner approval:** 2026-09-09  
**Branch:** `redesign-v0.0.12`  
**Baseline:** `precert-v0.0.11`  
**Final target remains:** `v0.1.0`

## 1. Authority Status
This file is an Owner-approved amendment to the existing MRC V0.1 authority set.

Where this file conflicts with `0000_MasterAuth.md` through `0010_Passes.md`, this file governs the v0.0.12 redesign and all subsequent pre-certification work unless the Owner explicitly changes it.

All non-conflicting prior safety and scope authorities remain in force.

## 2. Root Redesign Principle
V0.0.12 is not a patch bundle. It is a root-level redesign that preserves proven safety/update engineering while rebuilding the runtime attribution model, diagnostics, CLI presentation, release-stage handling, single-instance behavior, GUI presentation architecture, animation presentation, icon/identity integration, and verification strategy as coherent subsystems.

The implementation SHALL satisfy the complete 90-item requirement set recorded in:

`docs/superpowers/specs/2026-09-09-mrc-v0.0.12-root-redesign-design.md`

## 3. Machine and Root Authority
Authorized Windows machine identity is exactly:

`DOONCHYSCOMPUTI`

Authorized managed-runner root remains exactly:

`D:\Git_Runners_Main`

The human label `Main-PC` is descriptive only and is not the OS identity fence.

## 4. Runtime Ownership Classes
Every observed GitHub runner process SHALL be classified into exactly one of:

- `MANAGED` — ownership is proven to a discovered configured runner beneath the authorized root;
- `EXTERNAL` — ownership is proven outside the authorized root, including known Windows-service runners such as the Lotto runner;
- `UNATTRIBUTED` — the process is runner-related but exact ownership cannot be proven.

External and unattributed processes are never controllable by MRC.

An external or unattributed process MUST NOT force all managed runners into `ERROR` unless its ambiguity can materially overlap ownership of a particular managed runner.

## 5. Real-Machine Acceptance Fixture
The Owner supplied this required live acceptance case:

- 7 managed `Runner.Listener.exe` processes beneath `D:\Git_Runners_Main`;
- all 7 in the interactive user session;
- 0 `Runner.Worker.exe` processes;
- 1 external Lotto `Runner.Listener.exe` in Session 0;
- Lotto parent service: `C:\actions-runner-lotto-mainpc\bin\RunnerService.exe`;
- Lotto Windows service name: `actions.runner.doonchy16-cloud-Lotto_engine.Lotto_MainPC_Runner`.

Expected managed runtime truth for that fixture:

- TOTAL = 7
- IDLE = 7
- BUSY = 0
- OFF = 0
- ERROR = 0
- TRANSITION = 0

The Lotto runner appears only as an external warning and is never controlled.

## 6. Doctor and Diagnose
Canonical deep forensic command SHALL be added:

- `MRC -diagnose`
- `MRC --diagnose`

`MRC -diagnose` is read-only.

`MRC -doctor` SHALL become a repair-capable workflow:

`FIND -> REPAIR -> VERIFY -> FINAL HEALTH`

Doctor may automatically repair only low-risk MRC-owned state. Destructive, runner-affecting, process-affecting, or service-affecting repairs require explicit approval.

Doctor MUST re-run affected checks before reporting a repair as successful.

Final Doctor health state SHALL be one of:

- `HEALTHY`
- `REPAIRED`
- `ATTENTION`
- `BLOCKED`

## 7. Folder Classification
Immediate child directories beneath the authorized root SHALL be classified as:

- `MANAGED RUNNER`
- `BROKEN RUNNER CANDIDATE`
- `NON-RUNNER FOLDER`

A directory is not a failed runner merely because it exists beneath the root.

Known non-runner folders from live testing include:

- `dash-echo`
- `Forgey_Trainer`
- `HardwareMonitor`
- `RA-OS`
- `Subagents_runner`

These MUST NOT fail Doctor unless future evidence shows meaningful partial runner artifacts.

## 8. CLI Presentation Authority
All canonical CLI commands SHALL use one semantic rendering system.

Semantic colors:

- green — PASS / success / verified;
- red — FAIL / blocked / error;
- yellow — warning / attention / retained rollback;
- cyan — headings / actions / paths;
- purple — versions / build metadata;
- gray — secondary technical information.

Plain-text fallback is required when color is unavailable or output is redirected.

Bare `MRC` MUST print launch feedback. If the GUI is already running, MRC MUST report that and restore/focus the existing window instead of silently launching-and-exiting a duplicate process.

## 9. Release-Stage Authority
Pre-certification builds MUST be represented truthfully.

The model SHALL distinguish at minimum:

- semantic version;
- release channel;
- release stage;
- final target version.

An intentional `0.0.x` pre-certification build MUST NOT be reported as an invalid version by Doctor merely because final target is `0.1.0`.

The updater MUST be able to resolve allowed pre-certification builds without relying on misclassifying them as final/stable releases.

## 10. GUI Authority Amendment
The prior generic dashboard visual result is rejected by live Owner review.

V0.0.12 SHALL use a dense native Windows terminal/PowerShell operations-console presentation:

- near-black shell;
- strong monospace typography;
- compact inline status strip instead of large metric cards;
- runner table as the dominant visual surface;
- reduced whitespace;
- flatter geometry;
- compact terminal-like controls;
- dedicated external/system warning area;
- MRC-styled diagnostics instead of generic Windows message boxes;
- preserved accessibility and semantic state communication.

Existing state palette remains authoritative unless explicitly amended later.

## 11. Animation Authority Amendment
The one-shared-clock rule remains authoritative.

V0.0.12 uses state-specific terminal animation while preserving the locked cadences:

- `IDLE` — breathing sequence at 550 ms;
- `BUSY` — fast work spinner at 110 ms;
- `STARTING` — filling sequence at 180 ms;
- `STOPPING` — draining sequence at 250 ms;
- `OFF` — static `-`;
- `ERROR` — static `!`.

A bounded shared-clock `AnimationIntensity` signal may vary visible glyph opacity for active states. It MUST NOT alter `RunnerState`, and rows MUST NOT own independent timers.

## 12. Icon Authority
MRC SHALL have a real application identity icon, used for:

- executable resources;
- GUI title bar;
- taskbar;
- Alt-Tab;
- installed shortcuts;
- package/install identity.

The icon SHALL remain recognizable at Windows small-icon sizes.

## 13. Safety Preservation
The redesign MUST preserve:

- exact machine fence;
- exact root fence;
- exact managed-process ownership before control;
- no control of external/unattributed runner processes;
- no default Administrator requirement;
- BUSY protection;
- explicit BUSY force-stop confirmation;
- TURN ALL OFF skipping BUSY;
- closing MRC never stopping runners;
- SHA-256 verification;
- package validation;
- atomic activation;
- launch verification;
- rollback retention.

## 14. Certification Gate
V0.0.12 is not PASS merely because the 90 requirements are implemented.

Final certification requires:

- targeted and regression automated tests;
- actual Windows PowerShell/Terminal CLI review;
- rendered GUI review at approximately 1180x760 and 900x560;
- live Main-PC acceptance using the 7-managed + 1-external Lotto fixture;
- every applicable review category scoring exactly 10.0/10.

Any category below 10.0/10 or any unresolved/unverified requirement means HOLD.

## 15. V0.0.12 Pre-Certification Release Evidence
The root redesign implementation reached its automated pre-certification artifact gate and was published for Owner/Main-PC acceptance on 2026-09-09 Pacific time / 2026-09-10 UTC.

Canonical release:

- tag: `v0.0.12`;
- release title: `MRC v0.0.12 — Root Redesign Pre-Cert Build`;
- GitHub release ID: `385927198`;
- release target commit retained by GitHub release metadata: `43a4a4e366d23623bf835a70961f0c51d0510810`;
- final bootstrap-repair asset producer branch commit: `c22a0f6e53656352be0972a154fd471451fa4f6f`;
- final successful release/asset-replacement workflow run: `34422351936` — SUCCESS;
- package: `MRC-v0.0.12-win-x64.zip`;
- current package SHA-256: `0eba9134acfc5b2679d9312ec01dace9d67611f79ddb78c7887553cf76933071`;
- checksum authority: `SHA256SUMS.txt`;
- canonical preview assets: `MRC-v0.0.12-1180x760.png` and `MRC-v0.0.12-900x560.png`;
- current 1180x760 preview SHA-256: `d2e8f538e1c8f63d760249d4e8b0b2c2c2b4cc4b5fb80273e46de6bf445eda21`;
- current 900x560 preview SHA-256: `4e9811df3e4639a8193b541ddabf0266e7d7c5d09cdb4508423adc74643b8373`.

The final release/asset-replacement workflow run `34422351936` independently passed:

- v0.0.12 redesign acceptance harness;
- PASS4 operations/updater regression;
- PASS3 presentation/animation/preview regression;
- PASS2 runtime regression;
- PASS1 foundation/distribution regression;
- both canonical WPF preview renders;
- package build;
- SHA-256 and manifest verification;
- packaged executable version/stage/final-target verification;
- publication/replacement of the bootstrap-compatible release assets.

The release is intentionally represented by GitHub with `prerelease=false` ONLY as a bootstrap compatibility bridge because installed v0.0.11 clients resolve the legacy `/releases/latest` endpoint, which excludes GitHub prereleases. This GitHub transport flag does not change product authority: v0.0.12 reports `channel=precert`, `stage=PRE-CERTIFICATION`, and `final target=0.1.0`. After v0.0.12 is installed, its redesigned updater enumerates authorized releases and can discover true prerelease metadata.

Earlier publication/replacement attempts exposed bootstrap/package compatibility defects and were superseded. The authoritative downloadable assets are the assets currently attached to release ID `385927198`, generated by successful run `34422351936` from branch head `c22a0f6e53656352be0972a154fd471451fa4f6f`.

## 16. Remaining Final-Certification Boundary
Publication of v0.0.12 authorizes installation and live acceptance testing; it is NOT final certification.

The Owner/Main-PC acceptance sequence includes:

1. Update an installed v0.0.11 to v0.0.12 successfully.
2. Run `MRC --version` and verify `Version: 0.0.12`, `Channel: precert`, `Stage: PRE-CERTIFICATION`, and final target `0.1.0`.
3. Launch bare `MRC`; verify launch feedback, terminal-console GUI, application icon, and repeated-launch restore/focus behavior.
4. With the known real fixture of seven managed interactive listeners and zero workers, verify managed counters are exactly TOTAL 7 / IDLE 7 / BUSY 0 / OFF 0 / ERROR 0 / TRANSITION 0.
5. Verify Lotto remains visible only as an external/system warning and is never offered a MRC control path.
6. Run `MRC -doctor` and `MRC -diagnose`; review truthful repair/verification and forensic output on Main-PC.
7. Review the real GUI at normal viewing size and the minimum supported size; any clipping, crowding, weak hierarchy, poor animation, generic/debug aesthetics, misleading status, or accessibility regression is a certification blocker.
8. Score runtime safety, Doctor/Diagnose, CLI UX, updater, GUI visual design, animation, accessibility, packaging, regression engineering, and live Main-PC acceptance separately. Every applicable category must be exactly 10.0/10.

Until those live/visual checks pass, v0.0.12 remains a PRE-CERTIFICATION build and final `v0.1.0` certification remains HOLD.

## 17. Main-PC Installation Evidence — 2026-09-09 Pacific
Owner live evidence confirms the final bootstrap-repaired v0.0.12 package successfully updated the installed Main-PC copy from v0.0.11 to v0.0.12.

Observed successful update result:

`MRC updated from 0.0.11 to 0.0.12. Previous version retained for rollback.`

Observed `MRC --version` result after update:

- `Version: 0.0.12`
- `Channel: precert`
- `Stage: PRE-CERTIFICATION`
- `Final target: 0.1.0`
- install location: `C:\Users\doonc\AppData\Local\MRC\versions\0.0.12`
- runner root: `D:\Git_Runners_Main`

This closes the v0.0.11 -> v0.0.12 bootstrap installation/version-identity acceptance step. It does NOT by itself certify runtime state, Lotto isolation, Doctor/Diagnose live behavior, GUI visual quality, or final v0.1.0.

The Owner also identified a CLI presentation refinement for the next pre-cert build: label tokens should render white while their associated values/status tokens retain the semantic color they deserve. That v0.0.13 amendment is governed separately by `Auth/0012_V0013.md` on the v0.0.13 continuation branch.
