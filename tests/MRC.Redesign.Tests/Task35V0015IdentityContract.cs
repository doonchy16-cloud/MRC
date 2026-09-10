using System.Runtime.CompilerServices;
using MRC.Core;

internal static class Task35V0015IdentityContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        if (BuildInfo.Version != "0.0.15")
            throw new InvalidOperationException($"BuildInfo.Version is '{BuildInfo.Version}', expected 0.0.15.");

        var release = ReleaseAuthority.Current;
        if (release.Version != "0.0.15" || release.Channel != "precert" ||
            release.Stage != ReleaseStage.PreCertification || release.FinalTarget != "0.1.0")
            throw new InvalidOperationException($"v0.0.15 release authority mismatch: {release}.");
    }
}
