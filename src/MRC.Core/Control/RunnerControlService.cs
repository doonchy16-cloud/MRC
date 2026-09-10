using MRC.Core.Runners;
using MRC.Core.Runtime;

namespace MRC.Core.Control;

internal sealed class RunnerControlService
{
    private readonly string _authorizedRoot;
    private readonly IProcessSnapshotProvider _processProvider;
    private readonly IRunnerLauncher _launcher;
    private readonly IRunnerProcessTerminator _terminator;
    private readonly IRunnerForceProcessTerminator? _forceTerminator;
    private readonly RunnerTransitionTracker _transitions;
    private readonly Func<DateTimeOffset> _clock;

    public RunnerControlService(
        string authorizedRoot,
        IProcessSnapshotProvider processProvider,
        IRunnerLauncher launcher,
        IRunnerProcessTerminator terminator,
        RunnerTransitionTracker transitions,
        Func<DateTimeOffset> clock)
        : this(authorizedRoot, processProvider, launcher, terminator, null, transitions, clock)
    {
    }

    public RunnerControlService(
        string authorizedRoot,
        IProcessSnapshotProvider processProvider,
        IRunnerLauncher launcher,
        IRunnerProcessTerminator terminator,
        IRunnerForceProcessTerminator? forceTerminator,
        RunnerTransitionTracker transitions,
        Func<DateTimeOffset> clock)
    {
        _authorizedRoot = RunnerPath.Normalize(authorizedRoot);
        _processProvider = processProvider;
        _launcher = launcher;
        _terminator = terminator;
        _forceTerminator = forceTerminator;
        _transitions = transitions;
        _clock = clock;
    }

    public RunnerControlResult Start(RunnerDescriptor runner)
    {
        var validationError = ValidateRunner(runner);
        if (validationError is not null)
        {
            return Error(runner, validationError);
        }

        var transition = _transitions.Get(runner.DirectoryPath);
        if (transition is { Kind: RunnerTransitionKind.Starting or RunnerTransitionKind.Stopping })
        {
            return new RunnerControlResult(
                RunnerControlOutcome.NotOff,
                TransitionState(transition),
                "Runner already has a control transition in progress.");
        }
        if (transition?.Kind == RunnerTransitionKind.Error)
        {
            _transitions.Clear(runner.DirectoryPath);
        }

        var observation = Observe(runner);
        if (observation.State == RunnerState.ERROR)
        {
            return Error(runner, observation.Error ?? "Runner state could not be inspected safely.");
        }
        if (observation.State != RunnerState.OFF)
        {
            return new RunnerControlResult(RunnerControlOutcome.NotOff, observation.State, "Runner start is allowed only from OFF.");
        }

        try
        {
            _launcher.Launch(runner);
            _transitions.MarkStarting(runner.DirectoryPath, _clock());
            return new RunnerControlResult(RunnerControlOutcome.Starting, RunnerState.STARTING, "Runner launch requested through run.cmd.");
        }
        catch (Exception ex)
        {
            _transitions.MarkError(runner.DirectoryPath, _clock(), ex.Message);
            return Error(runner, $"Runner launch failed: {ex.Message}");
        }
    }

    public RunnerControlResult StopIdle(RunnerDescriptor runner)
    {
        var validationError = ValidateRunner(runner);
        if (validationError is not null)
        {
            return Error(runner, validationError);
        }

        var transition = _transitions.Get(runner.DirectoryPath);
        if (transition is { Kind: RunnerTransitionKind.Starting or RunnerTransitionKind.Stopping })
        {
            return new RunnerControlResult(
                RunnerControlOutcome.NotIdle,
                TransitionState(transition),
                "Runner already has a control transition in progress.");
        }
        if (transition?.Kind == RunnerTransitionKind.Error)
        {
            _transitions.Clear(runner.DirectoryPath);
        }

        var observation = Observe(runner);
        if (observation.State == RunnerState.ERROR)
        {
            return Error(runner, observation.Error ?? "Runner state could not be inspected safely.");
        }
        if (observation.State == RunnerState.BUSY)
        {
            return new RunnerControlResult(RunnerControlOutcome.BusyProtected, RunnerState.BUSY, "BUSY runner protected: normal OFF is blocked.");
        }
        if (observation.State != RunnerState.IDLE || observation.Association.Listener is null)
        {
            return new RunnerControlResult(RunnerControlOutcome.NotIdle, observation.State, "Normal OFF is allowed only for verified IDLE runners.");
        }

        _transitions.MarkStopping(runner.DirectoryPath, _clock());
        RunnerTerminationResult termination;
        try
        {
            termination = _terminator.Terminate(runner, observation.Association.Listener);
        }
        catch (Exception ex)
        {
            _transitions.MarkError(runner.DirectoryPath, _clock(), ex.Message);
            return Error(runner, $"Runner stop failed: {ex.Message}");
        }

        switch (termination.Outcome)
        {
            case RunnerTerminationOutcome.Terminated:
                _transitions.Clear(runner.DirectoryPath);
                return new RunnerControlResult(RunnerControlOutcome.Stopped, RunnerState.OFF, termination.Message);
            case RunnerTerminationOutcome.BusyNow:
                _transitions.Clear(runner.DirectoryPath);
                return new RunnerControlResult(RunnerControlOutcome.BusyProtected, RunnerState.BUSY, termination.Message);
            case RunnerTerminationOutcome.AlreadyOff:
                _transitions.Clear(runner.DirectoryPath);
                return new RunnerControlResult(RunnerControlOutcome.AlreadyOff, RunnerState.OFF, termination.Message);
            default:
                _transitions.MarkError(runner.DirectoryPath, _clock(), termination.Message);
                return Error(runner, termination.Message);
        }
    }

    public RunnerControlResult ForceStopBusy(RunnerDescriptor runner, bool confirmed)
    {
        var validationError = ValidateRunner(runner);
        if (validationError is not null)
        {
            return Error(runner, validationError);
        }

        var transition = _transitions.Get(runner.DirectoryPath);
        if (transition is { Kind: RunnerTransitionKind.Starting or RunnerTransitionKind.Stopping })
        {
            return new RunnerControlResult(
                RunnerControlOutcome.NotBusy,
                TransitionState(transition),
                "Runner already has a control transition in progress.");
        }
        if (transition?.Kind == RunnerTransitionKind.Error)
        {
            _transitions.Clear(runner.DirectoryPath);
        }

        var observation = Observe(runner);
        if (observation.State == RunnerState.ERROR)
        {
            return Error(runner, observation.Error ?? "Runner state could not be inspected safely.");
        }
        if (observation.State != RunnerState.BUSY || observation.Association.Listener is null)
        {
            return new RunnerControlResult(
                RunnerControlOutcome.NotBusy,
                observation.State,
                "Force-stop is allowed only for a verified BUSY runner.");
        }
        if (!confirmed)
        {
            return new RunnerControlResult(
                RunnerControlOutcome.ConfirmationRequired,
                RunnerState.BUSY,
                "BUSY force-stop requires explicit confirmation because it can interrupt an active GitHub Actions job.");
        }
        if (_forceTerminator is null)
        {
            return Error(runner, "BUSY force-stop is unavailable because no force-termination authority is configured.");
        }

        _transitions.MarkStopping(runner.DirectoryPath, _clock());
        RunnerTerminationResult termination;
        try
        {
            termination = _forceTerminator.TerminateForce(runner, observation.Association.Listener);
        }
        catch (Exception ex)
        {
            _transitions.MarkError(runner.DirectoryPath, _clock(), ex.Message);
            return Error(runner, $"Runner force-stop failed: {ex.Message}");
        }

        switch (termination.Outcome)
        {
            case RunnerTerminationOutcome.Terminated:
                return new RunnerControlResult(RunnerControlOutcome.ForceStopping, RunnerState.STOPPING, termination.Message);
            case RunnerTerminationOutcome.AlreadyOff:
                _transitions.Clear(runner.DirectoryPath);
                return new RunnerControlResult(RunnerControlOutcome.AlreadyOff, RunnerState.OFF, termination.Message);
            default:
                _transitions.MarkError(runner.DirectoryPath, _clock(), termination.Message);
                return Error(runner, termination.Message);
        }
    }

    private Observation Observe(RunnerDescriptor runner)
    {
        try
        {
            var association = RunnerProcessAssociator.Associate(runner, _processProvider.Capture());
            var state = RunnerStateEvaluator.Evaluate(runner, association, null, _clock());
            return new Observation(state, association, association.Error);
        }
        catch (Exception ex)
        {
            var association = new RunnerProcessAssociation(null, Array.Empty<ProcessSnapshot>(), ex.Message);
            return new Observation(RunnerState.ERROR, association, ex.Message);
        }
    }

    private string? ValidateRunner(RunnerDescriptor runner)
    {
        if (!RunnerPath.IsImmediateChildOf(_authorizedRoot, runner.DirectoryPath))
        {
            return "Runner directory is outside the authorized immediate-child root boundary.";
        }

        if (!RunnerPath.HasSignature(runner.DirectoryPath))
        {
            return "Runner signature is no longer valid.";
        }

        var currentIdentity = RunnerIdentityParser.Parse(runner.DirectoryPath);
        if (currentIdentity.HasIdentityError)
        {
            return currentIdentity.IdentityError;
        }

        return null;
    }

    private RunnerControlResult Error(RunnerDescriptor runner, string message)
    {
        _transitions.MarkError(runner.DirectoryPath, _clock(), message);
        return new RunnerControlResult(RunnerControlOutcome.Error, RunnerState.ERROR, message);
    }

    private static RunnerState TransitionState(RunnerTransition transition) =>
        transition.Kind == RunnerTransitionKind.Starting
            ? RunnerState.STARTING
            : RunnerState.STOPPING;

    private sealed record Observation(
        RunnerState State,
        RunnerProcessAssociation Association,
        string? Error);
}
