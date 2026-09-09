# MRC V0.1 — Scope Authority

## Product Identity
**Name:** Main Runner Control  
**CLI:** `MRC`  
**Release target:** `v0.1.0`  
**Git authority:** repository named exactly `MRC`

## In Scope
V0.1 includes:
- Main-PC-only operation;
- discovery of configured GitHub Actions runners beneath `D:\Git_Runners_Main`;
- per-runner identity display;
- per-runner runtime state display;
- per-runner ON/OFF controls;
- safe bulk TURN ALL ON / TURN ALL OFF;
- search and state filters;
- state counters;
- state animations;
- manual + automatic refresh;
- PATH-based `MRC` command;
- version/help/doctor/update CLI flags;
- GitHub Releases packaging/update foundation;
- release verification and rollback-aware update design.

## Explicitly Out of Scope for V0.1
Do not add unless Owner later authorizes:
- GitHub API authentication;
- GitHub organization/repository administration;
- runner registration/unregistration;
- runner configuration editing;
- runner credential editing;
- Windows service installation/management;
- cross-machine runner control;
- cloud fleet management;
- tray mode;
- charts/graphs;
- CPU/RAM telemetry dashboards;
- giant runner cards;
- database persistence;
- complex settings system;
- elaborate logging infrastructure;
- automatic scheduling;
- job cancellation through GitHub APIs.

## Product Philosophy
MRC should feel like a dense, fast operations console—not an enterprise dashboard. Prioritize:
1. truthfulness;
2. safety;
3. speed of scanning;
4. compact controls;
5. visual clarity;
6. minimal dependencies.

## Machine Boundary
The implementation must use a practical Main-PC identity fence plus the exact runner-root fence. If the machine identity check and root evidence disagree, fail closed and present an error instead of controlling processes.

## Runner Root Boundary
The only authorized runner root is:

`D:\Git_Runners_Main`

V0.1 must not auto-expand to other directories.
