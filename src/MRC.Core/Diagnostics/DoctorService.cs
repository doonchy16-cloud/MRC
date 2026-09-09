using MRC.Core.Runners;
using MRC.Core.Runtime;

namespace MRC.Core.Diagnostics;

public sealed class DoctorService
{
    private static readonly Uri ReleasesEndpoint = new("https://api.github.com/repos/doonchy16-cloud/MRC/releases?per_page=10");
    private readonly InstallHealthInspector _install;
    private readonly DoctorRepairService _repairs;

    public DoctorService()
        : this(new InstallHealthInspector())
    {
    }

    public DoctorService(InstallHealthInspector install)
    {
        _install = install;
        _repairs = new DoctorRepairService(install);
    }

    public Task<DoctorReport> RunAsync(CancellationToken cancellationToken = default) =>
        RunAsync(new DoctorRunOptions(), cancellationToken);

    public async Task<DoctorReport> RunAsync(
        DoctorRunOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        var findings = await FindAsync(cancellationToken);
        var repairActions = new List<DoctorRepairAction>();
        var verificationResults = new List<DoctorVerificationResult>();

        foreach (var finding in findings.Where(finding => finding.Repairable))
        {
            var action = _repairs.Execute(finding, options);
            repairActions.Add(action);
            if (action.Attempted)
            {
                verificationResults.Add(_repairs.Verify(finding.Id));
            }
        }

        var refreshedFindings = repairActions.Any(action => action.Attempted)
            ? await FindAsync(cancellationToken)
            : findings;

        var health = DetermineHealth(refreshedFindings, repairActions);
        return new DoctorReport(refreshedFindings, repairActions, verificationResults, health);
    }

    private async Task<IReadOnlyList<DoctorFinding>> FindAsync(CancellationToken cancellationToken)
    {
        var findings = new List<DoctorFinding>();

        var machineName = Environment.MachineName;
        var machineMatches = string.Equals(machineName, MrcConstants.TargetMachineName, StringComparison.OrdinalIgnoreCase);
        findings.Add(new DoctorFinding(
            "authority.machine",
            "Machine identity",
            machineMatches ? DoctorCheckStatus.Pass : DoctorCheckStatus.Fail,
            machineMatches
                ? $"{machineName} matches the authorized target."
                : $"{machineName} does not match required machine {MrcConstants.TargetMachineName}."));

        var rootExists = Directory.Exists(MrcConstants.RunnerRoot);
        findings.Add(new DoctorFinding(
            "authority.root",
            "Runner root",
            rootExists ? DoctorCheckStatus.Pass : DoctorCheckStatus.Fail,
            rootExists
                ? $"{MrcConstants.RunnerRoot} exists."
                : $"{MrcConstants.RunnerRoot} is missing."));

        var install = _install.Inspect();
        findings.Add(new DoctorFinding(
            "install.path",
            "PATH installation",
            install.PathHealthy ? DoctorCheckStatus.Pass : DoctorCheckStatus.Fail,
            install.PathHealthy
                ? $"User PATH contains exactly one current MRC bin entry: {install.BinRoot}."
                : $"User PATH is missing, duplicated, or contains stale MRC bin entries. Current-entry count: {install.ExpectedPathEntryCount}; stale entries: {install.StaleMrcPathEntries.Count}.",
            !install.PathHealthy,
            !install.PathHealthy ? DoctorRepairRisk.Automatic : null));

        findings.Add(new DoctorFinding(
            "install.pointer",
            "Active installation pointer",
            install.PointerHealthy ? DoctorCheckStatus.Pass : DoctorCheckStatus.Fail,
            install.PointerHealthy
                ? $"current.version resolves to valid payload {install.ActiveVersion}."
                : "current.version is missing, empty, or does not resolve to a valid MRC payload.",
            !install.PointerHealthy && install.ValidVersionRoots.Count > 0,
            !install.PointerHealthy && install.ValidVersionRoots.Count > 0 ? DoctorRepairRisk.Automatic : null));

        findings.Add(new DoctorFinding(
            "install.launcher",
            "MRC launcher",
            install.LauncherExists ? DoctorCheckStatus.Pass : DoctorCheckStatus.Fail,
            install.LauncherExists
                ? $"Launcher exists: {install.LauncherPath}."
                : $"Launcher is missing: {install.LauncherPath}.",
            !install.LauncherExists,
            !install.LauncherExists ? DoctorRepairRisk.Automatic : null));

        findings.Add(new DoctorFinding(
            "install.rollback",
            "Rollback availability",
            install.RollbackAvailable ? DoctorCheckStatus.Pass : DoctorCheckStatus.Warning,
            install.RollbackAvailable
                ? "At least one non-active valid MRC version payload is retained for rollback."
                : "No non-active valid MRC version payload is currently available for rollback."));

        var versionValid = IsAuthorizedPreCertOrFinalVersion(BuildInfo.Version);
        findings.Add(new DoctorFinding(
            "release.version-authority",
            "Version authority",
            versionValid ? DoctorCheckStatus.Pass : DoctorCheckStatus.Fail,
            versionValid
                ? $"Installed version {BuildInfo.Version} is valid for the authorized pre-cert/final version family."
                : $"BuildInfo.Version '{BuildInfo.Version}' is outside the authorized MRC pre-cert/final version family."));

        if (rootExists)
        {
            try
            {
                var folders = RunnerFolderClassifier.ClassifyImmediateChildren(MrcConstants.RunnerRoot);
                var managed = folders.Count(folder => folder.Kind == RunnerFolderKind.ManagedRunner);
                var broken = folders.Where(folder => folder.Kind == RunnerFolderKind.BrokenRunnerCandidate).ToArray();
                var nonRunner = folders.Count(folder => folder.Kind == RunnerFolderKind.NonRunnerFolder);

                findings.Add(new DoctorFinding(
                    "runner.discovery",
                    "Discovered runners",
                    DoctorCheckStatus.Pass,
                    $"{managed} managed runner(s), {broken.Length} broken candidate(s), and {nonRunner} non-runner folder(s) classified from the authorized root."));

                findings.Add(new DoctorFinding(
                    "runner.broken-candidates",
                    "Runner folder integrity",
                    broken.Length == 0 ? DoctorCheckStatus.Pass : DoctorCheckStatus.Warning,
                    broken.Length == 0
                        ? "No partial or contradictory runner installations were found."
                        : $"{broken.Length} broken runner candidate(s) require review: {string.Join(", ", broken.Select(folder => Path.GetFileName(folder.Path)))}."));
            }
            catch (Exception ex)
            {
                findings.Add(new DoctorFinding("runner.discovery", "Discovered runners", DoctorCheckStatus.Fail, $"Runner folder classification failed: {ex.Message}"));
            }
        }

        if (machineMatches && rootExists)
        {
            try
            {
                var diagnose = new DiagnoseService().Run();
                var runnerErrors = diagnose.Runners.Where(runner => runner.State == RunnerState.ERROR).ToArray();
                findings.Add(new DoctorFinding(
                    "runtime.managed",
                    "Managed runtime health",
                    runnerErrors.Length == 0 ? DoctorCheckStatus.Pass : DoctorCheckStatus.Fail,
                    runnerErrors.Length == 0
                        ? $"{diagnose.Runners.Count} managed runner(s) have no ERROR state."
                        : $"{runnerErrors.Length} managed runner(s) are in ERROR: {string.Join(", ", runnerErrors.Select(runner => runner.RunnerName))}."));

                var external = diagnose.SystemProcesses.Count(process => process.Kind == RunnerSystemFindingKind.External);
                var unattributed = diagnose.SystemProcesses.Count(process => process.Kind == RunnerSystemFindingKind.Unattributed);
                findings.Add(new DoctorFinding(
                    "runtime.external",
                    "External/unattributed runners",
                    unattributed > 0 ? DoctorCheckStatus.Warning : DoctorCheckStatus.Pass,
                    $"External runner process(es): {external}; unattributed runner process(es): {unattributed}. External processes are outside MRC control."));
            }
            catch (Exception ex)
            {
                findings.Add(new DoctorFinding("runtime.managed", "Managed runtime health", DoctorCheckStatus.Fail, $"Runtime diagnostics failed safely: {ex.Message}"));
            }
        }

        findings.Add(await CheckReleaseReachabilityAsync(cancellationToken));
        return findings;
    }

    private static DoctorHealth DetermineHealth(
        IReadOnlyList<DoctorFinding> findings,
        IReadOnlyList<DoctorRepairAction> repairs)
    {
        if (findings.Any(finding => finding.Status == DoctorCheckStatus.Fail))
        {
            return DoctorHealth.Blocked;
        }

        if (findings.Any(finding => finding.Status == DoctorCheckStatus.Warning)
            || repairs.Any(repair => !repair.Attempted && repair.Risk == DoctorRepairRisk.ApprovalRequired))
        {
            return DoctorHealth.Attention;
        }

        if (repairs.Any(repair => repair.Attempted && repair.Succeeded && repair.VerificationPassed))
        {
            return DoctorHealth.Repaired;
        }

        return DoctorHealth.Healthy;
    }

    private static bool IsAuthorizedPreCertOrFinalVersion(string value)
    {
        if (!Version.TryParse(value, out var parsed) || parsed.Major != 0) return false;
        if (parsed.Minor == 0) return parsed.Build >= 0;
        return parsed.Minor == 1 && parsed.Build == 0;
    }

    private static async Task<DoctorFinding> CheckReleaseReachabilityAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(4) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd($"MRC/{BuildInfo.Version}");
            using var response = await client.GetAsync(ReleasesEndpoint, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return new DoctorFinding(
                "release.reachability",
                "Release reachability",
                response.IsSuccessStatusCode ? DoctorCheckStatus.Pass : DoctorCheckStatus.Warning,
                response.IsSuccessStatusCode
                    ? "GitHub Releases collection is reachable."
                    : $"GitHub Releases collection responded {(int)response.StatusCode} {response.ReasonPhrase}." );
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            return new DoctorFinding("release.reachability", "Release reachability", DoctorCheckStatus.Warning, $"GitHub Releases collection could not be reached: {ex.Message}");
        }
    }
}
