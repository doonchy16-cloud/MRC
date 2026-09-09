# MRC V0.1 — Five Implementation Passes Authority

MRC v0.1.0 is implemented in exactly five major passes.

## PASS 1 — Foundation + Distribution
Build the project foundation only.

Required outcomes:
- repository structure in `MRC`;
- version authority for `0.1.0`;
- one canonical PATH command `MRC`;
- CLI dispatcher;
- `MRC` GUI launch entry;
- `-v/-version/--version`;
- `-h/-help/--help` foundation;
- install/PATH architecture;
- release packaging skeleton;
- Main-PC machine fence;
- `D:\Git_Runners_Main` root constant/fence;
- no runner start/stop implementation yet.

## PASS 2 — Runner Engine
Implement the non-UI control/state core.

Required outcomes:
- immediate-child discovery;
- runner signature validation;
- `.runner` parsing;
- repository-name derivation;
- exact listener/worker association;
- OFF/STARTING/IDLE/BUSY/STOPPING/ERROR model;
- safe `run.cmd` launch;
- IDLE stop behavior;
- BUSY protection;
- process-tree ownership fencing.

## PASS 3 — Elite UI
Implement the locked visual/interaction layer.

Required outcomes:
- dark palette exactly aligned to authority;
- white labels;
- cyan runner/repo identities;
- semantic state colors;
- dense scalable table;
- ~42 px rows;
- state stripe;
- spinner column;
- search;
- filters;
- counters;
- stable sorting;
- non-jumping refresh;
- shared animation timer;
- responsive resizing.

## PASS 4 — Operations + Updating
Finish operational command surface.

Required outcomes:
- TURN ALL ON;
- TURN ALL OFF with BUSY skip;
- per-runner controls;
- force-stop confirmation path;
- clear error handling;
- `MRC -doctor/--doctor`;
- `MRC -update/--update`;
- release resolution;
- SHA-256 verification;
- atomic activation;
- retained previous version/rollback foundation.

## PASS 5 — Certification + Release
Validate real behavior and cut the release.

Required outcomes:
- execute the full verification matrix in `0009_Test.md`;
- fix all material findings;
- verify Git cleanliness and intended diff;
- package `MRC-v0.1.0-win-x64.zip`;
- generate `SHA256SUMS.txt`;
- verify release artifact hashes;
- publish/tag `v0.1.0` only after release gate passes.

## Pass Discipline
- Do not smuggle later-pass features into an earlier pass unless required for testability/safety.
- Preserve prior verified behavior while advancing.
- Each pass should end with targeted tests/evidence appropriate to that pass.
- Do not call the whole product PASS before Pass 5 certification.
