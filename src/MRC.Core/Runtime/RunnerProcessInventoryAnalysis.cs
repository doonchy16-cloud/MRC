using MRC.Core.Runners;

namespace MRC.Core.Runtime;

internal sealed record RunnerProcessInventoryAnalysis(IReadOnlyList<RunnerObservedProcess> ObservedProcesses);

internal static class RunnerProcessInventoryAnalyzer
{
    private const int MaxAncestryDepth = 64;

    public static RunnerProcessInventoryAnalysis Analyze(
        string authorizedRoot,
        IReadOnlyList<RunnerDescriptor> managedRunners,
        ProcessInventory inventory) =>
        Analyze(authorizedRoot, managedRunners, inventory, Array.Empty<RunnerServiceEvidence>());

    public static RunnerProcessInventoryAnalysis Analyze(
        string authorizedRoot,
        IReadOnlyList<RunnerDescriptor> managedRunners,
        ProcessInventory inventory,
        IReadOnlyList<RunnerServiceEvidence> services)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authorizedRoot);
        ArgumentNullException.ThrowIfNull(managedRunners);
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(services);

        var processById = inventory.Processes
            .GroupBy(process => process.ProcessId)
            .ToDictionary(group => group.Key, group => group.First());
        var servicesByPid = services
            .Where(service => service.ProcessId is > 0)
            .GroupBy(service => service.ProcessId!.Value)
            .ToDictionary(group => group.Key, group => group.First());

        var observed = new List<RunnerObservedProcess>();
        foreach (var process in inventory.Processes.Where(IsRunnerProcess))
        {
            var ownership = Classify(
                process,
                authorizedRoot,
                managedRunners,
                processById,
                servicesByPid,
                out var managedRunnerPath,
                out var serviceName,
                out var reason);
            observed.Add(new RunnerObservedProcess(
                process.ProcessId,
                process.ParentProcessId,
                process.SessionId,
                process.ProcessName,
                process.ExecutablePath,
                ownership,
                managedRunnerPath,
                reason,
                process.InspectionError,
                serviceName));
        }

        return new RunnerProcessInventoryAnalysis(observed);
    }

    private static RunnerProcessOwnershipKind Classify(
        ProcessSnapshot process,
        string authorizedRoot,
        IReadOnlyList<RunnerDescriptor> managedRunners,
        IReadOnlyDictionary<int, ProcessSnapshot> processById,
        IReadOnlyDictionary<int, RunnerServiceEvidence> servicesByPid,
        out string? managedRunnerPath,
        out string? serviceName,
        out string reason)
    {
        managedRunnerPath = null;
        serviceName = null;

        if (string.IsNullOrWhiteSpace(process.ExecutablePath))
        {
            var relatedService = FindRelatedRunnerService(process, processById, servicesByPid);
            if (relatedService is not null
                && relatedService.Name.StartsWith("actions.runner.", StringComparison.OrdinalIgnoreCase)
                && TryExecutablePath(relatedService.PathName, out var serviceExecutable)
                && serviceExecutable is not null
                && !IsUnderRoot(serviceExecutable, authorizedRoot))
            {
                serviceName = relatedService.Name;
                reason = $"Runner process is owned by Windows service '{relatedService.Name}' whose executable is outside the authorized managed-runner root: {serviceExecutable}.";
                return RunnerProcessOwnershipKind.External;
            }

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

    private static RunnerServiceEvidence? FindRelatedRunnerService(
        ProcessSnapshot process,
        IReadOnlyDictionary<int, ProcessSnapshot> processById,
        IReadOnlyDictionary<int, RunnerServiceEvidence> servicesByPid)
    {
        int? current = process.ProcessId;
        var visited = new HashSet<int>();

        for (var depth = 0; depth < MaxAncestryDepth && current is not null; depth++)
        {
            if (!visited.Add(current.Value)) return null;
            if (servicesByPid.TryGetValue(current.Value, out var service)) return service;
            if (!processById.TryGetValue(current.Value, out var snapshot)) return null;
            current = snapshot.ParentProcessId;
        }

        return null;
    }

    private static bool TryExecutablePath(string? commandLine, out string? executablePath)
    {
        executablePath = null;
        if (string.IsNullOrWhiteSpace(commandLine)) return false;

        var value = Environment.ExpandEnvironmentVariables(commandLine.Trim());
        if (value.StartsWith('"'))
        {
            var end = value.IndexOf('"', 1);
            if (end <= 1) return false;
            executablePath = value[1..end];
            return true;
        }

        var exe = value.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
        if (exe < 0) return false;
        executablePath = value[..(exe + 4)].Trim();
        return true;
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
