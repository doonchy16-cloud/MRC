using System.Runtime.CompilerServices;
using MRC.Cli;
using MRC.Core.Diagnostics;

internal static class Task6DoctorCommandContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifyAutomaticLowRiskRepairMode().GetAwaiter().GetResult();
        Console.WriteLine("PASS  Task6E1 Doctor automatic low-risk repair mode");
    }

    private static async Task VerifyAutomaticLowRiskRepairMode()
    {
        var runnerType = typeof(Func<DoctorRunOptions, CancellationToken, Task<DoctorReport>>);
        var constructor = typeof(CliDispatcher).GetConstructor(new[] { typeof(IGuiLauncher), runnerType });
        Require(constructor is not null,
            "CliDispatcher lacks an injectable Doctor runner seam for deterministic repair-mode verification.");

        DoctorRunOptions? captured = null;
        Func<DoctorRunOptions, CancellationToken, Task<DoctorReport>> doctorRunner = (options, _) =>
        {
            captured = options;
            return Task.FromResult(new DoctorReport(
                Array.Empty<DoctorFinding>(),
                Array.Empty<DoctorRepairAction>(),
                Array.Empty<DoctorVerificationResult>(),
                DoctorHealth.Healthy));
        };

        var dispatcher = (CliDispatcher)constructor!.Invoke(new object[]
        {
            new SuccessfulLauncher(),
            doctorRunner
        });

        var output = new StringWriter();
        var error = new StringWriter();
        var exitCode = await dispatcher.ExecuteAsync(new[] { "-doctor" }, output, error);

        Require(exitCode == 0, $"MRC -doctor returned exit code {exitCode} with a healthy fake report.");
        Require(captured is not null, "MRC -doctor did not invoke the injected Doctor runner.");
        Require(captured!.ApplyAutomaticRepairs,
            "MRC -doctor does not enable automatic low-risk repairs.");
        Require(captured.ApprovedRepairIds is null || captured.ApprovedRepairIds.Count == 0,
            "MRC -doctor implicitly approves higher-risk repairs; approval-required repairs must remain gated.");
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
