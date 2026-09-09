# MRC V0.1 — Control & Safety Authority

## Core Principle
A BUSY runner represents an active GitHub Actions job and must be protected from accidental interruption.

## Single Runner Start
When OFF:
1. launch the runner's own `run.cmd`;
2. use the runner folder as working directory;
3. run hidden/background to avoid console-window explosion;
4. set UI to STARTING;
5. confirm listener association before reporting IDLE/BUSY.

## Single Runner Normal Stop
Normal OFF is permitted only for IDLE runners.

Sequence:
1. re-verify exact runner/process association;
2. verify runner is not BUSY;
3. transition to STOPPING;
4. request/perform runner-tree shutdown;
5. verify associated processes exited;
6. report OFF.

If a clean shutdown mechanism is not practical in V0.1, bounded process-tree termination may be used only after exact path/ownership verification and only when the runner is not BUSY.

## BUSY Protection
Normal individual OFF must not stop a BUSY runner.

Force-stop of BUSY:
- hidden behind `...` / secondary action;
- explicit warning;
- explicit confirmation;
- never part of TURN ALL OFF.

## TURN ALL ON
Starts every OFF runner.

For large runner counts, stagger launches slightly (approximately 150 ms between starts) to avoid unnecessary simultaneous process-creation spikes.

## TURN ALL OFF
Stops only IDLE runners.

Skips BUSY runners.

Confirmation must communicate exact effect, e.g.:
- number of idle runners that will stop;
- number of busy runners that will remain running.

## Controller Close Behavior
Closing MRC must NOT stop runners.

MRC is a controller/dashboard, not the parent lifecycle authority for all runner operation.

## Privilege Model
Do not require Administrator privileges by default.

If process access is denied, surface a permission error and provide a diagnostic path rather than silently escalating.
