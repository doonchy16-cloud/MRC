# MRC V0.1 — Runner Discovery & Identity Authority

## Discovery Root
Discover only immediate child directories under:

`D:\Git_Runners_Main`

Do not recursively scan `_work`, nested repositories, copied packages, or arbitrary subtrees.

## Valid Runner Signature
A folder is a V0.1 configured-runner candidate only if all exist:
- `.runner`
- `run.cmd`
- `run-helper.cmd.template`
- `bin\Runner.Listener.exe`

The signature intentionally excludes files that are not required for identity/control, including:
- `.runner_migrated`
- `config.cmd`
- `RunnerService.exe.config`
- dependency manifests;
- DLLs.

## Metadata Authority
Parse `.runner` JSON.

Use:
- `agentName` → primary runner display name;
- `gitHubUrl` → repository identity;
- `agentId` → details/diagnostics;
- `workFolder` → details/diagnostics.

Derive repository display name from the final path segment of `gitHubUrl`.

## Display Priority
Primary identity:
1. runner name;
2. repository name.

Secondary details only:
- full runner folder;
- full GitHub URL;
- agent ID;
- work folder;
- process IDs;
- runner version.

## Launch Authority
Start a runner through its existing `run.cmd` with its runner directory as working directory.

Do not invoke `Runner.Listener.exe` directly because the runner wrapper handles retry/update/relaunch behavior.

## Process Ownership Fence
MRC may control a runner process only when it can associate that process with one discovered runner beneath the authorized root.

Global process-name matches alone are never sufficient authority.
