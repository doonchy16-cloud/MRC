using MRC.Core;
using MRC.Core.Diagnostics;

namespace MRC.Cli;

public sealed class CliDispatcher
{
    private readonly IGuiLauncher _guiLauncher;

    public CliDispatcher(IGuiLauncher guiLauncher)
    {
        _guiLauncher = guiLauncher;
    }

    public async Task<int> ExecuteAsync(string[] args, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        if (args.Length == 0)
        {
            var launch = _guiLauncher.Launch();
            if (!launch.Success)
            {
                await error.WriteLineAsync(launch.Message);
                return 3;
            }

            return 0;
        }

        if (args.Length != 1)
        {
            await WriteUnknownAsync(error, string.Join(' ', args));
            return 2;
        }

        var option = args[0].Trim().ToLowerInvariant();
        switch (option)
        {
            case "-v":
            case "-version":
            case "--version":
                await WriteVersionAsync(output);
                return 0;

            case "-h":
            case "-help":
            case "--help":
                await WriteHelpAsync(output);
                return 0;

            case "-doctor":
            case "--doctor":
            {
                var report = await new DoctorService().RunAsync();
                await WriteDoctorAsync(output, report);
                return report.ExitCode;
            }

            case "-update":
            case "--update":
                await error.WriteLineAsync("MRC update is reserved for PASS 4 — Operations + Updating.");
                return 4;

            default:
                await WriteUnknownAsync(error, args[0]);
                return 2;
        }
    }

    private static async Task WriteVersionAsync(TextWriter output)
    {
        var installLocation = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        await output.WriteLineAsync(MrcConstants.ProductName);
        await output.WriteLineAsync($"Version: {BuildInfo.Version}");
        await output.WriteLineAsync($"Channel: {MrcConstants.ReleaseChannel}");
        await output.WriteLineAsync($"Install location: {installLocation}");
        await output.WriteLineAsync($"Runner root: {MrcConstants.RunnerRoot}");
    }

    private static async Task WriteDoctorAsync(TextWriter output, DoctorReport report)
    {
        await output.WriteLineAsync("MRC Doctor");
        await output.WriteLineAsync($"Version: {BuildInfo.Version} • Channel: {MrcConstants.ReleaseChannel}");
        await output.WriteLineAsync();
        foreach (var check in report.Checks)
        {
            var status = check.Status switch
            {
                DoctorCheckStatus.Pass => "PASS",
                DoctorCheckStatus.Warning => "WARN",
                DoctorCheckStatus.Fail => "FAIL",
                _ => "UNKNOWN"
            };
            await output.WriteLineAsync($"{check.Name}: [{status}] {check.Message}");
        }
    }

    private static async Task WriteHelpAsync(TextWriter output)
    {
        await output.WriteLineAsync("Main Runner Control (MRC)");
        await output.WriteLineAsync();
        await output.WriteLineAsync("Usage:");
        await output.WriteLineAsync("  MRC                         Open the GUI");
        await output.WriteLineAsync("  MRC -v | -version | --version");
        await output.WriteLineAsync("                              Show installed version information");
        await output.WriteLineAsync("  MRC -h | -help | --help    Show this help");
        await output.WriteLineAsync("  MRC -doctor | --doctor     Run read-only diagnostics");
        await output.WriteLineAsync("  MRC -update | --update     Update MRC (implemented in PASS 4)");
    }

    private static async Task WriteUnknownAsync(TextWriter error, string option)
    {
        await error.WriteLineAsync($"Unknown option: {option}");
        await error.WriteLineAsync("Run 'MRC -help' for the supported command surface.");
    }
}
