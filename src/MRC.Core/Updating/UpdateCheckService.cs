namespace MRC.Core.Updating;

public sealed class UpdateCheckService
{
    private readonly IUpdateReleaseSource _source;
    private readonly Func<Version> _currentVersion;

    public UpdateCheckService(IUpdateReleaseSource source)
        : this(source, CurrentBuildVersion)
    {
    }

    public UpdateCheckService(IUpdateReleaseSource source, Func<Version> currentVersion)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _currentVersion = currentVersion ?? throw new ArgumentNullException(nameof(currentVersion));
    }

    public async Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        var current = _currentVersion();
        var latest = await _source.ResolveLatestAsync(cancellationToken);

        if (!ReleaseAuthority.IsAllowedVersion(latest.Version))
        {
            throw new InvalidDataException($"Resolved release {latest.Version} is outside MRC release authority.");
        }

        var outcome = latest.Version > current
            ? UpdateCheckOutcome.UpdateAvailable
            : UpdateCheckOutcome.UpToDate;

        return new UpdateCheckResult(outcome, current, latest.Version);
    }

    private static Version CurrentBuildVersion() =>
        Version.TryParse(BuildInfo.Version, out var parsed)
            ? parsed
            : throw new InvalidOperationException($"MRC build version '{BuildInfo.Version}' is invalid.");
}
