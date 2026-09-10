namespace MRC.Core.Runtime;

public enum RunnerProcessOwnershipKind
{
    Managed,
    External,
    Unattributed
}

public sealed record RunnerObservedProcess(
    int ProcessId,
    int? ParentProcessId,
    int? SessionId,
    string ProcessName,
    string? ExecutablePath,
    RunnerProcessOwnershipKind Ownership,
    string? ManagedRunnerPath,
    string Reason,
    string? InspectionError,
    string? ServiceName = null);
