using MRC.Core.Runtime;

namespace MRC.Core.Control;

public enum RunnerControlOutcome
{
    Starting,
    Stopping,
    BusyProtected,
    AlreadyOff,
    NotOff,
    NotIdle,
    Error
}

public sealed record RunnerControlResult(
    RunnerControlOutcome Outcome,
    RunnerState State,
    string Message);
