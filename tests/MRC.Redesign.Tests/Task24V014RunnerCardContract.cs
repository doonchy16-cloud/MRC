using System.Runtime.CompilerServices;
using MRC.Core.Runners;
using MRC.Core.Runtime;
using MRC.Gui.Presentation;

internal static class Task24V014RunnerCardContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifyStateDerivedControlAuthority();
        VerifyControlAuthorityUpdatesWithRuntimeTruth();
        VerifyWindowDelegatesActionsToOperationsService();
        Console.WriteLine("PASS  Task24 v0.0.14 runner-card controls match shared lifecycle authority");
    }

    private static void VerifyStateDerivedControlAuthority()
    {
        AssertControls(RunnerState.OFF,      canStart: true,  canStop: false, canRestart: false, canShowDetails: false);
        AssertControls(RunnerState.IDLE,     canStart: false, canStop: true,  canRestart: true,  canShowDetails: false);
        AssertControls(RunnerState.BUSY,     canStart: false, canStop: false, canRestart: false, canShowDetails: true);
        AssertControls(RunnerState.STARTING, canStart: false, canStop: false, canRestart: false, canShowDetails: false);
        AssertControls(RunnerState.STOPPING, canStart: false, canStop: false, canRestart: false, canShowDetails: false);
        AssertControls(RunnerState.ERROR,    canStart: false, canStop: false, canRestart: false, canShowDetails: true);
    }

    private static void VerifyControlAuthorityUpdatesWithRuntimeTruth()
    {
        var runner = Descriptor();
        var row = new RunnerRowViewModel(new RunnerSnapshot(runner, RunnerState.OFF, null));
        Require(row.CanStart && !row.CanStop && !row.CanRestart, "OFF card authority is incorrect before update.");

        row.Update(new RunnerSnapshot(runner, RunnerState.IDLE, null));
        Require(!row.CanStart && row.CanStop && row.CanRestart,
            "Card control authority did not update when runtime truth changed OFF -> IDLE.");

        row.Update(new RunnerSnapshot(runner, RunnerState.BUSY, null));
        Require(!row.CanStart && !row.CanStop && !row.CanRestart && row.CanShowDetails,
            "BUSY card authority did not become protected/details-only.");
    }

    private static void VerifyWindowDelegatesActionsToOperationsService()
    {
        var source = File.ReadAllText(Path.Combine(
            Directory.GetCurrentDirectory(), "src", "MRC.Gui", "MainWindow.xaml.cs"));

        foreach (var handler in new[]
        {
            "RunnerStart_OnClick",
            "RunnerStop_OnClick",
            "RunnerRestart_OnClick",
            "RunnerDetails_OnClick"
        })
        {
            Require(source.Contains(handler, StringComparison.Ordinal), $"MainWindow is missing {handler}.");
        }

        Require(source.Contains("_operations.Start(row.Runner)", StringComparison.Ordinal),
            "START button does not delegate to RunnerOperationsService.Start.");
        Require(source.Contains("_operations.StopIdle(row.Runner)", StringComparison.Ordinal),
            "STOP button does not delegate to RunnerOperationsService.StopIdle.");
        Require(source.Contains("_operations.Restart(row.Runner)", StringComparison.Ordinal),
            "RESTART button does not delegate to RunnerOperationsService.Restart.");

        Require(!source.Contains("_engine.Start(", StringComparison.Ordinal)
                && !source.Contains("_engine.StopIdle(", StringComparison.Ordinal)
                && !source.Contains("_engine.Restart(", StringComparison.Ordinal)
                && !source.Contains("Process.Start(", StringComparison.Ordinal)
                && !source.Contains("Kill(", StringComparison.Ordinal),
            "MainWindow contains direct lifecycle/process mutation instead of delegating to the operations core.");
    }

    private static void AssertControls(
        RunnerState state,
        bool canStart,
        bool canStop,
        bool canRestart,
        bool canShowDetails)
    {
        var row = new RunnerRowViewModel(new RunnerSnapshot(
            Descriptor(),
            state,
            state == RunnerState.ERROR ? "test error" : null));

        Require(row.CanStart == canStart, $"{state} CanStart={row.CanStart}; expected {canStart}.");
        Require(row.CanStop == canStop, $"{state} CanStop={row.CanStop}; expected {canStop}.");
        Require(row.CanRestart == canRestart, $"{state} CanRestart={row.CanRestart}; expected {canRestart}.");
        Require(row.CanShowDetails == canShowDetails,
            $"{state} CanShowDetails={row.CanShowDetails}; expected {canShowDetails}.");
    }

    private static RunnerDescriptor Descriptor() => new(
        @"D:\Git_Runners_Main\card-test",
        "CardRunner",
        "https://github.com/doonchy16-cloud/card-test",
        "card-test",
        1,
        "_work",
        null);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
