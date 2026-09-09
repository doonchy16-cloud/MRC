# PASS 2 Runner Engine Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement MRC V0.1 PASS 2 as a non-UI runner engine that discovers configured Main-PC runners, derives truthful per-runner state from exact process ownership, launches through each runner's `run.cmd`, stops only verified IDLE runner trees, and blocks normal BUSY interruption.

**Architecture:** Keep the engine inside `MRC.Core` and separate identity/discovery, process inspection/association, state evaluation, transition tracking, launch, and termination behind focused types. Runtime decisions are fail-closed: every control action re-checks the runner signature and exact process path immediately before acting, and worker BUSY association requires the worker to live at the same runner path and descend from that runner's listener. PASS 2 exposes no bulk operations and makes no UI changes beyond preserving PASS 1 compatibility.

**Tech Stack:** C# / .NET 10 LTS, `System.Text.Json`, `System.Diagnostics.Process`, Windows `QueryFullProcessImageName` via P/Invoke for exact executable-path inspection, existing executable test harness, GitHub Actions `windows-latest`.

**Spec:** `Auth/0003_Runner.md`, `Auth/0004_State.md`, `Auth/0005_Safety.md`, `Auth/0009_Test.md`, `Auth/0010_Passes.md`

## Global Constraints

- Work only on branch `main` per Owner instruction.
- Authorized machine remains exactly `Main-PC`.
- Authorized runner root remains exactly `D:\Git_Runners_Main`.
- Discover only immediate child directories of the authorized root.
- A configured-runner signature requires `.runner`, `run.cmd`, `run-helper.cmd.template`, and `bin\Runner.Listener.exe`.
- Launch only through the runner's own `run.cmd` with the runner directory as working directory.
- Never use global process-name counts as runner authority.
- Normal OFF must never stop BUSY.
- PASS 2 must not add TURN ALL ON/OFF, force-stop UI, doctor/update implementation, or PASS 3 visual behavior.
- Implementation choice required by the authority's "process never appears" ERROR rule: transient STARTING/STOPPING requests use a 30-second fail-closed timeout before ERROR; the timeout is an internal V0.1 engine constant, not a new user-facing authority.

---

### Task 1: PASS 2 RED acceptance harness

**Files:**
- Modify: `tests/MRC.Tests/MRC.Tests.csproj`
- Modify: `tests/MRC.Tests/Program.cs`
- Create: `scripts/verify-pass2.ps1`
- Create: `.github/workflows/pass2.yml`

**Interfaces:**
- Consumes: existing PASS 1 test harness.
- Produces: executable acceptance tests that require the PASS 2 types before those types exist.

- [ ] **Step 1: Add a project reference from `MRC.Tests` to `src/MRC.Core/MRC.Core.csproj`.**

```xml
<ItemGroup>
  <ProjectReference Include="..\..\src\MRC.Core\MRC.Core.csproj" />
</ItemGroup>
```

- [ ] **Step 2: Add PASS 2 tests before production code.** Tests must cover: immediate-child-only discovery; signature validation; valid and malformed `.runner`; repository-name derivation; exact listener association; worker ancestry; foreign executable rejection; OFF/STARTING/IDLE/BUSY/STOPPING/ERROR; launch through `run.cmd`; BUSY normal stop block; IDLE stop; and kill-time path re-verification.

- [ ] **Step 3: Add `scripts/verify-pass2.ps1` and a Windows PASS 2 workflow that runs the harness.**

```powershell
& dotnet run --project 'tests\MRC.Tests\MRC.Tests.csproj' -c Release
if ($LASTEXITCODE -ne 0) { throw "PASS 2 verification failed with exit code $LASTEXITCODE." }
```

- [ ] **Step 4: Push the test-only commit to `main` and verify GitHub Actions fails in the PASS 2 test step because the new engine types do not exist.**

- [ ] **Step 5: Commit.**

```text
test: define PASS 2 runner engine acceptance
```

---

### Task 2: Discovery and identity

**Files:**
- Create: `src/MRC.Core/Runners/RunnerDescriptor.cs`
- Create: `src/MRC.Core/Runners/RunnerDiscovery.cs`
- Create: `src/MRC.Core/Runners/RunnerIdentityParser.cs`
- Create: `src/MRC.Core/Runners/RunnerPath.cs`

**Interfaces:**
- Produces: `RunnerDescriptor`, `RunnerDiscovery.Discover(string root)`, `RunnerIdentityParser.Parse(string runnerDirectory)`, and path helpers used by process/control layers.

- [ ] **Step 1: Implement an immutable descriptor carrying runner directory, agent name, GitHub URL, repository name, agent ID, work folder, and optional identity error.**

- [ ] **Step 2: Implement top-directory-only discovery.** A folder is included only when all four signature files exist. Nested runner-shaped folders under `_work` or any other child subtree are never scanned.

- [ ] **Step 3: Parse `.runner` using `System.Text.Json`.** Missing/malformed identity becomes a descriptor with `IdentityError`, allowing the runtime state layer to surface ERROR instead of silently dropping a configured runner.

- [ ] **Step 4: Derive repository identity from the final non-empty URL path segment and preserve the source segment rather than inventing repository metadata.

- [ ] **Step 5: Run the harness; discovery/identity tests must pass while process/control tests remain red.**

---

### Task 3: Exact process ownership and state model

**Files:**
- Create: `src/MRC.Core/Runtime/RunnerState.cs`
- Create: `src/MRC.Core/Runtime/ProcessSnapshot.cs`
- Create: `src/MRC.Core/Runtime/IProcessSnapshotProvider.cs`
- Create: `src/MRC.Core/Runtime/WindowsProcessSnapshotProvider.cs`
- Create: `src/MRC.Core/Runtime/RunnerProcessAssociation.cs`
- Create: `src/MRC.Core/Runtime/RunnerTransition.cs`
- Create: `src/MRC.Core/Runtime/RunnerStateEvaluator.cs`

**Interfaces:**
- Produces: exact listener/worker association and canonical state derivation.

- [ ] **Step 1: Define exactly six public states: `OFF`, `STARTING`, `IDLE`, `BUSY`, `STOPPING`, `ERROR`.**

- [ ] **Step 2: Capture process ID, parent process ID, executable path, and inspection completeness.** Windows process path reads use `OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION)` + `QueryFullProcessImageName`; parent PID is read with `NtQueryInformationProcess`. Access failure is represented as incomplete evidence, never guessed into a healthy state.

- [ ] **Step 3: Associate a listener only when its executable path equals `<runner>\bin\Runner.Listener.exe` using normalized case-insensitive Windows path equality.**

- [ ] **Step 4: Associate BUSY worker evidence only when its path equals `<runner>\bin\Runner.Worker.exe` and its parent ancestry reaches the associated listener PID.** Cycle detection and a bounded ancestry walk prevent malformed process graphs from hanging state refresh.

- [ ] **Step 5: Implement state evaluation:** identity/inspection contradiction => ERROR; transient request => STARTING/STOPPING while expected observation has not arrived; no listener => OFF; listener/no associated worker => IDLE; listener + associated worker => BUSY; expired transition => ERROR.

- [ ] **Step 6: Run the harness; process/state tests must pass.**

---

### Task 4: Safe launch and normal stop

**Files:**
- Create: `src/MRC.Core/Control/RunnerControlResult.cs`
- Create: `src/MRC.Core/Control/IRunnerLauncher.cs`
- Create: `src/MRC.Core/Control/WindowsRunnerLauncher.cs`
- Create: `src/MRC.Core/Control/IRunnerProcessTerminator.cs`
- Create: `src/MRC.Core/Control/WindowsRunnerProcessTerminator.cs`
- Create: `src/MRC.Core/Control/RunnerControlService.cs`

**Interfaces:**
- Produces: `StartAsync(RunnerDescriptor, CancellationToken)` and `StopIdleAsync(RunnerDescriptor, CancellationToken)` with explicit non-destructive outcomes.

- [ ] **Step 1: Implement start through `cmd.exe /d /s /c` targeting exactly `<runner>\run.cmd`, with runner directory as working directory, hidden/no new window.** The control service re-validates the signature and requires observed OFF before launch.

- [ ] **Step 2: After start request, mark STARTING; later refresh confirms listener and clears the transition.** Launch failure or timeout becomes ERROR.

- [ ] **Step 3: Implement normal stop as a fresh runtime observation followed by an unconditional BUSY block.** If not IDLE, return a non-destructive result.

- [ ] **Step 4: For IDLE only, mark STOPPING and terminate the associated listener tree.** The terminator opens the PID again and re-reads its executable path immediately before `Kill(entireProcessTree: true)`; a mismatch/denial fails closed without killing.

- [ ] **Step 5: Verify no controller disposal/close path kills runners.** PASS 2 creates no lifecycle-owned child registry and does not stop runners on MRC exit.

- [ ] **Step 6: Run the full harness and preserve every PASS 1 test.**

---

### Task 5: PASS 2 verification gate

**Files:**
- Modify only if a defect is found by verification.

**Interfaces:**
- Consumes: entire PASS 2 implementation and CI evidence.
- Produces: a PASS/HOLD decision for PASS 2 implementation readiness, not whole-product certification.

- [ ] **Step 1: Run the complete Windows PASS 2 workflow on the final `main` commit.**

- [ ] **Step 2: Confirm all PASS 1 regression tests and all PASS 2 engine tests pass with zero warnings/errors.**

- [ ] **Step 3: Inspect the final Git tree to confirm no PASS 3/4 feature leakage.**

- [ ] **Step 4: Reconcile every PASS 2 required outcome in `Auth/0010_Passes.md` against direct test or code evidence.**

- [ ] **Step 5: If and only if every PASS 2 category is 10.0/10 with no unresolved material implementation defect, advance immediately to PASS 3 per Owner instruction.** Live Main-PC release certification remains reserved for PASS 5 under `Auth/0009_Test.md`.
