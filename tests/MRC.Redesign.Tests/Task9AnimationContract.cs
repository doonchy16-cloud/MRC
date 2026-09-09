using System.Runtime.CompilerServices;
using MRC.Core.Runners;
using MRC.Core.Runtime;
using MRC.Gui.Presentation;

internal static class Task9AnimationContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifyLockedCadences();
        VerifyStateSpecificFrames();
        VerifyStaticStates();
        VerifySharedClockArchitecture();
        Console.WriteLine("PASS  Task9A state-specific shared-clock animation");
    }

    private static void VerifyLockedCadences()
    {
        Require(RunnerAnimationClock.IntervalFor(RunnerState.IDLE) == TimeSpan.FromMilliseconds(550), "IDLE cadence must be 550 ms.");
        Require(RunnerAnimationClock.IntervalFor(RunnerState.BUSY) == TimeSpan.FromMilliseconds(110), "BUSY cadence must be 110 ms.");
        Require(RunnerAnimationClock.IntervalFor(RunnerState.STARTING) == TimeSpan.FromMilliseconds(180), "STARTING cadence must be 180 ms.");
        Require(RunnerAnimationClock.IntervalFor(RunnerState.STOPPING) == TimeSpan.FromMilliseconds(250), "STOPPING cadence must be 250 ms.");
    }

    private static void VerifyStateSpecificFrames()
    {
        VerifySequence(RunnerState.IDLE, 550, new[] { "·", "•", "●", "•" });
        VerifySequence(RunnerState.BUSY, 110, new[] { "⠋", "⠙", "⠹", "⠸" });
        VerifySequence(RunnerState.STARTING, 180, new[] { "▏", "▎", "▍", "▌" });
        VerifySequence(RunnerState.STOPPING, 250, new[] { "█", "▉", "▊", "▋" });
    }

    private static void VerifyStaticStates()
    {
        var clock = new RunnerAnimationClock();
        var origin = DateTimeOffset.Parse("2026-09-09T22:00:00Z");

        var off = Row(RunnerState.OFF);
        clock.Tick(origin, new[] { off });
        clock.Tick(origin.AddSeconds(10), new[] { off });
        Require(off.Glyph == "-", $"OFF glyph must remain '-', got '{off.Glyph}'.");

        var error = Row(RunnerState.ERROR);
        clock.Tick(origin, new[] { error });
        clock.Tick(origin.AddSeconds(10), new[] { error });
        Require(error.Glyph == "!", $"ERROR glyph must remain '!', got '{error.Glyph}'.");
    }

    private static void VerifySharedClockArchitecture()
    {
        var repoRoot = Directory.GetCurrentDirectory();
        var mainWindow = File.ReadAllText(Path.Combine(repoRoot, "src", "MRC.Gui", "MainWindow.xaml.cs"));
        var rowViewModel = File.ReadAllText(Path.Combine(repoRoot, "src", "MRC.Gui", "Presentation", "RunnerRowViewModel.cs"));
        var clockSource = File.ReadAllText(Path.Combine(repoRoot, "src", "MRC.Gui", "Presentation", "RunnerAnimationClock.cs"));

        Require(mainWindow.Contains("_animationTimer.Interval = TimeSpan.FromMilliseconds(55)", StringComparison.Ordinal),
            "Shared GUI animation timer must tick at 55 ms.");
        Require(mainWindow.Contains("_animationClock.Tick", StringComparison.Ordinal),
            "MainWindow does not drive rows through the shared RunnerAnimationClock.");
        Require(!rowViewModel.Contains("DispatcherTimer", StringComparison.Ordinal)
                && !rowViewModel.Contains("System.Timers", StringComparison.Ordinal),
            "Runner rows must never own per-row animation timers.");
        Require(!clockSource.Contains("DispatcherTimer", StringComparison.Ordinal),
            "RunnerAnimationClock must remain timer-agnostic and driven by one shared GUI timer.");
    }

    private static void VerifySequence(RunnerState state, int intervalMs, IReadOnlyList<string> expected)
    {
        var clock = new RunnerAnimationClock();
        var row = Row(state);
        var origin = DateTimeOffset.Parse("2026-09-09T22:00:00Z");

        for (var index = 0; index < expected.Count; index++)
        {
            clock.Tick(origin.AddMilliseconds(index * intervalMs), new[] { row });
            Require(row.Glyph == expected[index],
                $"{state} frame {index} expected '{expected[index]}', got '{row.Glyph}'.");
        }
    }

    private static RunnerRowViewModel Row(RunnerState state)
    {
        var runner = new RunnerDescriptor(
            @"D:\Git_Runners_Main\animation-test",
            "AnimationTest",
            "https://github.com/doonchy16-cloud/animation-test",
            "animation-test",
            1,
            "_work",
            null);
        return new RunnerRowViewModel(new RunnerSnapshot(runner, state, state == RunnerState.ERROR ? "test error" : null));
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
