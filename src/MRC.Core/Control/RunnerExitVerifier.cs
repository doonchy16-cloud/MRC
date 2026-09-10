using MRC.Core.Runners;
using MRC.Core.Runtime;

namespace MRC.Core.Control;

internal sealed class RunnerExitVerifier : IRunnerExitVerifier
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(100);

    private readonly IProcessSnapshotProvider _processProvider;
    private readonly Func<DateTimeOffset> _clock;
    private readonly Action<TimeSpan> _delay;

    public RunnerExitVerifier(IProcessSnapshotProvider processProvider)
        : this(processProvider, () => DateTimeOffset.UtcNow, Thread.Sleep)
    {
    }

    internal RunnerExitVerifier(
        IProcessSnapshotProvider processProvider,
        Func<DateTimeOffset> clock,
        Action<TimeSpan> delay)
    {
        _processProvider = processProvider ?? throw new ArgumentNullException(nameof(processProvider));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _delay = delay ?? throw new ArgumentNullException(nameof(delay));
    }

    public bool WaitUntilOff(RunnerDescriptor runner, TimeSpan timeout, out string? error)
    {
        ArgumentNullException.ThrowIfNull(runner);
        var deadline = _clock().Add(timeout < TimeSpan.Zero ? TimeSpan.Zero : timeout);
        string? lastError = null;

        while (true)
        {
            ProcessInventory inventory;
            try
            {
                inventory = _processProvider.Capture();
            }
            catch (Exception ex)
            {
                lastError = $"Process inventory capture failed while verifying shutdown: {ex.Message}";
                if (_clock() >= deadline)
                {
                    error = lastError;
                    return false;
                }
                _delay(PollInterval);
                continue;
            }

            var association = RunnerProcessAssociator.AssociateExact(runner, inventory);
            if (association.Error is null && association.Listener is null && association.Workers.Count == 0)
            {
                error = null;
                return true;
            }

            lastError = association.Error
                ?? $"Runner processes are still present: listener PID {association.Listener?.ProcessId.ToString() ?? "<none>"}, workers {association.Workers.Count}.";

            if (_clock() >= deadline)
            {
                error = lastError;
                return false;
            }

            _delay(PollInterval);
        }
    }
}
