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
            ("IDLE uses exact canonical spinner sequence at 550 ms", IdleSequence),
            ("BUSY advances at exactly 110 ms", BusySpeed),
            ("STARTING advances at exactly 180 ms", StartingSpeed),
            ("STOPPING advances at exactly 250 ms", StoppingSpeed),
            ("one shared clock advances mixed runner states", SharedClock),
            ("animation never changes runtime state", StateTruthfulness)
        };

        var failures = 0;
        Console.WriteLine($"MRC PASS 3 animation harness — {tests.Length} tests");
        foreach (var test in tests)
        {
            try
            {
                test.Body();
                Console.WriteLine($"PASS  {test.Name}");
            }
            catch (Exception ex)
            {
                failures++;
                Console.WriteLine($"FAIL  {test.Name}");
                Console.WriteLine($"      {ex.Message}");
            }
        }

        Console.WriteLine(failures == 0
            ? $"PASS  all {tests.Length} PASS 3 animation tests"
            : $"FAIL  {failures} of {tests.Length} PASS 3 animation tests");
        Console.WriteLine();
        return failures;
    }

    private static void FrozenStates()
    {
        var clock = new RunnerAnimationClock();
        var off = Row("off", RunnerState.OFF);
        var error = Row("error", RunnerState.ERROR);

        clock.Tick(T0, new[] { off, error });
        clock.Tick(T0.AddSeconds(5), new[] { off, error });

        Require(off.Glyph == "-", $"OFF must be frozen '-', got '{off.Glyph}'.");
        Require(error.Glyph == "!", $"ERROR must be frozen '!', got '{error.Glyph}'.");
    }

    private static void IdleSequence()
    {
        var clock = new RunnerAnimationClock();
        var row = Row("idle", RunnerState.IDLE);

        clock.Tick(T0, new[] { row });
        Require(row.Glyph == "/", "IDLE initial frame must be '/'.");
        clock.Tick(T0.AddMilliseconds(549), new[] { row });
        Require(row.Glyph == "/", "IDLE advanced before 550 ms.");
        clock.Tick(T0.AddMilliseconds(550), new[] { row });
        Require(row.Glyph == "-", "IDLE second frame must be '-'.");
        clock.Tick(T0.AddMilliseconds(1100), new[] { row });
        Require(row.Glyph == "\\", "IDLE third frame must be '\\'.");
        clock.Tick(T0.AddMilliseconds(1650), new[] { row });
        Require(row.Glyph == "|", "IDLE fourth frame must be '|'.");
        clock.Tick(T0.AddMilliseconds(2200), new[] { row });
        Require(row.Glyph == "/", "IDLE sequence did not wrap to '/'.");
    }

    private static void BusySpeed() => AssertSpeed(RunnerState.BUSY, 110);
    private static void StartingSpeed() => AssertSpeed(RunnerState.STARTING, 180);
    private static void StoppingSpeed() => AssertSpeed(RunnerState.STOPPING, 250);

    private static void AssertSpeed(RunnerState state, int intervalMs)
    {
        var clock = new RunnerAnimationClock();
        var row = Row(state.ToString(), state);
        clock.Tick(T0, new[] { row });
        clock.Tick(T0.AddMilliseconds(intervalMs - 1), new[] { row });
        Require(row.Glyph == "/", $"{state} advanced before {intervalMs} ms.");
        clock.Tick(T0.AddMilliseconds(intervalMs), new[] { row });
        Require(row.Glyph == "-", $"{state} did not advance at {intervalMs} ms.");
    }

    private static void SharedClock()
    {
        var clock = new RunnerAnimationClock();
        var idle = Row("idle", RunnerState.IDLE);
        var busy = Row("busy", RunnerState.BUSY);
        var starting = Row("starting", RunnerState.STARTING);
        var stopping = Row("stopping", RunnerState.STOPPING);

        var rows = new[] { idle, busy, starting, stopping };
        clock.Tick(T0, rows);
        clock.Tick(T0.AddMilliseconds(250), rows);

        Require(idle.Glyph == "/", "IDLE should not yet advance at 250 ms.");
        Require(busy.Glyph == "\\", $"BUSY should have advanced two frames by 250 ms, got '{busy.Glyph}'.");
        Require(starting.Glyph == "-", "STARTING should have advanced once by 250 ms.");
        Require(stopping.Glyph == "-", "STOPPING should advance exactly once at 250 ms.");
    }

    private static void StateTruthfulness()
    {
        var clock = new RunnerAnimationClock();
        var rows = Enum.GetValues<RunnerState>().Select(state => Row(state.ToString(), state)).ToArray();
        var before = rows.Select(row => row.State).ToArray();

        clock.Tick(T0, rows);
        clock.Tick(T0.AddSeconds(2), rows);

        Require(before.SequenceEqual(rows.Select(row => row.State)), "Animation mutated runtime state.");
    }

    private static RunnerRowViewModel Row(string name, RunnerState state) =>
        new(new RunnerSnapshot(
            new RunnerDescriptor($@"C:\R\{name}", name, $"https://github.com/x/{name}", "Repo", 1, "_work", null),
            state,
            state == RunnerState.ERROR ? "error" : null));

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
