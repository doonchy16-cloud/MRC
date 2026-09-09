using System.Diagnostics;

namespace MRC.Core.Control;

internal sealed class WindowsProcessTreeKiller : IProcessTreeKiller
{
    public bool TryKillTree(int processId, out string? error)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            process.Kill(entireProcessTree: true);
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }
}
