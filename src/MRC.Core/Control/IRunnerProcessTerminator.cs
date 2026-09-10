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

internal enum RunnerGracefulShutdownOutcome
{
    Exited,
    TimedOut,
    Unavailable,
    Failed
}

internal sealed record RunnerGracefulShutdownResult(
    RunnerGracefulShutdownOutcome Outcome,
    string Message);

internal interface IRunnerProcessTerminator
{
    RunnerTerminationResult Terminate(RunnerDescriptor runner, ProcessSnapshot listener);
}

internal interface IRunnerGracefulShutdown
{
    RunnerGracefulShutdownResult TryShutdown(ProcessSnapshot listener, TimeSpan timeout);
}

internal interface IRunnerExitVerifier
{
    bool WaitUntilOff(RunnerDescriptor runner, TimeSpan timeout, out string? error);
}

internal interface IProcessPathReader
{
    bool TryGetPath(int processId, out string? path, out string? error);
}

internal interface IProcessTreeKiller
{
    bool TryKillTree(int processId, out string? error);
}
