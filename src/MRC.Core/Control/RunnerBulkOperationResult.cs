namespace MRC.Core.Control;

public sealed record RunnerBulkOperationResult(
    int Attempted,
    int Succeeded,
    int Skipped,
    int BusySkipped,
    int Errors,
    IReadOnlyList<string> Messages);
