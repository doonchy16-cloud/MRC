using MRC.Core.Runtime;

namespace MRC.Gui.Presentation;

public sealed class RunnerAnimationClock
{
    private static readonly string[] Frames = ["/", "-", "\\", "|"];
    private readonly Dictionary<RunnerRowViewModel, FrameState> _states = new();

    public void Tick(DateTimeOffset now, IEnumerable<RunnerRowViewModel> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var activeRows = rows.ToArray();
        var activeSet = activeRows.ToHashSet();

        foreach (var stale in _states.Keys.Where(row => !activeSet.Contains(row)).ToArray())
        {
            _states.Remove(stale);
        }

        foreach (var row in activeRows)
        {
            switch (row.State)
            {
                case RunnerState.OFF:
                    _states.Remove(row);
                    row.Glyph = "-";
                    continue;
                case RunnerState.ERROR:
                    _states.Remove(row);
                    row.Glyph = "!";
                    continue;
            }

            var interval = IntervalFor(row.State);
            if (!_states.TryGetValue(row, out var state) || state.RuntimeState != row.State || now < state.LastAdvance)
            {
                row.Glyph = Frames[0];
                _states[row] = new FrameState(row.State, 0, now);
                continue;
            }

            var elapsedMs = (now - state.LastAdvance).TotalMilliseconds;
            var steps = (int)Math.Floor(elapsedMs / interval.TotalMilliseconds);
            if (steps <= 0) continue;

            var nextIndex = (state.FrameIndex + steps) % Frames.Length;
            row.Glyph = Frames[nextIndex];
            _states[row] = state with
            {
                FrameIndex = nextIndex,
                LastAdvance = state.LastAdvance.AddMilliseconds(steps * interval.TotalMilliseconds)
            };
        }
    }

    public static TimeSpan IntervalFor(RunnerState state) => state switch
    {
        RunnerState.IDLE => TimeSpan.FromMilliseconds(550),
        RunnerState.BUSY => TimeSpan.FromMilliseconds(110),
        RunnerState.STARTING => TimeSpan.FromMilliseconds(180),
        RunnerState.STOPPING => TimeSpan.FromMilliseconds(250),
        RunnerState.OFF or RunnerState.ERROR => Timeout.InfiniteTimeSpan,
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown runner state.")
    };

    private sealed record FrameState(RunnerState RuntimeState, int FrameIndex, DateTimeOffset LastAdvance);
}
