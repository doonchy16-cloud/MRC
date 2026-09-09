using MRC.Core.Control;
using MRC.Core.Runners;
using MRC.Core.Runtime;

namespace MRC.Core;

public sealed class RunnerEngine
{
    private readonly string _root;
    private readonly IProcessSnapshotProvider _processProvider;
    private readonly RunnerTransitionTracker _transitions;
    private readonly RunnerControlService _control;
    private readonly Func<DateTimeOffset> _clock;
    private readonly Dictionary<string, RunnerDescriptor> _knownRunners = new(StringComparer.OrdinalIgnoreCase);

    public RunnerEngine()
    {
        var fence = EnvironmentFence.EvaluateCurrent();
        if (!fence.IsAuthorized)
        {
            throw new InvalidOperationException(fence.Message);
        }

        _root = MrcConstants.RunnerRoot;
        _clock = () => DateTimeOffset.UtcNow;
        _processProvider = new WindowsProcessSnapshotProvider();
        _transitions = new RunnerTransitionTracker();
        var launcher = new WindowsRunnerLauncher();
        var pathReader = new WindowsProcessPathReader();
        var killer = new WindowsProcessTreeKiller();
        var terminator = new WindowsRunnerProcessTerminator(_processProvider, pathReader, killer);
        var forceTerminator = new WindowsRunnerForceProcessTerminator(_processProvider, pathReader, killer);
        _control = new RunnerControlService(
            _root,
            _processProvider,
            launcher,
            terminator,
            forceTerminator,
            _transitions,
            _clock);
    }

    internal RunnerEngine(
        string root,
        IProcessSnapshotProvider processProvider,
        IRunnerLauncher launcher,
        IRunnerProcessTerminator terminator,
        Func<DateTimeOffset> clock)
    {
        _root = RunnerPath.Normalize(root);
        _clock = clock;
        _processProvider = processProvider;
        _transitions = new RunnerTransitionTracker();
        _control = new RunnerControlService(_root, _processProvider, launcher, terminator, _transitions, _clock);
    }

    internal RunnerEngine(
        string root,
        IProcessSnapshotProvider processProvider,
        IRunnerLauncher launcher,
        IRunnerProcessTerminator terminator,
        IRunnerForceProcessTerminator forceTerminator,
        Func<DateTimeOffset> clock)
    {
        _root = RunnerPath.Normalize(root);
        _clock = clock;
        _processProvider = processProvider;
        _transitions = new RunnerTransitionTracker();
        _control = new RunnerControlService(
            _root,
            _processProvider,
            launcher,
            terminator,
            forceTerminator,
            _transitions,
            _clock);
    }

    public IReadOnlyList<RunnerSnapshot> Refresh()
    {
        var discovered = RunnerDiscovery.Discover(_root);
        var discoveredByPath = discovered.ToDictionary(
            runner => RunnerPath.Normalize(runner.DirectoryPath),
            runner => runner,
            StringComparer.OrdinalIgnoreCase);

        foreach (var pair in discoveredByPath)
        {
            _knownRunners[pair.Key] = pair.Value;
        }

        foreach (var knownPath in _knownRunners.Keys.ToArray())
        {
            if (!Directory.Exists(knownPath))
            {
                _knownRunners.Remove(knownPath);
                _transitions.Clear(knownPath);
            }
        }

        ProcessInventory inventory;
        try
        {
            inventory = _processProvider.Capture();
        }
        catch (Exception ex)
        {
            inventory = new ProcessInventory(Array.Empty<ProcessSnapshot>(), false, ex.Message);
        }

        var snapshots = new List<RunnerSnapshot>(_knownRunners.Count);
        foreach (var known in _knownRunners.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            var path = known.Key;
            if (!discoveredByPath.TryGetValue(path, out var runner))
            {
                snapshots.Add(new RunnerSnapshot(
                    known.Value,
                    RunnerState.ERROR,
                    "Previously discovered runner no longer has the required configured-runner signature."));
                continue;
            }

            var association = RunnerProcessAssociator.Associate(runner, inventory);
            var transition = _transitions.Get(path);
            var state = RunnerStateEvaluator.Evaluate(runner, association, transition, _clock());

            if (transition?.Kind == RunnerTransitionKind.Starting &&
                association.Listener is not null &&
                state is RunnerState.IDLE or RunnerState.BUSY)
            {
                _transitions.Clear(path);
            }
            else if (transition?.Kind == RunnerTransitionKind.Stopping &&
                     association.Listener is null &&
                     state == RunnerState.OFF)
            {
                _transitions.Clear(path);
            }

            snapshots.Add(new RunnerSnapshot(runner, state, ErrorFor(runner, association, transition, state)));
        }

        return snapshots;
    }

    public RunnerControlResult Start(RunnerDescriptor runner) => _control.Start(runner);

    public RunnerControlResult StopIdle(RunnerDescriptor runner) => _control.StopIdle(runner);

    public RunnerControlResult ForceStopBusy(RunnerDescriptor runner, bool confirmed) =>
        _control.ForceStopBusy(runner, confirmed);

    private static string? ErrorFor(
        RunnerDescriptor runner,
        RunnerProcessAssociation association,
        RunnerTransition? transition,
        RunnerState state)
    {
        if (state != RunnerState.ERROR)
        {
            return null;
        }

        if (runner.HasIdentityError) return runner.IdentityError;
        if (!string.IsNullOrWhiteSpace(association.Error)) return association.Error;
        if (!string.IsNullOrWhiteSpace(transition?.Error)) return transition.Error;
        if (transition?.Kind == RunnerTransitionKind.Starting)
        {
            return $"STARTING exceeded the {RunnerTransitionTracker.DefaultTimeout.TotalSeconds:0}-second transition timeout.";
        }
        if (transition?.Kind == RunnerTransitionKind.Stopping)
        {
            return $"STOPPING exceeded the {RunnerTransitionTracker.DefaultTimeout.TotalSeconds:0}-second transition timeout.";
        }
        return "Runner runtime evidence is inconsistent or incomplete.";
    }
}
