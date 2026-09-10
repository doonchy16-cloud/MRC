using System.Reflection;
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
        VerifySharedIntensitySignal();
        VerifyIntensityIsRendered();
        VerifyStaticStates();
        VerifySharedClockArchitecture();
        Console.WriteLine("PASS  Task9A/9C state-specific true-20-FPS shared-clock animation + rendered intensity");
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

    private static void VerifySharedIntensitySignal()
    {
        var intensityProperty = typeof(RunnerRowViewModel).GetProperty(
            "AnimationIntensity",
            BindingFlags.Public | BindingFlags.Instance);
        Require(intensityProperty is not null && intensityProperty.PropertyType == typeof(double),
            "RunnerRowViewModel.AnimationIntensity double property is missing.");

        var clockSource = File.ReadAllText(Path.Combine(
            Directory.GetCurrentDirectory(), "src", "MRC.Gui", "Presentation", "RunnerAnimationClock.cs"));
        Require(clockSource.Contains("AnimationIntensity", StringComparison.Ordinal),
            "RunnerAnimationClock does not drive AnimationIntensity from the shared clock.");

        var origin = DateTimeOffset.Parse("2026-09-09T22:00:00Z");
        foreach (var state in new[] { RunnerState.BUSY, RunnerState.STARTING, RunnerState.STOPPING })
        {
            var clock = new RunnerAnimationClock();
            var row = Row(state);
            var values = new List<double>();
            for (var tick = 0; tick < 12; tick++)
            {
                clock.Tick(origin.AddMilliseconds(tick * 50), new[] { row });
                var value = (double)intensityProperty!.GetValue(row)!;
                Require(value >= 0.55 && value <= 1.0,
                    $"{state} AnimationIntensity must stay in [0.55, 1.00], got {value:F4}.");
                values.Add(value);
                Require(row.State == state,
                    $"Animation clock changed runner truth from {state} to {row.State}.");
            }

            Require(values.Select(value => Math.Round(value, 4)).Distinct().Count() >= 3,
                $"{state} AnimationIntensity did not visibly vary across shared 50 ms ticks.");
        }
    }

    private static void VerifyIntensityIsRendered()
    {
        var controlsRoot = Path.Combine(Directory.GetCurrentDirectory(), "src", "MRC.Gui", "Controls");
        var cardXaml = File.ReadAllText(Path.Combine(controlsRoot, "RunnerControlCard.xaml"));
        var orbXaml = File.ReadAllText(Path.Combine(controlsRoot, "StateOrb.xaml"));

        Require(cardXaml.Contains("<controls:StateOrb", StringComparison.Ordinal)
                && cardXaml.Contains("Intensity=\"{Binding Row.AnimationIntensity, ElementName=Root}\"", StringComparison.Ordinal),
            "Extracted runner card does not route the shared AnimationIntensity signal into its active StateOrb indicator.");
        Require(orbXaml.Contains("Opacity=\"{Binding Intensity, ElementName=Root}\"", StringComparison.Ordinal),
            "StateOrb does not render its shared Intensity input through the luminous halo.");
    }

    private static void VerifyStaticStates()
    {
        var intensityProperty = typeof(RunnerRowViewModel).GetProperty("AnimationIntensity");
        var clock = new RunnerAnimationClock();
        var origin = DateTimeOffset.Parse("2026-09-09T22:00:00Z");

        var off = Row(RunnerState.OFF);
        clock.Tick(origin, new[] { off });
        clock.Tick(origin.AddSeconds(10), new[] { off });
        Require(off.Glyph == "-", $"OFF glyph must remain '-', got '{off.Glyph}'.");
        if (intensityProperty is not null)
        {
            Require((double)intensityProperty.GetValue(off)! == 1.0, "OFF intensity must remain static at 1.0.");
        }

        var error = Row(RunnerState.ERROR);
        clock.Tick(origin, new[] { error });
        clock.Tick(origin.AddSeconds(10), new[] { error });
        Require(error.Glyph == "!", $"ERROR glyph must remain '!', got '{error.Glyph}'.");
        if (intensityProperty is not null)
        {
            Require((double)intensityProperty.GetValue(error)! == 1.0, "ERROR intensity must remain static at 1.0.");
        }
    }

    private static void VerifySharedClockArchitecture()
    {
        var repoRoot = Directory.GetCurrentDirectory();
        var mainWindow = File.ReadAllText(Path.Combine(repoRoot, "src", "MRC.Gui", "MainWindow.xaml.cs"));
        var rowViewModel = File.ReadAllText(Path.Combine(repoRoot, "src", "MRC.Gui", "Presentation", "RunnerRowViewModel.cs"));
        var clockSource = File.ReadAllText(Path.Combine(repoRoot, "src", "MRC.Gui", "Presentation", "RunnerAnimationClock.cs"));

        Require(mainWindow.Contains("_animationTimer.Interval = TimeSpan.FromMilliseconds(50)", StringComparison.Ordinal),
            "Shared GUI animation timer must tick at exactly 50 ms (20 FPS).");
        Require(mainWindow.Contains("_animationClock.Tick", StringComparison.Ordinal),
            "MainWindow does not drive rows through the shared RunnerAnimationClock.");
        Require(Count(mainWindow, "new DispatcherTimer()") == 2,
            "MainWindow must retain exactly one refresh timer plus one shared animation timer.");
        Require(!rowViewModel.Contains("DispatcherTimer", StringComparison.Ordinal)
                && !rowViewModel.Contains("System.Timers", StringComparison.Ordinal),
            "Runner rows must never own per-row animation timers.");
        Require(!clockSource.Contains("DispatcherTimer", StringComparison.Ordinal),
            "RunnerAnimationClock must remain timer-agnostic and driven by one shared GUI timer.");
    }

    private static int Count(string source, string token)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(token, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += token.Length;
        }
        return count;
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
