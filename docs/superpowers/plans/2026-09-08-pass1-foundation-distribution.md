# MRC PASS 1 Foundation + Distribution Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement only MRC v0.1 PASS 1: repository foundation, version/CLI/GUI entry, installation/PATH architecture, packaging skeleton, and hard Main-PC/runner-root fences with no runner control.

**Architecture:** Use .NET 10 LTS with three small units: `MRC.Core` owns immutable product/boundary facts, `MRC.Cli` owns the canonical `MRC` command and launches the GUI, and `MRC.Gui` is a WPF shell that validates the boundary and exposes no runner controls in PASS 1. Distribution is user-scoped under `%LOCALAPPDATA%\MRC`, with a stable PATH shim resolving an immutable version payload.

**Tech Stack:** C# 14 / .NET 10 LTS, WPF, PowerShell 7-compatible scripts, GitHub Actions on `windows-latest`.

**Spec:** `Auth/0000_MasterAuth.md` through `Auth/0010_Passes.md`.

## Global Constraints

- Repository is exactly `doonchy16-cloud/MRC` and work is committed only to `main` per Owner instruction.
- Product target is `v0.1.0`; assembly/package version is `0.1.0`.
- Target machine is `Main-PC` only.
- Authorized runner root is exactly `D:\Git_Runners_Main`.
- One canonical PATH command is `MRC`.
- PASS 1 implements version/help and reserves doctor/update; doctor/update behavior completes in PASS 4.
- No runner discovery, state engine, start, stop, process control, or GitHub API behavior is implemented in PASS 1.
- Installation is user-scoped and must not require Administrator by default.
- Static/build evidence may verify PASS 1 implementation quality, but full product certification remains blocked until PASS 5 live Main-PC evidence.

---

### Task 1: Repository and Version Foundation

**Files:**
- Create: `.gitignore`
- Create: `README.md`
- Create: `global.json`
- Create: `Directory.Build.props`

**Interfaces:**
- Produces: repository-wide `Version=0.1.0`, .NET 10 SDK baseline, and documented authority entry point.

- [ ] Add repository hygiene and root documentation.
- [ ] Pin .NET 10 SDK family and set one repository-wide assembly/package version.
- [ ] Verify no PASS 2+ implementation is introduced.

### Task 2: Boundary Core

**Files:**
- Create: `src/MRC.Core/MRC.Core.csproj`
- Create: `src/MRC.Core/MrcConstants.cs`
- Create: `src/MRC.Core/BuildInfo.cs`
- Create: `src/MRC.Core/EnvironmentFence.cs`

**Interfaces:**
- Produces: `MrcConstants`, `BuildInfo.Version`, `EnvironmentFence.Evaluate(...)`, and `EnvironmentFence.EvaluateCurrent()`.

- [ ] Write tests that require wrong-machine, wrong-root, and missing-root cases to fail closed.
- [ ] Implement immutable machine/root constants and deterministic fence evaluation.
- [ ] Verify correct Main-PC + exact root evidence authorizes only the boundary, not runner control.

### Task 3: Canonical CLI and GUI Entry

**Files:**
- Create: `src/MRC.Cli/MRC.Cli.csproj`
- Create: `src/MRC.Cli/Program.cs`
- Create: `src/MRC.Cli/CliDispatcher.cs`
- Create: `src/MRC.Cli/GuiLauncher.cs`
- Create: `src/MRC.Gui/MRC.Gui.csproj`
- Create: `src/MRC.Gui/App.xaml`
- Create: `src/MRC.Gui/App.xaml.cs`
- Create: `src/MRC.Gui/MainWindow.xaml`
- Create: `src/MRC.Gui/MainWindow.xaml.cs`

**Interfaces:**
- Produces: canonical `MRC.exe`, internal `MRC.Gui.exe`, version/help aliases, detached GUI launch, duplicate-GUI mutex, and reserved PASS 4 doctor/update responses.

- [ ] Test version/help aliases and unknown/reserved flags without launching a GUI.
- [ ] Implement `MRC` with no arguments as detached GUI launch.
- [ ] Implement a PASS 1 WPF shell that displays boundary truth and no runner controls.

### Task 4: User-Scoped Installation and Packaging Skeleton

**Files:**
- Create: `scripts/install.ps1`
- Create: `scripts/package.ps1`
- Create: `scripts/verify-pass1.ps1`

**Interfaces:**
- Produces: `%LOCALAPPDATA%\MRC\bin\MRC.cmd`, immutable `%LOCALAPPDATA%\MRC\versions\0.1.0\` payloads, `current.version` indirection, candidate `MRC-v0.1.0-win-x64.zip`, and `SHA256SUMS.txt`.

- [ ] Implement a fail-closed initial installer that checks Main-PC and exact root existence before activation.
- [ ] Implement self-contained `win-x64` CLI/GUI publishing and candidate archive/checksum generation.
- [ ] Add a verification script that builds, runs tests, packages, and inspects required candidate contents.

### Task 5: Automated PASS 1 Evidence

**Files:**
- Create: `tests/MRC.Tests/MRC.Tests.csproj`
- Create: `tests/MRC.Tests/Program.cs`
- Create: `.github/workflows/pass1.yml`

**Interfaces:**
- Produces: zero-dependency executable test harness and Windows CI evidence for build/test/package.

- [ ] Add deterministic core/CLI tests with explicit failure exit code.
- [ ] Build CLI and WPF projects on `windows-latest` using .NET 10.
- [ ] Run the test harness and package script in CI.
- [ ] Upload the candidate package as a CI artifact only; do not publish a GitHub Release or tag in PASS 1.
