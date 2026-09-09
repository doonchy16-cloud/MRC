using MRC.Core.Runners;
using MRC.Core.Runtime;

namespace MRC.Core.Control;

public sealed class RunnerOperationsService
{
    private static readonly TimeSpan LaunchStagger = TimeSpan.FromMilliseconds(150);

    private readonly Func<IReadOnlyList<RunnerSnapshot>> _refresh;
    private readonly Func<RunnerDescriptor, RunnerControlResult> _start;
    private readonly Func<RunnerDescriptor, RunnerControlResult> _stopIdle;
    private readonly Func<RunnerDescriptor, bool, RunnerControlResult> _forceStopBusy;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;

    public RunnerOperationsService(RunnerEngine engine)
        : this(
            engine.Refresh,
            engine.Start,
            engine.StopIdle,
            engine.ForceStopBusy,
            static (delay, cancellationToken) => Task.Delay(delay, cancellationToken))
    {
        ArgumentNullException.ThrowIfNull(engine);
    }

    internal RunnerOperationsService(
        Func<IReadOnlyList<RunnerSnapshot>> refresh,
        Func<RunnerDescriptor, RunnerControlResult> start,
        Func<RunnerDescriptor, RunnerControlResult> stopIdle,
        Func<RunnerDescriptor, bool, RunnerControlResult> forceStopBusy,
        Func<TimeSpan, CancellationToken, Task> delay)
    {
        _refresh = refresh;
        _start = start;
        _stopIdle = stopIdle;
        _forceStopBusy = forceStopBusy;
        _delay = delay;
    }

    public RunnerControlResult Start(RunnerDescriptor runner) => _start(runner);

    public RunnerControlResult StopIdle(RunnerDescriptor runner) => _stopIdle(runner);

    public RunnerControlResult ForceStopBusy(RunnerDescriptor runner, bool confirmed) =>
        _forceStopBusy(runner, confirmed);

    public async Task<RunnerBulkOperationResult> TurnAllOnAsync(CancellationToken cancellationToken = default)
    {
        var snapshots = _refresh();
        var targets = snapshots.Where(snapshot => snapshot.State == RunnerState.OFF).ToArray();
        var attempted = 0;
        var succeeded = 0;
        var skipped = snapshots.Count - targets.Length;
        var errors = 0;
        var messages = new List<string>();

        for (var index = 0; index < targets.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            attempted++;
            var result = _start(targets[index].Runner);
            messages.Add($"{targets[index].Runner.AgentName ?? targets[index].Runner.DirectoryPath}: {result.Message}");

            switch (result.Outcome)
            {
                case RunnerControlOutcome.Starting:
                    succeeded++;
                    break;
                case RunnerControlOutcome.Error:
                    errors++;
                    break;
                default:
                    skipped++;
                    break;
            }

            if (index < targets.Length - 1)
            {
                await _delay(LaunchStagger, cancellationToken).ConfigureAwait(false);
            }
        }

        return new RunnerBulkOperationResult(attempted, succeeded, skipped, 0, errors, messages);
    }

    public RunnerBulkOperationResult TurnAllOff()
    {
        var snapshots = _refresh();
        var attempted = 0;
        var succeeded = 0;
        var skipped = 0;
        var busySkipped = 0;
        var errors = 0;
        var messages = new List<string>();

        foreach (var snapshot in snapshots)
        {
            if (snapshot.State == RunnerState.BUSY)
            {
                skipped++;
                busySkipped++;
                messages.Add($"{snapshot.Runner.AgentName ?? snapshot.Runner.DirectoryPath}: BUSY runner remains running.");
                continue;
            }

            if (snapshot.State != RunnerState.IDLE)
            {
                skipped++;
                continue;
            }

            attempted++;
            var result = _stopIdle(snapshot.Runner);
            messages.Add($"{snapshot.Runner.AgentName ?? snapshot.Runner.DirectoryPath}: {result.Message}");

            switch (result.Outcome)
            {
                case RunnerControlOutcome.Stopping:
                case RunnerControlOutcome.AlreadyOff:
                    succeeded++;
                    break;
                case RunnerControlOutcome.BusyProtected:
                    skipped++;
                    busySkipped++;
                    break;
                case RunnerControlOutcome.Error:
                    errors++;
                    break;
                default:
                    skipped++;
                    break;
            }
        }

        return new RunnerBulkOperationResult(attempted, succeeded, skipped, busySkipped, errors, messages);
    }
}
