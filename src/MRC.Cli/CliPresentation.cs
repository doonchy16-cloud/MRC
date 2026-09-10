using MRC.Core;
using MRC.Core.Diagnostics;
using MRC.Core.Runners;
using MRC.Core.Runtime;
using MRC.Core.Updating;

namespace MRC.Cli;

public sealed record CliSegment(string Text, CliTone Tone);

public sealed class CliLine
{
    public CliLine(string text, CliTone tone)
        : this(tone, new CliSegment(text, tone))
    {
    }

    public CliLine(CliTone tone, params CliSegment[] segments)
    {
        ArgumentNullException.ThrowIfNull(segments);
        Tone = tone;
        Segments = Array.AsReadOnly(segments.ToArray());
        Text = string.Concat(Segments.Select(segment => segment.Text));
    }

    public string Text { get; }
    public CliTone Tone { get; }
    public IReadOnlyList<CliSegment> Segments { get; }
}

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
            Field("Version: ", release.Version.ToString(), CliTone.Metadata),
            Field("Channel: ", release.Channel, CliTone.Metadata),
            Field("Stage: ", stage, CliTone.Metadata),
            Field("Final target: ", release.FinalTarget.ToString(), CliTone.Metadata),
            Field("Install location: ", installLocation, CliTone.Path),
            Field("Runner root: ", MrcConstants.RunnerRoot, CliTone.Path)
        };
    }

    public static CliLine UpdateProgressLine(UpdateProgress progress)
    {
        ArgumentNullException.ThrowIfNull(progress);
        var tone = progress.Stage switch
        {
            UpdateProgressStage.Compare => CliTone.Metadata,
            UpdateProgressStage.RollbackRetention => CliTone.Warning,
            UpdateProgressStage.Complete => LooksLikeFailure(progress.Message) ? CliTone.Error : CliTone.Success,
            _ => CliTone.Heading
        };

        var segments = new List<CliSegment>();
        if (progress.Percent is int value)
        {
            segments.Add(new CliSegment($"[{value,3}%] ", CliTone.Secondary));
        }
        segments.Add(new CliSegment(progress.Stage.ToString(), tone));
        segments.Add(new CliSegment(" • ", CliTone.Secondary));
        segments.Add(new CliSegment(progress.Message, tone));
        return new CliLine(tone, segments.ToArray());
    }

    public static IReadOnlyList<CliLine> DoctorLines(DoctorReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        var lines = new List<CliLine>
        {
            new("MRC Doctor", CliTone.Heading),
            Field("Version: ", BuildInfo.Version, CliTone.Metadata),
            Field("Channel: ", MrcConstants.ReleaseChannel, CliTone.Metadata),
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
                lines.Add(StatusLine(status, tone, $" {finding.Name} • {finding.Message}"));
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
                lines.Add(new CliLine(
                    tone,
                    new CliSegment($"[{status}]", tone),
                    new CliSegment($" {repair.Description} • ", CliTone.Normal),
                    new CliSegment(repair.Id, CliTone.Secondary),
                    new CliSegment(" • Risk: ", CliTone.Normal),
                    new CliSegment(repair.Risk.ToString(), repair.Risk == DoctorRepairRisk.Automatic ? CliTone.Success : CliTone.Warning),
                    new CliSegment(" • ", CliTone.Secondary),
                    new CliSegment(repair.Message, CliTone.Normal)));
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
                var tone = verification.Passed ? CliTone.Success : CliTone.Error;
                lines.Add(new CliLine(
                    tone,
                    new CliSegment($"[{(verification.Passed ? "PASS" : "FAIL")}]", tone),
                    new CliSegment($" {verification.FindingId} • {verification.Message}", CliTone.Normal)));
            }
        }

        lines.Add(new(string.Empty, CliTone.Normal));
        lines.Add(new("RESULT", CliTone.Heading));
        var healthTone = report.Health switch
        {
            DoctorHealth.Healthy => CliTone.Success,
            DoctorHealth.Repaired => CliTone.Success,
            DoctorHealth.Attention => CliTone.Warning,
            DoctorHealth.Blocked => CliTone.Error,
            _ => CliTone.Normal
        };
        lines.Add(Field("Health: ", report.Health.ToString().ToUpperInvariant(), healthTone));

        return lines;
    }

    public static IReadOnlyList<CliLine> DiagnoseLines(DiagnoseReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        var elevatedTone = report.IsElevated ? CliTone.Warning : CliTone.Success;
        var lines = new List<CliLine>
        {
            new("MRC Diagnose", CliTone.Heading),
            Field("Machine: ", report.MachineName, CliTone.Metadata),
            Field("User: ", report.UserName, CliTone.Metadata),
            Field("Session: ", Value(report.SessionId), CliTone.Metadata),
            Field("Elevated: ", report.IsElevated ? "YES" : "NO", elevatedTone),
            Field("Runner root: ", MrcConstants.RunnerRoot, CliTone.Path),
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
                var stateTone = ToneFor(runner.State);
                lines.Add(new CliLine(
                    stateTone,
                    new CliSegment($"[{runner.State}]", stateTone),
                    new CliSegment($" {runner.RunnerName} • ", CliTone.Normal),
                    new CliSegment(runner.RepositoryName, CliTone.Metadata)));
                lines.Add(Field("  Path: ", runner.DirectoryPath, CliTone.Path));
                lines.Add(Field("  Expected listener: ", runner.ExpectedListenerPath, CliTone.Path));
                lines.Add(new CliLine(
                    CliTone.Metadata,
                    new CliSegment("  Listener: ", CliTone.Normal),
                    new CliSegment($"PID {Value(runner.ListenerProcessId)}", CliTone.Metadata),
                    new CliSegment(" • Parent ", CliTone.Secondary),
                    new CliSegment(Value(runner.ListenerParentProcessId), CliTone.Metadata),
                    new CliSegment(" • Session ", CliTone.Secondary),
                    new CliSegment(Value(runner.ListenerSessionId), CliTone.Metadata),
                    new CliSegment(" • ", CliTone.Secondary),
                    new CliSegment(runner.ListenerProcessName ?? "<not running>", CliTone.Metadata)));
                lines.Add(Field(
                    "  Executable: ",
                    runner.ListenerExecutablePath ?? "<unavailable>",
                    runner.ListenerExecutablePath is null ? CliTone.Secondary : CliTone.Path));
                lines.Add(Field(
                    "  Ownership: ",
                    runner.OwnershipProof,
                    runner.ListenerProcessId is null ? CliTone.Secondary : CliTone.Success));
                lines.Add(Field(
                    "  Workers: ",
                    runner.WorkerProcessIds.Count == 0 ? "<none>" : string.Join(", ", runner.WorkerProcessIds),
                    CliTone.Secondary));

                if (!string.IsNullOrWhiteSpace(runner.InspectionError))
                {
                    lines.Add(Field("  Inspection: ", runner.InspectionError, CliTone.Warning));
                }

                if (!string.IsNullOrWhiteSpace(runner.Error))
                {
                    lines.Add(Field("  Error: ", runner.Error, CliTone.Error));
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
                lines.Add(new CliLine(
                    kindTone,
                    new CliSegment($"[{process.Kind}]", kindTone),
                    new CliSegment(" PID ", CliTone.Normal),
                    new CliSegment(process.ProcessId.ToString(), CliTone.Metadata),
                    new CliSegment(" • Parent ", CliTone.Secondary),
                    new CliSegment(Value(process.ParentProcessId), CliTone.Metadata),
                    new CliSegment(" • Session ", CliTone.Secondary),
                    new CliSegment(Value(process.SessionId), CliTone.Metadata),
                    new CliSegment(" • ", CliTone.Secondary),
                    new CliSegment(process.ProcessName, CliTone.Normal)));
                lines.Add(Field(
                    "  Executable: ",
                    process.ExecutablePath ?? "<unavailable>",
                    process.ExecutablePath is null ? CliTone.Secondary : CliTone.Path));
                if (!string.IsNullOrWhiteSpace(process.ServiceName))
                {
                    lines.Add(Field("  Service: ", process.ServiceName, CliTone.Metadata));
                }

                lines.Add(Field("  Ownership: ", process.OwnershipProof, kindTone));
                if (!string.IsNullOrWhiteSpace(process.InspectionError))
                {
                    lines.Add(Field("  Inspection: ", process.InspectionError, CliTone.Warning));
                }
            }
        }

        return lines;
    }

    private static CliLine Field(string label, string value, CliTone valueTone) =>
        new(
            valueTone,
            new CliSegment(label, CliTone.Normal),
            new CliSegment(value, valueTone));

    private static CliLine StatusLine(string status, CliTone statusTone, string rest) =>
        new(
            statusTone,
            new CliSegment($"[{status}]", statusTone),
            new CliSegment(rest, CliTone.Normal));

    private static bool LooksLikeFailure(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return false;
        var value = message.ToLowerInvariant();
        return value.Contains("failed", StringComparison.Ordinal)
            || value.Contains("failure", StringComparison.Ordinal)
            || value.Contains("missing", StringComparison.Ordinal)
            || value.Contains("incomplete", StringComparison.Ordinal)
            || value.Contains("not activated", StringComparison.Ordinal)
            || value.Contains("invalid", StringComparison.Ordinal);
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
