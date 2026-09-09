using MRC.Core.Runners;
using MRC.Core.Runtime;

namespace MRC.Core.Control;

internal enum RunnerTerminationOutcome
{
    Terminated,
    BusyNow,
    AlreadyOff,
    OwnershipMismatch,
    InspectionError,
    KillFailed
}

internal sealed record RunnerTerminationResult(
    RunnerTerminationOutcome Outcome,
    string Message);

internal interface IRunnerProcessTerminator
{
    RunnerTerminationResult Terminate(RunnerDescriptor runner, ProcessSnapshot listener);
}

internal interface IProcessPathReader
{
    bool TryGetPath(int processId, out string? path, out string? error);
}

internal interface IProcessTreeKiller
{
    bool TryKillTree(int processId, out string? error);
}
