namespace MRC.Core;

public sealed record EnvironmentFenceResult(
    bool IsAuthorized,
    string MachineName,
    string RunnerRoot,
    string Code,
    string Message);

public static class EnvironmentFence
{
    public static EnvironmentFenceResult Evaluate(string machineName, string runnerRoot, bool runnerRootExists)
    {
        var observedMachine = machineName?.Trim() ?? string.Empty;
        var observedRoot = NormalizeRoot(runnerRoot);
        var authorizedRoot = NormalizeRoot(MrcConstants.RunnerRoot);

        if (!string.Equals(observedMachine, MrcConstants.TargetMachineName, StringComparison.OrdinalIgnoreCase))
        {
            return Blocked(observedMachine, observedRoot, "WRONG_MACHINE",
                $"MRC is authorized only on {MrcConstants.TargetMachineName}.");
        }

        if (!string.Equals(observedRoot, authorizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            return Blocked(observedMachine, observedRoot, "RUNNER_ROOT_MISMATCH",
                $"Runner root must be exactly {MrcConstants.RunnerRoot}.");
        }

        if (!runnerRootExists)
        {
            return Blocked(observedMachine, observedRoot, "RUNNER_ROOT_MISSING",
                $"Authorized runner root does not exist: {MrcConstants.RunnerRoot}");
        }

        return new EnvironmentFenceResult(
            true,
            observedMachine,
            authorizedRoot,
            "AUTHORIZED",
            "Main-PC identity and exact runner-root evidence agree.");
    }

    public static EnvironmentFenceResult EvaluateCurrent() =>
        Evaluate(Environment.MachineName, MrcConstants.RunnerRoot, Directory.Exists(MrcConstants.RunnerRoot));

    private static EnvironmentFenceResult Blocked(string machineName, string runnerRoot, string code, string message) =>
        new(false, machineName, runnerRoot, code, message);

    private static string NormalizeRoot(string? path) =>
        string.IsNullOrWhiteSpace(path)
            ? string.Empty
            : path.Trim().TrimEnd('\\', '/').Replace('/', '\\');
}
