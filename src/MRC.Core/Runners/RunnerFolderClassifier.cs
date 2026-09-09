namespace MRC.Core.Runners;

public enum RunnerFolderKind
{
    ManagedRunner = 0,
    BrokenRunnerCandidate = 1,
    NonRunnerFolder = 2
}

public sealed record RunnerFolderFinding(
    string Path,
    RunnerFolderKind Kind,
    IReadOnlyList<string> Evidence);

public static class RunnerFolderClassifier
{
    private static readonly string[] RequiredRelativePaths =
    {
        ".runner",
        "run.cmd",
        "run-helper.cmd.template",
        Path.Combine("bin", "Runner.Listener.exe")
    };

    public static RunnerFolderFinding Classify(string directoryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
        var normalized = RunnerPath.Normalize(directoryPath);

        if (RunnerPath.HasSignature(normalized))
        {
            return new RunnerFolderFinding(
                normalized,
                RunnerFolderKind.ManagedRunner,
                RequiredRelativePaths.Select(path => $"present: {path}").ToArray());
        }

        var present = RunnerArtifactEvidence(normalized).ToArray();
        if (present.Length == 0)
        {
            return new RunnerFolderFinding(
                normalized,
                RunnerFolderKind.NonRunnerFolder,
                new[] { "no meaningful GitHub runner artifacts detected" });
        }

        var evidence = new List<string>();
        evidence.AddRange(present.Select(path => $"present: {path}"));
        evidence.AddRange(RequiredRelativePaths
            .Where(relative => !File.Exists(Path.Combine(normalized, relative)))
            .Select(relative => $"missing: {relative}"));
        return new RunnerFolderFinding(normalized, RunnerFolderKind.BrokenRunnerCandidate, evidence);
    }

    public static IReadOnlyList<RunnerFolderFinding> ClassifyImmediateChildren(string root)
    {
        if (!Directory.Exists(root)) return Array.Empty<RunnerFolderFinding>();
        return Directory.EnumerateDirectories(root, "*", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(Classify)
            .ToArray();
    }

    private static IEnumerable<string> RunnerArtifactEvidence(string directoryPath)
    {
        foreach (var relative in RequiredRelativePaths)
        {
            if (File.Exists(Path.Combine(directoryPath, relative))) yield return relative;
        }

        var serviceCandidates = new[]
        {
            Path.Combine("bin", "RunnerService.exe"),
            Path.Combine("actions-runner", "bin", "Runner.Listener.exe"),
            Path.Combine("actions-runner", "bin", "RunnerService.exe")
        };
        foreach (var relative in serviceCandidates)
        {
            if (File.Exists(Path.Combine(directoryPath, relative))) yield return relative;
        }
    }
}
