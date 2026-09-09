# MRC V0.1 — Runtime State Authority

## Canonical States
MRC exposes exactly:
- OFF
- STARTING
- IDLE
- BUSY
- STOPPING
- ERROR

## Exact Association Principle
State must be determined per runner using exact executable/process-tree association to that runner directory.

Expected listener executable:
`<runner>\bin\Runner.Listener.exe`

Expected worker executable:
`<runner>\bin\Runner.Worker.exe`

Never infer per-runner state merely by counting global process names.

## State Rules
### OFF
No listener associated with this runner is present.

### STARTING
ON was requested and the listener has not yet been confirmed.

### IDLE
Listener exists for this runner and no active worker associated with this runner is present.

### BUSY
Listener exists and an associated worker for the same runner is active.

### STOPPING
OFF was requested and the runner process tree has not yet fully exited.

### ERROR
Use when a configured runner exists but runtime or launch evidence is inconsistent, malformed, denied, or contradictory.

Examples:
- launch requested but process never appears;
- expected executable missing after initial discovery;
- process path cannot be safely associated;
- permission denial prevents truthful inspection/control;
- malformed `.runner` metadata prevents safe identity.

## Refresh Model
Runtime scanning should occur on an approximately 3-second cadence in V0.1.

Manual refresh must remain available.

State refresh must not reorder rows or rebuild the entire UI in a way that causes flicker/jumping.

## Truthfulness Rule
Animation and button state are presentation layers only. They never determine runtime state.
