using MRC.Core.Runners;

namespace MRC.Core.Runtime;

internal sealed record RunnerProcessInventoryAnalysis(IReadOnlyList<RunnerObservedProcess> ObservedProcesses);

internal static class RunnerProcessInventoryAnalyzer
{
    public static RunnerProcessInventoryAnalysis Analyze(
        string authorizedRoot,
        IReadOnlyList<RunnerDescriptor> managedRunners,
        ProcessInventory inventory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authorizedRoot);
        ArgumentNullException.ThrowIfNull(managedRunners);
        ArgumentNullException.ThrowIfNull(inventory);

        var observed = new List<RunnerObservedProcess>();
        foreach (var process in inventory.Processes.Where(IsRunnerProcess))
        {
            var ownership = Classify(process, authorizedRoot, managedRunners, out var managedRunnerPath, out var reason);
            observed.Add(new RunnerObservedProcess(
                process.ProcessId,
                process.ParentProcessId,
                process.SessionId,
                process.ProcessName,
                process.ExecutablePath,
                ownership,
                managedRunnerPath,
                reason,
                process.InspectionError));
        }

        return new RunnerProcessInventoryAnalysis(observed);
    }

    private static RunnerProcessOwnershipKind Classify(
        ProcessSnapshot process,
        string authorizedRoot,
        IReadOnlyList<RunnerDescriptor> managedRunners,
        out string? managedRunnerPath,
        out string reason)
    {
        managedRunnerPath = null;

        if (string.IsNullOrWhiteSpace(process.ExecutablePath))
        {
            reason = string.IsNullOrWhiteSpace(process.InspectionError)
                ? "Runner process executable path is unavailable; exact ownership cannot be proven."
                : $"Runner process executable path is unavailable: {process.InspectionError}";
            return RunnerProcessOwnershipKind.Unattributed;
        }

        foreach (var runner in managedRunners)
        {
            if (RunnerPath.EqualsWindows(process.ExecutablePath, RunnerPath.ListenerExecutable(runner.DirectoryPath))
                || RunnerPath.EqualsWindows(process.ExecutablePath, RunnerPath.WorkerExecutable(runner.DirectoryPath)))
            {
                managedRunnerPath = RunnerPath.Normalize(runner.DirectoryPath);
                reason = "Executable path exactly matches a discovered managed runner artifact.";
                return RunnerProcessOwnershipKind.Managed;
            }
        }

        if (IsUnderRoot(process.ExecutablePath, authorizedRoot))
        {
            reason = "Runner process is beneath the authorized root but does not match a discovered configured runner artifact.";
            return RunnerProcessOwnershipKind.Unattributed;
        }

        reason = "Runner process executable path is outside the authorized managed-runner root.";
        return RunnerProcessOwnershipKind.External;
    }

    private static bool IsUnderRoot(string candidate, string root)
    {
        try
        {
            var normalizedRoot = RunnerPath.Normalize(root);
            var normalizedCandidate = RunnerPath.Normalize(candidate);
            var prefix = normalizedRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            return normalizedCandidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsRunnerProcess(ProcessSnapshot process) =>
        process.ProcessName.Equals("Runner.Listener", StringComparison.OrdinalIgnoreCase)
        || process.ProcessName.Equals("Runner.Listener.exe", StringComparison.OrdinalIgnoreCase)
        || process.ProcessName.Equals("Runner.Worker", StringComparison.OrdinalIgnoreCase)
        || process.ProcessName.Equals("Runner.Worker.exe", StringComparison.OrdinalIgnoreCase);
}
