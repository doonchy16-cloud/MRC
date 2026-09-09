namespace MRC.Core.Runners;

internal static class RunnerPath
{
    public static string Normalize(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        return Path.GetFullPath(path.Trim())
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    public static bool EqualsWindows(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        return string.Equals(Normalize(left), Normalize(right), StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsImmediateChildOf(string root, string candidate)
    {
        try
        {
            var normalizedRoot = Normalize(root);
            var normalizedCandidate = Normalize(candidate);
            var parent = Directory.GetParent(normalizedCandidate)?.FullName;
            return parent is not null && EqualsWindows(normalizedRoot, parent);
        }
        catch
        {
            return false;
        }
    }

    public static bool HasSignature(string runnerDirectory) =>
        File.Exists(Path.Combine(runnerDirectory, ".runner")) &&
        File.Exists(RunCommand(runnerDirectory)) &&
        File.Exists(Path.Combine(runnerDirectory, "run-helper.cmd.template")) &&
        File.Exists(ListenerExecutable(runnerDirectory));

    public static string RunCommand(string runnerDirectory) =>
        Path.Combine(runnerDirectory, "run.cmd");

    public static string ListenerExecutable(string runnerDirectory) =>
        Path.Combine(runnerDirectory, "bin", "Runner.Listener.exe");

    public static string WorkerExecutable(string runnerDirectory) =>
        Path.Combine(runnerDirectory, "bin", "Runner.Worker.exe");
}
