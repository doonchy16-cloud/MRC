using MRC.Core;
using MRC.Core.Diagnostics;
using MRC.Core.Runners;
using MRC.Core.Runtime;

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

    public static IReadOnlyList<CliLine> DoctorLines(DoctorReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        var lines = new List<CliLine>
        {
            new("MRC Doctor", CliTone.Heading),
            new($"Version: {BuildInfo.Version}", CliTone.Metadata),
            new($"Channel: {MrcConstants.ReleaseChannel}", CliTone.Metadata),
            new(string.Empty, CliTone.Normal),
            new("FIND", CliTone.Heading)
        };

        if (report.Findings.Count == 0)
        {
            lines.Add(new("  <no findings>", CliTone.Secondary));
        }
        else
        {
            foreach (var finding in report.Findings)
            {
                var (status, tone) = finding.Status switch
                {
                    DoctorCheckStatus.Pass => ("PASS", CliTone.Success),
                    DoctorCheckStatus.Warning => ("WARN", CliTone.Warning),
                    DoctorCheckStatus.Fail => ("FAIL", CliTone.Error),
                    _ => ("UNKNOWN", CliTone.Secondary)
                };
                lines.Add(new($"[{status}] {finding.Name} • {finding.Message}", tone));
            }
        }

        lines.Add(new(string.Empty, CliTone.Normal));
        lines.Add(new("REPAIR", CliTone.Heading));
        if (report.Repairs.Count == 0)
        {
            lines.Add(new("  <no repairs attempted>", CliTone.Secondary));
        }
        else
        {
            foreach (var repair in report.Repairs)
            {
                var tone = !repair.Attempted
                    ? CliTone.Warning
                    : repair.Succeeded && repair.VerificationPassed
                        ? CliTone.Success
                        : CliTone.Error;
                var status = !repair.Attempted
                    ? "SKIP"
                    : repair.Succeeded && repair.VerificationPassed
                        ? "PASS"
                        : "FAIL";
                lines.Add(new(
                    $"[{status}] {repair.Description} • {repair.Id} • Risk: {repair.Risk} • {repair.Message}",
                    tone));
            }
        }

        lines.Add(new(string.Empty, CliTone.Normal));
        lines.Add(new("VERIFY", CliTone.Heading));
        if (report.VerificationResults.Count == 0)
        {
            lines.Add(new("  <no verification results>", CliTone.Secondary));
        }
        else
        {
            foreach (var verification in report.VerificationResults)
            {
                lines.Add(new(
                    $"[{(verification.Passed ? "PASS" : "FAIL")}] {verification.FindingId} • {verification.Message}",
                    verification.Passed ? CliTone.Success : CliTone.Error));
            }
        }

        lines.Add(new(string.Empty, CliTone.Normal));
        lines.Add(new("RESULT", CliTone.Heading));
        lines.Add(new(
            $"Health: {report.Health.ToString().ToUpperInvariant()}",
            report.Health switch
            {
                DoctorHealth.Healthy => CliTone.Success,
                DoctorHealth.Repaired => CliTone.Success,
                DoctorHealth.Attention => CliTone.Warning,
                DoctorHealth.Blocked => CliTone.Error,
                _ => CliTone.Normal
            }));

        return lines;
    }

    public static IReadOnlyList<CliLine> DiagnoseLines(DiagnoseReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        var lines = new List<CliLine>
        {
            new("MRC Diagnose", CliTone.Heading),
            new($"Machine: {report.MachineName}", CliTone.Metadata),
            new($"User: {report.UserName}", CliTone.Metadata),
            new($"Session: {Value(report.SessionId)}", CliTone.Metadata),
            new($"Elevated: {(report.IsElevated ? "YES" : "NO")}", report.IsElevated ? CliTone.Warning : CliTone.Success),
            new($"Runner root: {MrcConstants.RunnerRoot}", CliTone.Path),
            new(string.Empty, CliTone.Normal),
            new($"Managed runners ({report.Runners.Count})", CliTone.Heading)
        };

        if (report.Runners.Count == 0)
        {
            lines.Add(new("  <none discovered>", CliTone.Secondary));
        }
        else
        {
            foreach (var runner in report.Runners)
            {
                lines.Add(new($"[{runner.State}] {runner.RunnerName} • {runner.RepositoryName}", ToneFor(runner.State)));
                lines.Add(new($"  Path: {runner.DirectoryPath}", CliTone.Path));
                lines.Add(new($"  Expected listener: {runner.ExpectedListenerPath}", CliTone.Path));
                lines.Add(new(
                    $"  Listener: PID {Value(runner.ListenerProcessId)} • Parent {Value(runner.ListenerParentProcessId)} • Session {Value(runner.ListenerSessionId)} • {runner.ListenerProcessName ?? "<not running>"}",
                    CliTone.Metadata));
                lines.Add(new($"  Executable: {runner.ListenerExecutablePath ?? "<unavailable>"}",
                    runner.ListenerExecutablePath is null ? CliTone.Secondary : CliTone.Path));
                lines.Add(new($"  Ownership: {runner.OwnershipProof}",
                    runner.ListenerProcessId is null ? CliTone.Secondary : CliTone.Success));
                lines.Add(new($"  Workers: {(runner.WorkerProcessIds.Count == 0 ? "<none>" : string.Join(", ", runner.WorkerProcessIds))}", CliTone.Secondary));

                if (!string.IsNullOrWhiteSpace(runner.InspectionError))
                {
                    lines.Add(new($"  Inspection: {runner.InspectionError}", CliTone.Warning));
                }

                if (!string.IsNullOrWhiteSpace(runner.Error))
                {
                    lines.Add(new($"  Error: {runner.Error}", CliTone.Error));
                }
            }
        }

        lines.Add(new(string.Empty, CliTone.Normal));
        lines.Add(new($"External / unattributed runner processes ({report.SystemProcesses.Count})", CliTone.Heading));
        if (report.SystemProcesses.Count == 0)
        {
            lines.Add(new("  <none observed>", CliTone.Secondary));
        }
        else
        {
            foreach (var process in report.SystemProcesses)
            {
                var kindTone = process.Kind == RunnerSystemFindingKind.External ? CliTone.Warning : CliTone.Error;
                lines.Add(new(
                    $"[{process.Kind}] PID {process.ProcessId} • Parent {Value(process.ParentProcessId)} • Session {Value(process.SessionId)} • {process.ProcessName}",
                    kindTone));
                lines.Add(new($"  Executable: {process.ExecutablePath ?? "<unavailable>"}",
                    process.ExecutablePath is null ? CliTone.Secondary : CliTone.Path));
                if (!string.IsNullOrWhiteSpace(process.ServiceName))
                {
                    lines.Add(new($"  Service: {process.ServiceName}", CliTone.Metadata));
                }

                lines.Add(new($"  Ownership: {process.OwnershipProof}", kindTone));
                if (!string.IsNullOrWhiteSpace(process.InspectionError))
                {
                    lines.Add(new($"  Inspection: {process.InspectionError}", CliTone.Warning));
                }
            }
        }

        return lines;
    }

    private static string Value(int? value) => value?.ToString() ?? "<unknown>";

    private static CliTone ToneFor(RunnerState state) => state switch
    {
        RunnerState.IDLE => CliTone.Success,
        RunnerState.BUSY => CliTone.Warning,
        RunnerState.ERROR => CliTone.Error,
        RunnerState.STARTING => CliTone.Heading,
        RunnerState.STOPPING => CliTone.Metadata,
        RunnerState.OFF => CliTone.Secondary,
        _ => CliTone.Normal
    };
}
