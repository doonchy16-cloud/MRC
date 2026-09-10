# MRC V0.1 — Master Authority

**Project:** Main Runner Control (MRC)  
**Version target:** v0.1.0  
**Repository authority:** Git repository named exactly `MRC`  
**Authorized Windows identity:** `DOONCHYSCOMPUTI` only  
**Human machine label:** `Main-PC`  
**Runner root authority:** `D:\Git_Runners_Main` only

> **V0.0.12 redesign amendment:** `Auth/0011_Redesign.md` is Owner-approved and supersedes any conflicting pre-redesign detail in `0000_MasterAuth.md` through `0010_Passes.md` for v0.0.12 and subsequent pre-certification work. All non-conflicting safety/scope authority remains in force.
>
> **V0.0.13 CLI-color amendment:** `Auth/0012_V0013.md` is Owner-approved and governs segmented CLI coloring for v0.0.13 and later: human-readable labels are white; dynamic values/results/status tokens use their semantically appropriate colors; redirected output remains plain text.

## 1. Purpose
MRC is a small Windows utility for discovering, viewing, starting, and safely stopping the GitHub Actions self-hosted runners that belong to the authorized Main-PC under the single authorized runner root `D:\Git_Runners_Main`.

MRC is intentionally not a GitHub management suite, runner installer, service manager, or multi-machine fleet manager. It is a local Main-PC runner control center.

## 2. Hard Safety Boundary
MRC MUST:
- operate only when the Windows machine identity is exactly `DOONCHYSCOMPUTI`;
- discover/control managed runners only beneath `D:\Git_Runners_Main`;
- never touch runners on other machines;
- never control runners outside that root;
- never control external or unattributed runner processes;
- never register/unregister GitHub runners;
- never edit runner credentials or configuration;
- never manage runners as Windows services;
- never stop a BUSY runner through normal OFF or TURN ALL OFF;
- require explicit confirmation for any force-stop of a BUSY runner.

## 3. Runner Package Facts
A representative configured runner confirmed the standard GitHub Actions Windows runner chain:

`run.cmd` → `run-helper.cmd` → `bin\Runner.Listener.exe run`

Representative `.runner` metadata provides:
- `agentName`
- `gitHubUrl`
- `agentId`
- `workFolder`

Representative package version observed during planning: `2.337.0.0`.

MRC MUST launch a runner through its own `run.cmd`, not by directly invoking `Runner.Listener.exe`.

## 4. Discovery Authority
V0.1 discovers only immediate child directories of `D:\Git_Runners_Main`.

A valid configured runner requires all of:
- `.runner`
- `run.cmd`
- `run-helper.cmd.template`
- `bin\Runner.Listener.exe`

`.runner` is the display/identity metadata authority. Filesystem + exact process association are runtime-state authority.

For v0.0.12 and later pre-certification work, non-runner child folders are explicitly classified and must not be treated as failed runners merely because they exist beneath the root; see `0011_Redesign.md`.

## 5. Canonical Runtime States
MRC V0.1 uses exactly these user-visible managed-runner states:
- `OFF`
- `STARTING`
- `IDLE`
- `BUSY`
- `STOPPING`
- `ERROR`

State derivation is governed by `0004_State.md` plus the v0.0.12 ownership-isolation amendment in `0011_Redesign.md`.

## 6. Canonical Per-Runner Row
Each runner row shows:
- thin state-colored left stripe;
- animated/frozen ASCII state glyph;
- runner name from `.runner.agentName`;
- repository name derived from `.runner.gitHubUrl`;
- state text;
- compact control.

Canonical row model:

`┃ /  DBridge_MAIN   Doonchy_Bridge   IDLE   [ OFF ]`

Full paths, PIDs, agent IDs, versions, and other technical metadata stay out of the main row and may appear in tooltip/details UI.

## 7. Locked Visual Language
Base theme: dark charcoal / near-black.

Primary palette:
- background `#0B0F14`
- panel `#111821`
- raised panel `#18212B`
- border `#263342`
- white labels `#F4F7FA`
- secondary text `#AAB6C3`
- muted text `#667381`
- runner cyan `#35D9FF`
- repo cyan `#6BE6FF`
- technical blue `#74A9D8`
- IDLE green `#39E58C`
- BUSY yellow `#FFD166`
- OFF red `#FF5C6C`
- ERROR orange `#FF8A3D`
- STARTING blue `#4CA7FF`
- STOPPING purple `#B48CFF`
- count purple `#C792EA`

Labels/headings are white. Runner/repository identity is cyan. States use state-fitting semantic colors.

For CLI output beginning with v0.0.13, label/value coloring is tokenized: labels are white while values/results/status tokens carry their deserving semantic colors. See `0012_V0013.md`.

The v0.0.12 terminal/PowerShell shell redesign in `0011_Redesign.md` supersedes the rejected dashboard presentation while preserving this semantic palette unless explicitly changed by the Owner.

## 8. Locked Animation Language
Per-runner state indicator uses the ASCII sequence:

`/ - \\ |`

Behavior:
- OFF: frozen `-`, red;
- IDLE: slow spin, green;
- BUSY: fast spin, yellow;
- STARTING: medium-fast spin, blue;
- STOPPING: medium spin, purple;
- ERROR: frozen `!`, orange.

Use one shared UI animation timer, never a separate timer per row.

The v0.0.12 state-specific terminal animation and shared-intensity amendment in `0011_Redesign.md` supersedes conflicting frame details here.

## 9. Canonical CLI
One PATH command: `MRC`.

Supported command surface:
- `MRC` → open/focus GUI and print launch feedback;
- `MRC -v`, `MRC -version`, `MRC --version`;
- `MRC -update`, `MRC --update`;
- `MRC -doctor`, `MRC --doctor`;
- `MRC -diagnose`, `MRC --diagnose`;
- optional targeted diagnose form `MRC -diagnose <runner-name>` / `MRC --diagnose <runner-name>`;
- `MRC -h`, `MRC -help`, `MRC --help`.

Do not create separate `MRC-version`, `MRC-update`, `MRC-doctor`, or `MRC-diagnose` executables as the canonical design.

## 10. Distribution Model
MRC is distributed through GitHub Releases from the `MRC` repository.

Final release versioning uses semantic tags such as:
- `v0.1.0`
- `v0.1.1`
- `v0.2.0`
- `v1.0.0`

Pre-certification `0.0.x` builds may be distributed for live Main-PC validation and must carry truthful release-stage metadata.

V0.1 final release artifact target:
- `MRC-v0.1.0-win-x64.zip`
- `SHA256SUMS.txt`

Local installation should be user-scoped and PATH-based, without requiring Administrator rights by default.

## 11. Five Implementation Passes
The final V0.1 implementation/certification lifecycle remains divided into five planned passes:
1. Foundation + Distribution
2. Runner Engine
3. Elite UI
4. Operations + Updating
5. Certification + Release

Live v0.0.12 redesign evidence may reopen conclusions from earlier pre-certification pass reviews without weakening the final five-pass release gate.

The detailed pass authority is `0010_Passes.md` plus the v0.0.12 certification amendment in `0011_Redesign.md`.

## 12. Release Gate
Do not call V0.1 certified or PASS while any material requirement is unresolved, untested, or contradicted by live Main-PC evidence.

Writing code is not sufficient evidence. Actual launch, discovery, state detection, busy protection, stop behavior, PATH invocation, version command, update path, Doctor/Diagnose behavior, GUI behavior, and rendered/live visual quality must be tested before release certification.

Every applicable review category must score exactly 10.0/10; any lower category, unresolved defect, or unverified requirement means HOLD.

## 13. Continuation Rule
Implementation should continue in a chat that has the Git/GitHub plugin available. Do not depend on Doonchy Bridge for MRC implementation; the user explicitly stated that Doonchy Bridge does not work reliably enough yet for this task.

All future work must preserve these authorities unless the Owner explicitly changes them.
