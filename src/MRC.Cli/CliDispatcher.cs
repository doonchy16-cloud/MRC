using MRC.Core;
using MRC.Core.Diagnostics;
using MRC.Core.Updating;

namespace MRC.Cli;

public sealed class CliDispatcher
{
    private readonly IGuiLauncher _guiLauncher;
    private readonly Func<DoctorRunOptions, CancellationToken, Task<DoctorReport>> _doctorRunner;
    private readonly Func<IProgress<UpdateProgress>, CancellationToken, Task<UpdateResult>> _updateRunner;
    private readonly Func<EnvironmentFenceResult> _fenceEvaluator;

    public CliDispatcher(IGuiLauncher guiLauncher)
        : this(
            guiLauncher,
            static (options, cancellationToken) => new DoctorService().RunAsync(options, cancellationToken),
            RunDefaultUpdateAsync,
            EnvironmentFence.EvaluateCurrent)
    {
    }

    public CliDispatcher(
        IGuiLauncher guiLauncher,
        Func<DoctorRunOptions, CancellationToken, Task<DoctorReport>> doctorRunner)
        : this(guiLauncher, doctorRunner, RunDefaultUpdateAsync, EnvironmentFence.EvaluateCurrent)
    {
    }

    public CliDispatcher(
        IGuiLauncher guiLauncher,
        Func<DoctorRunOptions, CancellationToken, Task<DoctorReport>> doctorRunner,
        Func<IProgress<UpdateProgress>, CancellationToken, Task<UpdateResult>> updateRunner,
        Func<EnvironmentFenceResult> fenceEvaluator)
    {
        _guiLauncher = guiLauncher ?? throw new ArgumentNullException(nameof(guiLauncher));
        _doctorRunner = doctorRunner ?? throw new ArgumentNullException(nameof(doctorRunner));
        _updateRunner = updateRunner ?? throw new ArgumentNullException(nameof(updateRunner));
        _fenceEvaluator = fenceEvaluator ?? throw new ArgumentNullException(nameof(fenceEvaluator));
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

            if (launch.Outcome == GuiLaunchOutcome.AlreadyRunningActivated)
            {
                await output.WriteLineAsync("● Main Runner Control is already running.");
                await output.WriteLineAsync("✓ Existing window restored and focused.");
            }
            else
            {
                await output.WriteLineAsync("✓ Main Runner Control opened.");
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

    private async Task<int> RunUpdateAsync(TextWriter output, TextWriter error)
    {
        var outputRenderer = new CliRenderer(output);
        var errorRenderer = new CliRenderer(error);
        await outputRenderer.WriteLineAsync("MRC Update", CliTone.Heading);

        var fence = _fenceEvaluator();
        if (!fence.IsAuthorized)
        {
            await errorRenderer.WriteLineAsync($"CONTROL BLOCKED • {fence.Code} • {fence.Message}", CliTone.Error);
            return 1;
        }

        try
        {
            var progress = new ImmediateProgress<UpdateProgress>(updateProgress =>
            {
                var line = CliPresentation.UpdateProgressLine(updateProgress);
                outputRenderer.WriteLineAsync(line.Text, line.Tone).GetAwaiter().GetResult();
            });
            var result = await _updateRunner(progress, CancellationToken.None);
            await outputRenderer.WriteLineAsync(
                result.Message,
                result.Outcome switch
                {
                    UpdateOutcome.Updated => CliTone.Success,
                    UpdateOutcome.UpToDate => CliTone.Success,
                    UpdateOutcome.Failed => CliTone.Error,
                    _ => CliTone.Normal
                });
            return result.Outcome == UpdateOutcome.Failed ? 1 : 0;
        }
        catch (Exception ex)
        {
            await errorRenderer.WriteLineAsync($"Update failed safely: {ex.Message}", CliTone.Error);
            return 1;
        }
    }

    private static async Task<UpdateResult> RunDefaultUpdateAsync(IProgress<UpdateProgress> progress, CancellationToken cancellationToken)
    {
        var installRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MRC");
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        var source = new GitHubReleaseSource(client);
        var updater = new UpdateService(source, installRoot, new ExecutableVersionVerifier(), progress);
        return await updater.RunAsync(cancellationToken);
    }

    private static async Task WriteVersionAsync(TextWriter output)
    {
        var installLocation = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var renderer = new CliRenderer(output);
        foreach (var line in CliPresentation.VersionLines(installLocation)) await renderer.WriteLineAsync(line.Text, line.Tone);
    }

    private static async Task WriteDoctorAsync(TextWriter output, DoctorReport report)
    {
        var renderer = new CliRenderer(output);
        foreach (var line in CliPresentation.DoctorLines(report)) await renderer.WriteLineAsync(line.Text, line.Tone);
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

    private sealed class ImmediateProgress<T> : IProgress<T>
    {
        private readonly Action<T> _handler;
        public ImmediateProgress(Action<T> handler) => _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        public void Report(T value) => _handler(value);
    }
}
