using MRC.Core.Runners;
using MRC.Core.Runtime;

namespace MRC.Core.Control;

internal sealed class WindowsRunnerProcessTerminator : IRunnerProcessTerminator
{
    private static readonly TimeSpan GracefulShutdownTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan FallbackExitTimeout = TimeSpan.FromSeconds(5);

    private readonly IProcessSnapshotProvider _processProvider;
    private readonly IProcessPathReader _pathReader;
    private readonly IProcessTreeKiller _killer;
    private readonly IRunnerGracefulShutdown _gracefulShutdown;
    private readonly IRunnerExitVerifier _exitVerifier;

    public WindowsRunnerProcessTerminator(
        IProcessSnapshotProvider processProvider,
        IProcessPathReader pathReader,
        IProcessTreeKiller killer)
        : this(
            processProvider,
            pathReader,
            killer,
            new WindowsRunnerGracefulShutdown(),
            new RunnerExitVerifier(processProvider))
    {
    }

    internal WindowsRunnerProcessTerminator(
        IProcessSnapshotProvider processProvider,
        IProcessPathReader pathReader,
        IProcessTreeKiller killer,
        IRunnerGracefulShutdown gracefulShutdown,
        IRunnerExitVerifier exitVerifier)
    {
        _processProvider = processProvider ?? throw new ArgumentNullException(nameof(processProvider));
        _pathReader = pathReader ?? throw new ArgumentNullException(nameof(pathReader));
        _killer = killer ?? throw new ArgumentNullException(nameof(killer));
        _gracefulShutdown = gracefulShutdown ?? throw new ArgumentNullException(nameof(gracefulShutdown));
        _exitVerifier = exitVerifier ?? throw new ArgumentNullException(nameof(exitVerifier));
    }

    public RunnerTerminationResult Terminate(RunnerDescriptor runner, ProcessSnapshot listener)
    {
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(listener);

        var preflight = RecheckExactAssociation(runner, listener.ProcessId);
        if (preflight.Result is not null)
        {
            return preflight.Result;
        }

        var currentListener = preflight.Association!.Listener!;
        var ownershipError = VerifyCurrentListenerPath(runner, currentListener.ProcessId);
        if (ownershipError is not null)
        {
            return ownershipError;
        }

        var graceful = _gracefulShutdown.TryShutdown(currentListener, GracefulShutdownTimeout);
        if (graceful.Outcome == RunnerGracefulShutdownOutcome.Exited)
        {
            if (_exitVerifier.WaitUntilOff(runner, FallbackExitTimeout, out var verifyError))
            {
                return new RunnerTerminationResult(
                    RunnerTerminationOutcome.Terminated,
                    "Graceful runner shutdown completed and exact runner processes exited.");
            }

            // The listener we signaled exited but the runner wrapper may have relaunched it.
            // Re-enter the exact ownership/BUSY fence before any hard fallback.
            var afterGraceful = RecheckExactAssociation(runner, currentListener.ProcessId, allowChangedListener: true);
            if (afterGraceful.Result is not null && afterGraceful.Result.Outcome != RunnerTerminationOutcome.OwnershipMismatch)
            {
                return afterGraceful.Result;
            }

            if (afterGraceful.Association?.Listener is null)
            {
                return new RunnerTerminationResult(
                    RunnerTerminationOutcome.InspectionError,
                    $"Listener exited, but shutdown could not be verified safely: {verifyError}");
            }
        }

        // Graceful shutdown was unavailable, failed, timed out, or did not leave the
        // runner fully OFF. Before fallback termination, take a fresh process snapshot
        // so a newly BUSY runner is never killed by stale IDLE evidence.
        var fallback = RecheckExactAssociation(runner, currentListener.ProcessId, allowChangedListener: false);
        if (fallback.Result is not null)
        {
            return fallback.Result;
        }

        var fallbackListener = fallback.Association!.Listener!;
        ownershipError = VerifyCurrentListenerPath(runner, fallbackListener.ProcessId);
        if (ownershipError is not null)
        {
            return ownershipError;
        }

        if (!_killer.TryKillTree(fallbackListener.ProcessId, out var killError))
        {
            return new RunnerTerminationResult(
                RunnerTerminationOutcome.KillFailed,
                $"Process-tree fallback termination failed after graceful shutdown did not complete: {killError}");
        }

        if (!_exitVerifier.WaitUntilOff(runner, FallbackExitTimeout, out var exitError))
        {
            return new RunnerTerminationResult(
                RunnerTerminationOutcome.KillFailed,
                $"Fallback termination was requested, but exact runner exit was not verified: {exitError}");
        }

        return new RunnerTerminationResult(
            RunnerTerminationOutcome.Terminated,
            $"Graceful shutdown did not complete ({graceful.Outcome}); verified hard-stop fallback completed and exact runner processes exited.");
    }

    private (RunnerProcessAssociation? Association, RunnerTerminationResult? Result) RecheckExactAssociation(
        RunnerDescriptor runner,
        int expectedListenerProcessId,
        bool allowChangedListener = false)
    {
        ProcessInventory inventory;
        try
        {
            inventory = _processProvider.Capture();
        }
        catch (Exception ex)
        {
            return (null, new RunnerTerminationResult(
                RunnerTerminationOutcome.InspectionError,
                $"Process inventory could not be refreshed before shutdown: {ex.Message}"));
        }

        if (!inventory.IsComplete)
        {
            return (null, new RunnerTerminationResult(
                RunnerTerminationOutcome.InspectionError,
                inventory.Error ?? "Process inventory is incomplete."));
        }

        var association = RunnerProcessAssociator.AssociateExact(runner, inventory);
        if (!string.IsNullOrWhiteSpace(association.Error))
        {
            return (association, new RunnerTerminationResult(
                RunnerTerminationOutcome.InspectionError,
                association.Error));
        }

        if (association.Listener is null)
        {
            return (association, new RunnerTerminationResult(
                RunnerTerminationOutcome.AlreadyOff,
                "Exact managed listener exited before further termination was required."));
        }

        if (!allowChangedListener && association.Listener.ProcessId != expectedListenerProcessId)
        {
            return (association, new RunnerTerminationResult(
                RunnerTerminationOutcome.OwnershipMismatch,
                "Listener PID changed between the IDLE check and the fresh ownership check."));
        }

        if (association.Workers.Count > 0)
        {
            return (association, new RunnerTerminationResult(
                RunnerTerminationOutcome.BusyNow,
                "Runner became BUSY after the initial IDLE check; normal stop was blocked."));
        }

        return (association, null);
    }

    private RunnerTerminationResult? VerifyCurrentListenerPath(RunnerDescriptor runner, int processId)
    {
        if (!_pathReader.TryGetPath(processId, out var currentPath, out var pathError))
        {
            return new RunnerTerminationResult(
                RunnerTerminationOutcome.InspectionError,
                $"Listener path could not be re-verified immediately before shutdown: {pathError}");
        }

        var expectedPath = RunnerPath.ListenerExecutable(runner.DirectoryPath);
        if (currentPath is null || !RunnerPath.EqualsWindows(currentPath, expectedPath))
        {
            return new RunnerTerminationResult(
                RunnerTerminationOutcome.OwnershipMismatch,
                "Listener executable path changed before shutdown; no termination signal was sent.");
        }

        return null;
    }
}
