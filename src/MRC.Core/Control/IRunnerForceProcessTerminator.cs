using MRC.Core.Runners;
using MRC.Core.Runtime;

namespace MRC.Core.Control;

internal interface IRunnerForceProcessTerminator
{
    RunnerTerminationResult TerminateForce(RunnerDescriptor runner, ProcessSnapshot listener);
}
