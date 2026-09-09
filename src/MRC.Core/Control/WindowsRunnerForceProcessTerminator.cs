using MRC.Core.Runners;
using MRC.Core.Runtime;

namespace MRC.Core.Control;

internal sealed class WindowsRunnerForceProcessTerminator : IRunnerForceProcessTerminator
{
    private readonly IProcessSnapshotProvider _processProvider;
    private readonly IProcessPathReader _pathReader;
    private readonly IProcessTreeKiller _killer;

    public WindowsRunnerForceProcessTerminator(
        IProcessSnapshotProvider processProvider,
        IProcessPathReader pathReader,
        IProcessTreeKiller killer)
    {
        _processProvider = processProvider;
        _pathReader = pathReader;
        _killer = killer;
    }

    public RunnerTerminationResult TerminateForce(RunnerDescriptor runner, ProcessSnapshot listener)
    {
        var finalInventory = _processProvider.Capture();
        var finalAssociation = RunnerProcessAssociator.Associate(runner, finalInventory);
        if (!string.IsNullOrWhiteSpace(finalAssociation.Error))
        {
            return new RunnerTerminationResult(RunnerTerminationOutcome.InspectionError, finalAssociation.Error);
        }

        if (finalAssociation.Listener is null)
        {
            return new RunnerTerminationResult(RunnerTerminationOutcome.AlreadyOff, "Listener exited before force termination was required.");
        }

        if (finalAssociation.Listener.ProcessId != listener.ProcessId)
        {
            return new RunnerTerminationResult(
                RunnerTerminationOutcome.OwnershipMismatch,
                "Listener PID changed between the BUSY confirmation and the final ownership check.");
        }

        if (!_pathReader.TryGetPath(listener.ProcessId, out var currentPath, out var pathError))
        {
            return new RunnerTerminationResult(
                RunnerTerminationOutcome.InspectionError,
                $"Listener path could not be re-verified immediately before force termination: {pathError}");
        }

        var expectedPath = RunnerPath.ListenerExecutable(runner.DirectoryPath);
        if (currentPath is null || !RunnerPath.EqualsWindows(currentPath, expectedPath))
        {
            return new RunnerTerminationResult(
                RunnerTerminationOutcome.OwnershipMismatch,
                "Listener executable path changed before force termination; no process was killed.");
        }

        if (!_killer.TryKillTree(listener.ProcessId, out var killError))
        {
            return new RunnerTerminationResult(
                RunnerTerminationOutcome.KillFailed,
                $"Force process-tree termination failed: {killError}");
        }

        return new RunnerTerminationResult(
            RunnerTerminationOutcome.Terminated,
            "Verified BUSY listener tree force-termination requested after explicit confirmation.");
    }
}
