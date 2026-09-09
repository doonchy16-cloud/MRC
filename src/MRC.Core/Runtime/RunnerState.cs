using MRC.Core.Runners;

namespace MRC.Core.Runtime;

public enum RunnerState
{
    OFF = 0,
    STARTING = 1,
    IDLE = 2,
    BUSY = 3,
    STOPPING = 4,
    ERROR = 5
}

public sealed record RunnerSnapshot(
    RunnerDescriptor Runner,
    RunnerState State,
    string? Error);
