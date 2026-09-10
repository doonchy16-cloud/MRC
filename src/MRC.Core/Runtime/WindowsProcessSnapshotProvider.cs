using System.Diagnostics;

namespace MRC.Core.Runtime;

internal sealed class WindowsProcessSnapshotProvider : IProcessSnapshotProvider
{
    public ProcessInventory Capture()
    {
        Process[] processes;
        try
        {
            processes = Process.GetProcesses();
        }
        catch (Exception ex)
        {
            return new ProcessInventory(Array.Empty<ProcessSnapshot>(), false, $"Process enumeration failed: {ex.Message}");
        }

        var fallbackParents = WindowsNativeProcess.CaptureParentProcessIds();
        var snapshots = new List<ProcessSnapshot>(processes.Length);
        foreach (var process in processes)
        {
            using (process)
            {
                int processId;
                string processName;
                try
                {
                    processId = process.Id;
                    processName = process.ProcessName;
                }
                catch
                {
                    continue;
                }

                int? sessionId = null;
                try
                {
                    sessionId = process.SessionId;
                }
                catch
                {
                    // Session data is diagnostic-only; inability to read it must not invalidate process inventory.
                }

                WindowsNativeProcess.TryGetImagePath(processId, out var executablePath, out var pathError);
                var handleParentSucceeded = WindowsNativeProcess.TryGetParentProcessId(
                    processId,
                    out var handleParentProcessId,
                    out var handleParentError);
                var parentEvidence = ParentProcessEvidence.Resolve(
                    processId,
                    handleParentSucceeded,
                    handleParentProcessId,
                    handleParentError,
                    fallbackParents);
                var error = Combine(pathError, parentEvidence.Error);
                snapshots.Add(new ProcessSnapshot(
                    processId,
                    parentEvidence.ParentProcessId,
                    processName,
                    executablePath,
                    error,
                    sessionId));
            }
        }

        return new ProcessInventory(snapshots, true, null);
    }

    private static string? Combine(string? first, string? second)
    {
        var normalizedFirst = Normalize(first);
        var normalizedSecond = Normalize(second);

        if (normalizedFirst is null) return normalizedSecond;
        if (normalizedSecond is null) return normalizedFirst;
        if (string.Equals(normalizedFirst, normalizedSecond, StringComparison.OrdinalIgnoreCase))
        {
            return normalizedFirst;
        }

        return normalizedFirst + " | " + normalizedSecond;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
