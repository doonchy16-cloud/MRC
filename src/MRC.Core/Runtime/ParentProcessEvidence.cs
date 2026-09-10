namespace MRC.Core.Runtime;

internal sealed record ParentProcessEvidenceResult(int? ParentProcessId, string? Error);

internal static class ParentProcessEvidence
{
    public static ParentProcessEvidenceResult Resolve(
        int processId,
        bool handleReadSucceeded,
        int? handleParentProcessId,
        string? handleError,
        IReadOnlyDictionary<int, int?> fallbackParents)
    {
        ArgumentNullException.ThrowIfNull(fallbackParents);

        if (handleReadSucceeded)
        {
            return new ParentProcessEvidenceResult(handleParentProcessId, null);
        }

        if (fallbackParents.TryGetValue(processId, out var fallbackParentProcessId))
        {
            return new ParentProcessEvidenceResult(fallbackParentProcessId, null);
        }

        return new ParentProcessEvidenceResult(null, Normalize(handleError));
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
