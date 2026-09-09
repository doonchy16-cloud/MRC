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

                WindowsNativeProcess.TryGetImagePath(processId, out var executablePath, out var pathError);
                WindowsNativeProcess.TryGetParentProcessId(processId, out var parentProcessId, out var parentError);
                var error = Combine(pathError, parentError);
                snapshots.Add(new ProcessSnapshot(processId, parentProcessId, processName, executablePath, error));
            }
        }

        return new ProcessInventory(snapshots, true, null);
    }

    private static string? Combine(string? first, string? second)
    {
        if (string.IsNullOrWhiteSpace(first)) return second;
        if (string.IsNullOrWhiteSpace(second)) return first;
        return first + " | " + second;
    }
}
