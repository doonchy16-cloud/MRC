using MRC.Core.Runners;
using MRC.Core.Runtime;
using MRC.Gui.Presentation;

namespace MRC.Pass3.Tests;

internal static class AnimationAcceptance
{
    private static readonly DateTimeOffset T0 = new(2026, 9, 9, 0, 0, 0, TimeSpan.Zero);

    public static int Run()
    {
        var tests = new (string Name, Action Body)[]
        {
            ("OFF and ERROR glyphs remain frozen", FrozenStates),
            ("IDLE uses breathing sequence at 550 ms", IdleSequence),
            ("BUSY uses fast work spinner at 110 ms", BusySequence),
            ("STARTING uses fill sequence at 180 ms", StartingSequence),
            ("STOPPING uses drain sequence at 250 ms", StoppingSequence),
            ("one shared clock drives bounded intensity without changing state", SharedClockTruth)
        };
        var failures = 0;
        Console.WriteLine($"MRC PASS 3 animation harness — {tests.Length} tests");
        foreach (var test in tests)
        {
            try { test.Body(); Console.WriteLine($"PASS  {test.Name}"); }
            catch (Exception ex) { failures++; Console.WriteLine($"FAIL  {test.Name}\n      {ex.Message}"); }
        }
        Console.WriteLine(failures == 0 ? $"PASS  all {tests.Length} PASS 3 animation tests" : $"FAIL  {failures} of {tests.Length} PASS 3 animation tests");
        Console.WriteLine(); return failures;
    }

    private static void FrozenStates()
    {
        var clock = new RunnerAnimationClock(); var off = Row("off", RunnerState.OFF); var error = Row("error", RunnerState.ERROR);
        clock.Tick(T0, new[] { off, error }); clock.Tick(T0.AddSeconds(5), new[] { off, error });
        Require(off.Glyph == "-" && off.AnimationIntensity == 1.0, "OFF must remain static '-' at intensity 1.0.");
        Require(error.Glyph == "!" && error.AnimationIntensity == 1.0, "ERROR must remain static '!' at intensity 1.0.");
    }

    private static void IdleSequence() => AssertSequence(RunnerState.IDLE, 550, new[] { "·", "•", "●", "•" });
    private static void BusySequence() => AssertSequence(RunnerState.BUSY, 110, new[] { "⠋", "⠙", "⠹", "⠸" });
    private static void StartingSequence() => AssertSequence(RunnerState.STARTING, 180, new[] { "▏", "▎", "▍", "▌" });
    private static void StoppingSequence() => AssertSequence(RunnerState.STOPPING, 250, new[] { "█", "▉", "▊", "▋" });

    private static void AssertSequence(RunnerState state, int interval, IReadOnlyList<string> expected)
    {
        var clock = new RunnerAnimationClock(); var row = Row(state.ToString(), state);
        for (var i = 0; i < expected.Count; i++) { clock.Tick(T0.AddMilliseconds(i * interval), new[] { row }); Require(row.Glyph == expected[i], $"{state} frame {i} expected '{expected[i]}', got '{row.Glyph}'."); }
    }

    private static void SharedClockTruth()
    {
        var clock = new RunnerAnimationClock();
        var rows = new[] { Row("idle", RunnerState.IDLE), Row("busy", RunnerState.BUSY), Row("starting", RunnerState.STARTING), Row("stopping", RunnerState.STOPPING) };
        var states = rows.Select(r => r.State).ToArray(); var intensities = new List<double>();
        for (var tick = 0; tick < 12; tick++) { clock.Tick(T0.AddMilliseconds(tick * 55), rows); intensities.Add(rows[1].AnimationIntensity); }
        Require(states.SequenceEqual(rows.Select(r => r.State)), "Animation mutated runtime state truth.");
        Require(intensities.All(v => v >= 0.55 && v <= 1.0) && intensities.Select(v => Math.Round(v, 4)).Distinct().Count() >= 3, "Shared clock does not drive bounded visible intensity variation.");
        var main = File.ReadAllText(Path.Combine(RepoRoot(), "src", "MRC.Gui", "MainWindow.xaml.cs"));
        var rowSource = File.ReadAllText(Path.Combine(RepoRoot(), "src", "MRC.Gui", "Presentation", "RunnerRowViewModel.cs"));
        Require(main.Contains("_animationTimer.Interval = TimeSpan.FromMilliseconds(55)") && !rowSource.Contains("DispatcherTimer"), "Animation must remain one shared 55 ms GUI timer with no per-row timers.");
    }

    private static RunnerRowViewModel Row(string name, RunnerState state) => new(new RunnerSnapshot(new RunnerDescriptor($@"C:\R\{name}", name, $"https://github.com/x/{name}", "Repo", 1, "_work", null), state, state == RunnerState.ERROR ? "error" : null));
    private static string RepoRoot(){var d=new DirectoryInfo(Directory.GetCurrentDirectory());while(d is not null){if(File.Exists(Path.Combine(d.FullName,"Auth","0000_MasterAuth.md")))return d.FullName;d=d.Parent;}throw new InvalidOperationException("Could not locate MRC repository root.");}
    private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
}
