using MRC.Core.Runners;

namespace MRC.Core.Runtime;

internal static class RunnerStateEvaluator
{
    public static RunnerState Evaluate(
        RunnerDescriptor runner,
        RunnerProcessAssociation association,
        RunnerTransition? transition,
        DateTimeOffset now)
    {
        if (runner.HasIdentityError || !string.IsNullOrWhiteSpace(association.Error))
        {
            return RunnerState.ERROR;
        }

        if (transition is not null)
        {
            if (transition.Kind == RunnerTransitionKind.Error)
            {
                return RunnerState.ERROR;
            }

            if (transition.Kind == RunnerTransitionKind.Starting)
            {
                if (association.Listener is not null)
                {
                    return BaseState(association);
                }

                return now >= transition.ExpiresAt ? RunnerState.ERROR : RunnerState.STARTING;
            }

            if (transition.Kind == RunnerTransitionKind.Stopping)
            {
                if (association.Listener is null)
                {
                    return RunnerState.OFF;
                }

                return now >= transition.ExpiresAt ? RunnerState.ERROR : RunnerState.STOPPING;
            }
        }

        return BaseState(association);
    }

    private static RunnerState BaseState(RunnerProcessAssociation association)
    {
        if (association.Listener is null)
        {
            return RunnerState.OFF;
        }

        return association.Workers.Count > 0 ? RunnerState.BUSY : RunnerState.IDLE;
    }
}
