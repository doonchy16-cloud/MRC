using MRC.Core.Runtime;

namespace MRC.Core.Diagnostics;

public sealed class WindowsServiceInspector
{
    private readonly IRunnerServiceEvidenceProvider _provider;

    public WindowsServiceInspector()
        : this(new WindowsRunnerServiceEvidenceProvider())
    {
    }

    internal WindowsServiceInspector(IRunnerServiceEvidenceProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    public IReadOnlyList<WindowsServiceInfo> Inspect() =>
        _provider.Inspect()
            .Select(service => new WindowsServiceInfo(
                service.Name,
                service.DisplayName,
                service.State,
                service.StartMode,
                service.StartName,
                service.PathName,
                service.ProcessId))
            .ToArray();
}
