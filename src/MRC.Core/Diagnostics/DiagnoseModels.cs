using MRC.Core.Runners;
using MRC.Core.Runtime;

namespace MRC.Core.Diagnostics;

public sealed record WindowsServiceInfo(
    string Name,
    string? DisplayName,
    string State,
    string StartMode,
    string? StartName,
    string? PathName,
    int? ProcessId);

public sealed record DiagnoseRunnerFinding(
    string RunnerName,
    string RepositoryName,
    string DirectoryPath,
    RunnerState State,
    string? Error,
    string ExpectedListenerPath,
    int? ListenerProcessId,
    IReadOnlyList<int> WorkerProcessIds,
    int? ListenerParentProcessId = null,
    int? ListenerSessionId = null,
    string? ListenerProcessName = null,
    string? ListenerExecutablePath = null,
    string OwnershipProof = "Ownership proof unavailable.",
    string? InspectionError = null);

public sealed record DiagnoseProcessFinding(
    int ProcessId,
    int? ParentProcessId,
    int? SessionId,
    string ProcessName,
    string? ExecutablePath,
    RunnerSystemFindingKind Kind,
    string OwnershipProof,
    string? InspectionError,
    string? ServiceName);

public sealed record DiagnoseReport(
    string MachineName,
    string UserName,
    int? SessionId,
    bool IsElevated,
    IReadOnlyList<DiagnoseRunnerFinding> Runners,
    IReadOnlyList<DiagnoseProcessFinding> SystemProcesses,
    IReadOnlyList<RunnerFolderFinding> Folders,
    IReadOnlyList<WindowsServiceInfo> Services);
