using MRC.Core.Runners;

namespace MRC.Core.Diagnostics;

public enum DoctorCheckStatus
{
    Pass,
    Warning,
    Fail
}

public sealed record DoctorCheck(string Name, DoctorCheckStatus Status, string Message);

public sealed record DoctorReport(IReadOnlyList<DoctorCheck> Checks)
{
    public int ExitCode => Checks.Any(check => check.Status == DoctorCheckStatus.Fail) ? 1 : 0;
}

public sealed class DoctorService
{
    private static readonly Uri ReleasesEndpoint = new("https://api.github.com/repos/doonchy16-cloud/MRC/releases?per_page=1");

    public async Task<DoctorReport> RunAsync(CancellationToken cancellationToken = default)
    {
        var checks = new List<DoctorCheck>(7);

        var machineName = Environment.MachineName;
        var machineMatches = string.Equals(machineName, MrcConstants.TargetMachineName, StringComparison.OrdinalIgnoreCase);
        checks.Add(new DoctorCheck(
            "Machine identity",
            machineMatches ? DoctorCheckStatus.Pass : DoctorCheckStatus.Fail,
            machineMatches
                ? $"{machineName} matches the authorized target."
                : $"{machineName} does not match required machine {MrcConstants.TargetMachineName}."));

        var rootExists = Directory.Exists(MrcConstants.RunnerRoot);
        checks.Add(new DoctorCheck(
            "Runner root",
            rootExists ? DoctorCheckStatus.Pass : DoctorCheckStatus.Fail,
            rootExists
                ? $"{MrcConstants.RunnerRoot} exists."
                : $"{MrcConstants.RunnerRoot} is missing."));

        var expectedBin = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MRC",
            "bin");
        var userPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? string.Empty;
        var pathInstalled = ContainsPath(userPath, expectedBin);
        checks.Add(new DoctorCheck(
            "PATH installation",
            pathInstalled ? DoctorCheckStatus.Pass : DoctorCheckStatus.Fail,
            pathInstalled
                ? $"User PATH contains {expectedBin}."
                : $"User PATH does not contain {expectedBin}."));

        var versionValid = Version.TryParse(BuildInfo.Version, out var parsedVersion)
            && parsedVersion.Major == 0
            && parsedVersion.Minor == 1
            && parsedVersion.Build == 0;
        checks.Add(new DoctorCheck(
            "Version authority",
            versionValid ? DoctorCheckStatus.Pass : DoctorCheckStatus.Fail,
            versionValid
                ? $"Installed authority is {BuildInfo.Version} ({MrcConstants.ReleaseChannel})."
                : $"BuildInfo.Version '{BuildInfo.Version}' is not the authorized v0.1 version."));

        if (rootExists)
        {
            try
            {
                var directories = Directory.EnumerateDirectories(MrcConstants.RunnerRoot, "*", SearchOption.TopDirectoryOnly).ToArray();
                var complete = directories.Count(RunnerPath.HasSignature);
                var incomplete = directories.Length - complete;
                checks.Add(new DoctorCheck(
                    "Discovered runners",
                    DoctorCheckStatus.Pass,
                    $"{complete} signature-complete runner(s) discovered from {directories.Length} immediate child folder(s)."));
                checks.Add(new DoctorCheck(
                    "Runner signatures",
                    incomplete == 0 ? DoctorCheckStatus.Pass : DoctorCheckStatus.Fail,
                    incomplete == 0
                        ? "All immediate runner folders contain the required runner signature."
                        : $"{incomplete} immediate runner folder(s) have incomplete signatures."));
            }
            catch (Exception ex)
            {
                checks.Add(new DoctorCheck("Discovered runners", DoctorCheckStatus.Fail, $"Runner discovery failed: {ex.Message}"));
                checks.Add(new DoctorCheck("Runner signatures", DoctorCheckStatus.Fail, "Signature completeness could not be verified."));
            }
        }
        else
        {
            checks.Add(new DoctorCheck("Discovered runners", DoctorCheckStatus.Warning, "Runner count unavailable because the runner root is missing."));
            checks.Add(new DoctorCheck("Runner signatures", DoctorCheckStatus.Warning, "Signature completeness unavailable because the runner root is missing."));
        }

        checks.Add(await CheckReleaseReachabilityAsync(cancellationToken));
        return new DoctorReport(checks);
    }

    private static async Task<DoctorCheck> CheckReleaseReachabilityAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(4) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("MRC/0.1.0");
            using var response = await client.GetAsync(ReleasesEndpoint, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return new DoctorCheck(
                "Release reachability",
                response.IsSuccessStatusCode ? DoctorCheckStatus.Pass : DoctorCheckStatus.Warning,
                response.IsSuccessStatusCode
                    ? "GitHub Releases endpoint is reachable."
                    : $"GitHub Releases endpoint responded {(int)response.StatusCode} {response.ReasonPhrase}." );
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            return new DoctorCheck("Release reachability", DoctorCheckStatus.Warning, $"GitHub Releases endpoint could not be reached: {ex.Message}");
        }
    }

    private static bool ContainsPath(string pathValue, string expected)
    {
        if (string.IsNullOrWhiteSpace(pathValue)) return false;
        var normalizedExpected = NormalizePath(expected);
        return pathValue
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizePath)
            .Any(candidate => string.Equals(candidate, normalizedExpected, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizePath(string value) =>
        value.Trim().Trim('"').TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}
