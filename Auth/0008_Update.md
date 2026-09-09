# MRC V0.1 — Release & Update Authority

## Distribution Authority
GitHub Releases in the `MRC` repository are the distribution source of truth for user-installable versions.

## Versioning
Use semantic versions.

Initial release target:
`v0.1.0`

## Release Assets
V0.1 target assets:
- `MRC-v0.1.0-win-x64.zip`
- `SHA256SUMS.txt`

Additional release metadata may be added if it improves verification without bloating V0.1.

## Update Command
`MRC -update` and `MRC --update` are equivalent.

## Update Safety
An update must never partially overwrite the active working version.

Required conceptual flow:
1. determine installed version;
2. resolve latest allowed release;
3. download to staging;
4. verify hash against release checksum authority;
5. extract to a version-specific directory;
6. validate required payload files;
7. switch active version atomically;
8. verify launch/version response;
9. preserve previous version for rollback.

## Failure Behavior
If any step fails:
- do not activate the candidate;
- preserve current version;
- provide a clear error;
- leave a recoverable state.

## Update UX
The CLI should show concise progress such as:
- current version;
- latest version;
- download;
- verification;
- install/activation;
- success/failure.

## No Hidden Mutation
MRC update must not modify runner configuration, runner packages, or `D:\Git_Runners_Main` contents.
