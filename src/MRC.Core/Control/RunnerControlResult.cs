using MRC.Core.Runtime;

namespace MRC.Core.Control;

public enum RunnerControlOutcome
{
    Starting,
    Stopping,
    Stopped,
    ForceStopping,
    ConfirmationRequired,
    BusyProtected,
    AlreadyOff,
    NotOff,
    NotIdle,
    NotBusy,
    Error
}

public sealed record RunnerControlResult(
    RunnerControlOutcome Outcome,
    RunnerState State,
    string Message);
