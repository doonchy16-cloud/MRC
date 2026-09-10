namespace MRC.Core;

public enum ReleaseStage
{
    PreCertification = 0,
    Final = 1
}

public sealed record ReleaseInfo(
    string Version,
    string Channel,
    ReleaseStage Stage,
    string FinalTarget);

public static class ReleaseAuthority
{
    public static readonly Version FinalTargetVersion = new(0, 1, 0);
    private static readonly Version LegacyBootstrapVersion = new(0, 0, 12);

    public static ReleaseInfo Current
    {
        get
        {
            var version = ParseRequired(BuildInfo.Version);
            return new ReleaseInfo(
                version.ToString(),
                ChannelFor(version),
                StageFor(version),
                FinalTargetVersion.ToString());
        }
    }

    public static bool IsAllowedVersion(Version version)
    {
        ArgumentNullException.ThrowIfNull(version);
        return version.Major == 0
               && ((version.Minor == 0 && version.Build >= 0)
                   || version == FinalTargetVersion);
    }

    public static ReleaseStage StageFor(Version version) =>
        version == FinalTargetVersion ? ReleaseStage.Final : ReleaseStage.PreCertification;

    public static string ChannelFor(Version version) =>
        StageFor(version) == ReleaseStage.Final ? "stable" : "precert";

    // v0.0.11 validates candidate manifest.channel against its own legacy "stable"
    // authority before the new executable can ever run. v0.0.12 is therefore a
    // one-version transport bridge: the package manifest remains legacy-readable
    // while the installed v0.0.12 application truthfully reports channel=precert.
    public static string ManifestCompatibilityChannelFor(Version version)
    {
        ArgumentNullException.ThrowIfNull(version);
        return version == LegacyBootstrapVersion ? "stable" : ChannelFor(version);
    }

    private static Version ParseRequired(string value) =>
        Version.TryParse(value, out var parsed)
            ? parsed
            : throw new InvalidOperationException($"MRC build version '{value}' is invalid.");
}
