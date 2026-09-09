using MRC.Core;
using MRC.Core.Diagnostics;
using MRC.Core.Updating;

namespace MRC.Cli;

public sealed class CliDispatcher
{
    private readonly IGuiLauncher _guiLauncher;
    private readonly Func<DoctorRunOptions, CancellationToken, Task<DoctorReport>> _doctorRunner;

    public CliDispatcher(IGuiLauncher guiLauncher)
        : this(guiLauncher, static (options, cancellationToken) =>
            new DoctorService().RunAsync(options, cancellationToken))
    {
    }

    public CliDispatcher(
        IGuiLauncher guiLauncher,
        Func<DoctorRunOptions, CancellationToken, Task<DoctorReport>> doctorRunner)
    {
        _guiLauncher = guiLauncher ?? throw new ArgumentNullException(nameof(guiLauncher));
        _doctorRunner = doctorRunner ?? throw new ArgumentNullException(nameof(doctorRunner));
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

            await output.WriteLineAsync("✓ Main Runner Control opened.");
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
                var report = await _doctorRunner(
                    new DoctorRunOptions(ApplyAutomaticRepairs: true),
                    CancellationToken.None);
                await WriteDoctorAsync(output, report);
                return report.ExitCode;
            }

            case "-diagnose":
            case "--diagnose":
                return await RunDiagnoseAsync(output, error);

            case "-update":
            case "--update":
                return await RunUpdateAsync(output, error);

            default:
                await WriteUnknownAsync(error, args[0]);
                return 2;
        }
    }

    private static async Task<int> RunDiagnoseAsync(TextWriter output, TextWriter error)
    {
        try
        {
            var report = new DiagnoseService().Run();
            var renderer = new CliRenderer(output);
            foreach (var line in CliPresentation.DiagnoseLines(report))
            {
                await renderer.WriteLineAsync(line.Text, line.Tone);
            }

            return 0;
        }
        catch (Exception ex)
        {
            var renderer = new CliRenderer(error);
            await renderer.WriteLineAsync($"Diagnose failed safely: {ex.Message}", CliTone.Error);
            return 1;
        }
    }

    private static async Task<int> RunUpdateAsync(TextWriter output, TextWriter error)
    {
        await output.WriteLineAsync("MRC Update");
        var fence = EnvironmentFence.EvaluateCurrent();
        if (!fence.IsAuthorized)
        {
            await error.WriteLineAsync($"CONTROL BLOCKED • {fence.Code} • {fence.Message}");
            return 1;
        }

        var installRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MRC");

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            var source = new GitHubReleaseSource(client);
            var updater = new UpdateService(source, installRoot, new ExecutableVersionVerifier());
            var result = await updater.RunAsync();
            await output.WriteLineAsync(result.Message);
            return result.Outcome == UpdateOutcome.Failed ? 1 : 0;
        }
        catch (Exception ex)
        {
            await error.WriteLineAsync($"Update failed safely: {ex.Message}");
            return 1;
        }
    }

    private static async Task WriteVersionAsync(TextWriter output)
    {
        var installLocation = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var renderer = new CliRenderer(output);
        foreach (var line in CliPresentation.VersionLines(installLocation))
        {
            await renderer.WriteLineAsync(line.Text, line.Tone);
        }
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
        await output.WriteLineAsync("  MRC -doctor | --doctor     Find issues and apply verified automatic low-risk repairs");
        await output.WriteLineAsync("  MRC -diagnose | --diagnose Run deep read-only runner/process diagnostics");
        await output.WriteLineAsync("  MRC -update | --update     Resolve, verify, and atomically activate an allowed release");
    }

    private static async Task WriteUnknownAsync(TextWriter error, string option)
    {
        await error.WriteLineAsync($"Unknown option: {option}");
        await error.WriteLineAsync("Run 'MRC -help' for the supported command surface.");
    }
}
