using System.Diagnostics;
using MRC.Core.Runners;

namespace MRC.Core.Control;

internal sealed class WindowsRunnerLauncher : IRunnerLauncher
{
    public void Launch(RunnerDescriptor runner)
    {
        var startInfo = BuildStartInfo(runner);
        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not launch {RunnerPath.RunCommand(runner.DirectoryPath)}.");
    }

    internal static ProcessStartInfo BuildStartInfo(RunnerDescriptor runner)
    {
        var runCommand = RunnerPath.RunCommand(runner.DirectoryPath);
        var startInfo = new ProcessStartInfo("cmd.exe")
        {
            WorkingDirectory = runner.DirectoryPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };
        startInfo.ArgumentList.Add("/d");
        startInfo.ArgumentList.Add("/s");
        startInfo.ArgumentList.Add("/c");
        startInfo.ArgumentList.Add($"\"{runCommand}\"");
        return startInfo;
    }
}
