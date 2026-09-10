namespace MRC.Core.Runtime;

internal sealed record RunnerServiceEvidence(
    string Name,
    string? PathName,
    int? ProcessId,
    string? DisplayName = null,
    string State = "UNKNOWN",
    string StartMode = "Unknown",
    string? StartName = null);

internal interface IRunnerServiceEvidenceProvider
{
    IReadOnlyList<RunnerServiceEvidence> Inspect();
}

internal sealed class EmptyRunnerServiceEvidenceProvider : IRunnerServiceEvidenceProvider
{
    public static EmptyRunnerServiceEvidenceProvider Instance { get; } = new();

    private EmptyRunnerServiceEvidenceProvider()
    {
    }

    public IReadOnlyList<RunnerServiceEvidence> Inspect() => Array.Empty<RunnerServiceEvidence>();
}
