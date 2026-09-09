# MRC V0.1 — Master Authority

**Project:** Main Runner Control (MRC)  
**Version target:** v0.1.0  
**Repository authority:** Git repository named exactly `MRC`  
**Primary machine:** `Main-PC` only  
**Runner root authority:** `D:\Git_Runners_Main` only

## 1. Purpose
MRC is a small Windows utility for discovering, viewing, starting, and safely stopping the GitHub Actions self-hosted runners that belong to Main-PC under the single authorized runner root `D:\Git_Runners_Main`.

MRC is intentionally not a GitHub management suite, runner installer, service manager, or multi-machine fleet manager. It is a local Main-PC runner control center.

## 2. Hard Safety Boundary
MRC MUST:
- operate only on Main-PC;
- discover/control runners only beneath `D:\Git_Runners_Main`;
- never touch runners on other machines;
- never touch runners outside that root;
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

## 5. Canonical Runtime States
MRC V0.1 uses exactly these user-visible states:
- `OFF`
- `STARTING`
- `IDLE`
- `BUSY`
- `STOPPING`
- `ERROR`

State derivation is governed by `0004_State.md`.

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

## 9. Canonical CLI
One PATH command: `MRC`.

Supported V0.1 command surface:
- `MRC` → open GUI;
- `MRC -v`, `MRC -version`, `MRC --version`;
- `MRC -update`, `MRC --update`;
- `MRC -doctor`, `MRC --doctor`;
- `MRC -h`, `MRC -help`, `MRC --help`.

Do not create separate `MRC-version` or `MRC-update` executables as the canonical design.

## 10. Distribution Model
MRC is distributed through GitHub Releases from the `MRC` repository.

Release versioning uses semantic tags such as:
- `v0.1.0`
- `v0.1.1`
- `v0.2.0`
- `v1.0.0`

V0.1 release artifact target:
- `MRC-v0.1.0-win-x64.zip`
- `SHA256SUMS.txt`

Local installation should be user-scoped and PATH-based, without requiring Administrator rights by default.

## 11. Five Implementation Passes
V0.1 implementation is divided into exactly five planned passes:
1. Foundation + Distribution
2. Runner Engine
3. Elite UI
4. Operations + Updating
5. Certification + Release

The detailed pass authority is `0010_Passes.md`.

## 12. Release Gate
Do not call V0.1 certified or PASS while any material requirement is unresolved, untested, or contradicted by live Main-PC evidence.

Writing code is not sufficient evidence. Actual launch, discovery, state detection, busy protection, stop behavior, PATH invocation, version command, update path, and UI behavior must be tested before release certification.

## 13. Continuation Rule
Implementation should continue in a chat that has the Git/GitHub plugin available. Do not depend on Doonchy Bridge for MRC implementation; the user explicitly stated that Doonchy Bridge does not work reliably enough yet for this task.

All future work must preserve these authorities unless the Owner explicitly changes them.
