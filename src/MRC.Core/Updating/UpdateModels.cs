namespace MRC.Core.Updating;

public enum UpdateOutcome
{
    Updated,
    UpToDate,
    Failed
}

public sealed record UpdateAsset(string Name, Uri DownloadUrl);

public sealed record UpdateRelease(
    string TagName,
    Version Version,
    IReadOnlyList<UpdateAsset> Assets);

public sealed record UpdateResult(
    UpdateOutcome Outcome,
    string Message,
    Version? PreviousVersion = null,
    Version? CurrentVersion = null);

public interface IUpdateReleaseSource
{
    Task<UpdateRelease> ResolveLatestAsync(CancellationToken cancellationToken = default);
    Task<byte[]> DownloadAssetAsync(UpdateAsset asset, CancellationToken cancellationToken = default);
}

public sealed record ActivationVerificationResult(bool Success, string Message);

public interface IUpdateActivationVerifier
{
    Task<ActivationVerificationResult> VerifyAsync(
        string versionRoot,
        Version expectedVersion,
        CancellationToken cancellationToken = default);
}
