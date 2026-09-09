namespace MRC.Core.Runtime;

internal sealed record ProcessSnapshot(
    int ProcessId,
    int? ParentProcessId,
    string ProcessName,
    string? ExecutablePath,
    string? InspectionError = null,
    int? SessionId = null);

internal sealed record ProcessInventory(
    IReadOnlyList<ProcessSnapshot> Processes,
    bool IsComplete,
    string? Error);
