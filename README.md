# Main Runner Control (MRC)

MRC is a small Windows control center for the GitHub Actions self-hosted runners that belong to **Main-PC** beneath the single authorized root:

`D:\Git_Runners_Main`

The governing project authority lives in [`Auth/`](Auth/). Any implementation change must preserve those files unless the Owner explicitly changes the authority.

## Canonical command

```powershell
MRC
MRC -v
MRC -version
MRC --version
MRC -h
MRC -help
MRC --help
MRC -doctor
MRC --doctor
MRC -update
MRC --update
```

`doctor` and `update` are part of the v0.1 command surface but their operational implementations are intentionally reserved for PASS 4.

## PASS 1 architecture

- `src/MRC.Core` — immutable product facts, version access, and fail-closed Main-PC/root fence.
- `src/MRC.Cli` — the canonical `MRC.exe` dispatcher and GUI launch entry.
- `src/MRC.Gui` — internal WPF GUI shell; PASS 1 contains no runner controls.
- `scripts/install.ps1` — user-scoped `%LOCALAPPDATA%\MRC` installation and PATH shim.
- `scripts/package.ps1` — self-contained `win-x64` candidate package/checksum skeleton.
- `tests/MRC.Tests` — zero-dependency executable PASS 1 acceptance harness.

## Development verification

On Windows with .NET 10 installed:

```powershell
pwsh ./scripts/verify-pass1.ps1
```

Full MRC certification is governed by `Auth/0009_Test.md` and does not occur until PASS 5 with direct Main-PC runtime evidence.
