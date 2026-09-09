using MRC.Core;

namespace MRC.Cli;

public sealed record CliLine(string Text, CliTone Tone);

public static class CliPresentation
{
    public static IReadOnlyList<CliLine> VersionLines(string installLocation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(installLocation);
        var release = ReleaseAuthority.Current;
        var stage = release.Stage == ReleaseStage.PreCertification ? "PRE-CERTIFICATION" : "FINAL";

        return new[]
        {
            new CliLine(MrcConstants.ProductName, CliTone.Heading),
            new CliLine($"Version: {release.Version}", CliTone.Metadata),
            new CliLine($"Channel: {release.Channel}", CliTone.Metadata),
            new CliLine($"Stage: {stage}", CliTone.Metadata),
            new CliLine($"Final target: {release.FinalTarget}", CliTone.Metadata),
            new CliLine($"Install location: {installLocation}", CliTone.Path),
            new CliLine($"Runner root: {MrcConstants.RunnerRoot}", CliTone.Path)
        };
    }
}
