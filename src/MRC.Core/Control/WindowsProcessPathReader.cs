using MRC.Core.Runtime;

namespace MRC.Core.Control;

internal sealed class WindowsProcessPathReader : IProcessPathReader
{
    public bool TryGetPath(int processId, out string? path, out string? error) =>
        WindowsNativeProcess.TryGetImagePath(processId, out path, out error);
}
