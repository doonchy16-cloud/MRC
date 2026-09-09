namespace MRC.Core.Runtime;

public enum RunnerSystemFindingKind
{
    External = 0,
    Unattributed = 1
}

public sealed record RunnerSystemFinding(
    RunnerSystemFindingKind Kind,
    int ProcessId,
    int? ParentProcessId,
    int? SessionId,
    string ProcessName,
    string? ExecutablePath,
    string Message);

public sealed record RunnerRuntimeReport(
    IReadOnlyList<RunnerSnapshot> ManagedRunners,
    IReadOnlyList<RunnerSystemFinding> SystemFindings);
