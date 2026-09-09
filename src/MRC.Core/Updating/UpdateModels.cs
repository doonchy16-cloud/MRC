namespace MRC.Core.Updating;

public enum UpdateOutcome
{
    Updated,
    UpToDate,
    Failed
}

public enum UpdateProgressStage
{
    Resolve = 0,
    Compare = 1,
    Download = 2,
    Sha256Verify = 3,
    ManifestValidate = 4,
    Install = 5,
    Activate = 6,
    ActivationVerify = 7,
    RollbackRetention = 8,
    Complete = 9
}

public sealed record UpdateProgress(
    UpdateProgressStage Stage,
    string Message,
    int? Percent = null);

public sealed record UpdateAsset(string Name, Uri DownloadUrl);

public sealed record UpdateRelease(
    string TagName,
    Version Version,
    IReadOnlyList<UpdateAsset> Assets,
    bool IsPrerelease = false);

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
