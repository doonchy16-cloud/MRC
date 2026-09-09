using MRC.Core.Runners;

namespace MRC.Core.Control;

internal interface IRunnerLauncher
{
    void Launch(RunnerDescriptor runner);
}
