using MRC.Core.Runners;

namespace MRC.Core.Runtime;

internal sealed record RunnerProcessAssociation(
    ProcessSnapshot? Listener,
    IReadOnlyList<ProcessSnapshot> Workers,
    string? Error);

internal static class RunnerProcessAssociator
{
    private const int MaxAncestryDepth = 64;

    public static RunnerProcessAssociation Associate(RunnerDescriptor runner, ProcessInventory inventory)
    {
        if (!inventory.IsComplete)
        {
            return Error(inventory.Error ?? "Process inventory is incomplete.");
        }

        var expectedListener = RunnerPath.ListenerExecutable(runner.DirectoryPath);
        var expectedWorker = RunnerPath.WorkerExecutable(runner.DirectoryPath);

        var listeners = inventory.Processes
            .Where(process => process.ExecutablePath is not null && RunnerPath.EqualsWindows(process.ExecutablePath, expectedListener))
            .ToArray();
        if (listeners.Length > 1)
        {
            return Error("Multiple listener processes are associated with the same runner path.");
        }

        var workersAtRunnerPath = inventory.Processes
            .Where(process => process.ExecutablePath is not null && RunnerPath.EqualsWindows(process.ExecutablePath, expectedWorker))
            .ToArray();

        if (listeners.Length == 0)
        {
            if (workersAtRunnerPath.Length != 0)
            {
                return Error("A same-runner worker exists without an associated listener.");
            }

            var unresolved = inventory.Processes
                .Where(process => IsRunnerProcess(process.ProcessName) && string.IsNullOrWhiteSpace(process.ExecutablePath))
                .ToArray();
            if (unresolved.Length != 0)
            {
                var detail = string.Join("; ", unresolved.Select(process =>
                    $"PID {process.ProcessId} {process.ProcessName}: {process.InspectionError ?? "executable path unavailable"}"));
                return Error($"Runner appears OFF, but unresolved runner-process ownership could overlap this runner. {detail}");
            }

            return new RunnerProcessAssociation(null, Array.Empty<ProcessSnapshot>(), null);
        }

        var listener = listeners[0];
        var processById = inventory.Processes
            .GroupBy(process => process.ProcessId)
            .ToDictionary(group => group.Key, group => group.First());
        var associatedWorkers = new List<ProcessSnapshot>();

        foreach (var worker in workersAtRunnerPath)
        {
            if (worker.ParentProcessId is null)
            {
                return Error($"Worker PID {worker.ProcessId} has no provable parent process.");
            }

            if (!DescendsFrom(worker, listener.ProcessId, processById))
            {
                return Error($"Worker PID {worker.ProcessId} is at the runner path but does not descend from listener PID {listener.ProcessId}.");
            }

            associatedWorkers.Add(worker);
        }

        return new RunnerProcessAssociation(listener, associatedWorkers, null);
    }

    private static bool DescendsFrom(
        ProcessSnapshot process,
        int ancestorProcessId,
        IReadOnlyDictionary<int, ProcessSnapshot> processById)
    {
        var current = process.ParentProcessId;
        var visited = new HashSet<int>();

        for (var depth = 0; depth < MaxAncestryDepth && current is not null; depth++)
        {
            if (current.Value == ancestorProcessId)
            {
                return true;
            }

            if (!visited.Add(current.Value) || !processById.TryGetValue(current.Value, out var parent))
            {
                return false;
            }

            current = parent.ParentProcessId;
        }

        return false;
    }

    private static bool IsRunnerProcess(string processName) =>
        processName.Equals("Runner.Listener", StringComparison.OrdinalIgnoreCase)
        || processName.Equals("Runner.Listener.exe", StringComparison.OrdinalIgnoreCase)
        || processName.Equals("Runner.Worker", StringComparison.OrdinalIgnoreCase)
        || processName.Equals("Runner.Worker.exe", StringComparison.OrdinalIgnoreCase);

    private static RunnerProcessAssociation Error(string message) =>
        new(null, Array.Empty<ProcessSnapshot>(), message);
}
