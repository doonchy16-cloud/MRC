namespace MRC.Core.Runtime;

internal enum RunnerProcessOwnershipKind
{
    Managed = 0,
    External = 1,
    Unattributed = 2
}

internal sealed record RunnerObservedProcess(
    int ProcessId,
    int? ParentProcessId,
    int? SessionId,
    string ProcessName,
    string? ExecutablePath,
    RunnerProcessOwnershipKind Ownership,
    string? ManagedRunnerPath,
    string Reason,
    string? InspectionError);
