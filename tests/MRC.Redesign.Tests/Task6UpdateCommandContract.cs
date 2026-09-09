using System.Reflection;
using System.Runtime.CompilerServices;
using MRC.Cli;
using MRC.Core;
using MRC.Core.Diagnostics;
using MRC.Core.Updating;

internal static class Task6UpdateCommandContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifyStagedUpdatePresentation().GetAwaiter().GetResult();
        Console.WriteLine("PASS  Task6F staged semantic update presentation");
    }

    private static async Task VerifyStagedUpdatePresentation()
    {
        var progressLine = typeof(CliPresentation).GetMethod(
            "UpdateProgressLine",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(UpdateProgress) },
            modifiers: null);
        Require(progressLine is not null, "CliPresentation.UpdateProgressLine(UpdateProgress) is missing.");

        var probe = progressLine!.Invoke(null, new object[]
        {
            new UpdateProgress(UpdateProgressStage.Sha256Verify, "Verifying package SHA-256.", 35)
        });
        Require(probe is not null, "UpdateProgressLine returned null.");
        var probeText = probe!.GetType().GetProperty("Text")?.GetValue(probe)?.ToString() ?? string.Empty;
        var probeTone = probe.GetType().GetProperty("Tone")?.GetValue(probe)?.ToString() ?? string.Empty;
        Require(probeText.Contains("SHA256", StringComparison.OrdinalIgnoreCase)
                || probeText.Contains("SHA-256", StringComparison.OrdinalIgnoreCase),
            $"SHA progress line is not identifiable: {probeText}");
        Require(probeTone == "Heading",
            $"Active SHA verification stage tone is {probeTone}, expected Heading/action cyan.");

        var doctorType = typeof(Func<DoctorRunOptions, CancellationToken, Task<DoctorReport>>);
        var updateType = typeof(Func<IProgress<UpdateProgress>, CancellationToken, Task<UpdateResult>>);
        var fenceType = typeof(Func<EnvironmentFenceResult>);
        var constructor = typeof(CliDispatcher).GetConstructor(new[]
        {
            typeof(IGuiLauncher), doctorType, updateType, fenceType
        });
        Require(constructor is not null,
            "CliDispatcher lacks injectable update/fence seams for deterministic staged-output verification.");

        Func<DoctorRunOptions, CancellationToken, Task<DoctorReport>> doctorRunner = (_, _) =>
            Task.FromResult(new DoctorReport(
                Array.Empty<DoctorFinding>(),
                Array.Empty<DoctorRepairAction>(),
                Array.Empty<DoctorVerificationResult>(),
                DoctorHealth.Healthy));

        Func<IProgress<UpdateProgress>, CancellationToken, Task<UpdateResult>> updateRunner = (progress, _) =>
        {
            foreach (var stage in Enum.GetValues<UpdateProgressStage>())
            {
                progress.Report(new UpdateProgress(stage, $"stage-{stage}", (int)stage * 10));
            }

            return Task.FromResult(new UpdateResult(
                UpdateOutcome.Updated,
                "MRC updated from 0.0.11 to 0.0.12. Previous version retained for rollback.",
                new Version(0, 0, 11),
                new Version(0, 0, 12)));
        };

        Func<EnvironmentFenceResult> fence = () => new EnvironmentFenceResult(
            true,
            MrcConstants.TargetMachineName,
            MrcConstants.RunnerRoot,
            "AUTHORIZED",
            "test authority");

        var dispatcher = (CliDispatcher)constructor!.Invoke(new object[]
        {
            new SuccessfulLauncher(), doctorRunner, updateRunner, fence
        });

        var output = new StringWriter();
        var error = new StringWriter();
        var exitCode = await dispatcher.ExecuteAsync(new[] { "-update" }, output, error);
        var text = output.ToString();

        Require(exitCode == 0, $"MRC -update returned exit code {exitCode} for a successful fake update.");
        Require(string.IsNullOrWhiteSpace(error.ToString()), $"Successful staged update wrote to stderr: {error}");
        Require(!text.Contains("\u001b[", StringComparison.Ordinal),
            "Redirected staged update output contains ANSI escape sequences.");

        var expectedOrder = Enum.GetValues<UpdateProgressStage>();
        var cursor = -1;
        foreach (var stage in expectedOrder)
        {
            var index = text.IndexOf(stage.ToString(), cursor + 1, StringComparison.OrdinalIgnoreCase);
            Require(index > cursor, $"Update stage {stage} is missing or out of order: {text}");
            cursor = index;
        }

        Require(text.Contains("0.0.11", StringComparison.Ordinal)
                && text.Contains("0.0.12", StringComparison.Ordinal),
            $"Update completion does not show previous/current versions: {text}");
        Require(text.Contains("rollback", StringComparison.OrdinalIgnoreCase),
            $"Update completion does not expose rollback retention: {text}");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class SuccessfulLauncher : IGuiLauncher
    {
        public GuiLaunchResult Launch() => new(true, "MRC GUI launch requested.");
    }
}
