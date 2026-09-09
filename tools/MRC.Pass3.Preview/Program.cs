using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MRC.Core.Runners;
using MRC.Core.Runtime;
using MRC.Gui;

namespace MRC.Pass3.Preview;

internal static class Program
{
    private const int DefaultPreviewWidth = 1180;
    private const int DefaultPreviewHeight = 760;
    private const int MinimumPreviewWidth = 900;
    private const int MinimumPreviewHeight = 560;

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            var previewWidth = args.Length > 1 ? ParseDimension(args[1], "width") : DefaultPreviewWidth;
            var previewHeight = args.Length > 2 ? ParseDimension(args[2], "height") : DefaultPreviewHeight;
            if (previewWidth < MinimumPreviewWidth || previewHeight < MinimumPreviewHeight)
            {
                throw new ArgumentOutOfRangeException(nameof(args),
                    $"PASS 3 preview geometry must be at least {MinimumPreviewWidth}x{MinimumPreviewHeight}.");
            }

            var outputPath = Path.GetFullPath(args.Length > 0
                ? args[0]
                : Path.Combine("artifacts", "pass3", $"MRC-PASS3-{previewWidth}x{previewHeight}.png"));
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            var window = new MainWindow
            {
                Width = previewWidth,
                Height = previewHeight,
                MinWidth = previewWidth,
                MinHeight = previewHeight,
                MaxWidth = previewWidth,
                MaxHeight = previewHeight,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.None,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = -20000,
                Top = -20000
            };

            window.ConfigurePreview(BuildPreviewReport());
            window.Show();
            window.Measure(new Size(previewWidth, previewHeight));
            window.Arrange(new Rect(0, 0, previewWidth, previewHeight));
            window.UpdateLayout();

            var bitmap = new RenderTargetBitmap(
                previewWidth,
                previewHeight,
                96,
                96,
                PixelFormats.Pbgra32);
            bitmap.Render(window);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (var stream = File.Create(outputPath))
            {
                encoder.Save(stream);
            }

            window.Close();
            application.Shutdown();

            Console.WriteLine($"PASS 3 preview rendered: {outputPath}");
            Console.WriteLine($"Dimensions: {previewWidth}x{previewHeight}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static int ParseDimension(string raw, string name)
    {
        if (!int.TryParse(raw, out var value) || value <= 0)
        {
            throw new ArgumentException($"Invalid preview {name}: '{raw}'.", name);
        }
        return value;
    }

    private static RunnerRuntimeReport BuildPreviewReport() =>
        new(
            BuildPreviewSnapshots(),
            new[]
            {
                new RunnerSystemFinding(
                    RunnerSystemFindingKind.External,
                    31716,
                    50276,
                    0,
                    "Runner.Listener",
                    null,
                    "EXTERNAL Lotto_MainPC_Runner // Session 0 // Windows service outside D:\\Git_Runners_Main // not controllable by MRC.")
            });

    private static IReadOnlyList<RunnerSnapshot> BuildPreviewSnapshots() =>
    [
        Snap(@"D:\Git_Runners_Main\alpha-idle", "Alpha Builder", "Project Atlas", RunnerState.IDLE),
        Snap(@"D:\Git_Runners_Main\beta-busy", "Beta Compile", "Project Atlas", RunnerState.BUSY),
        Snap(@"D:\Git_Runners_Main\gamma-off", "Gamma Release", "Project Borealis", RunnerState.OFF),
        Snap(@"D:\Git_Runners_Main\delta-error", "Delta QA", "Project Borealis", RunnerState.ERROR, "Listener process evidence is inconsistent."),
        Snap(@"D:\Git_Runners_Main\epsilon-starting", "Epsilon Docs", "Project Cirrus", RunnerState.STARTING),
        Snap(@"D:\Git_Runners_Main\zeta-stopping", "Zeta Packaging", "Project Cirrus", RunnerState.STOPPING),
        Snap(@"D:\Git_Runners_Main\eta-idle", "Eta Research", "Project Drift", RunnerState.IDLE),
        Snap(@"D:\Git_Runners_Main\theta-busy", "Theta Integration", "Project Drift", RunnerState.BUSY),
        Snap(@"D:\Git_Runners_Main\iota-off", "Iota Backup", "Project Ember", RunnerState.OFF),
        Snap(
            @"D:\Git_Runners_Main\VERY-LONG-RUNNER-FOLDER-NAME-FOR-ELLIPSIS-VALIDATION-0123456789",
            "VERY-LONG-RUNNER-NAME-FOR-ELLIPSIS-VALIDATION-ALPHA-0123456789",
            "VERY-LONG-REPOSITORY-NAME-FOR-ELLIPSIS-VALIDATION-OMEGA-0123456789",
            RunnerState.BUSY)
    ];

    private static RunnerSnapshot Snap(
        string path,
        string agent,
        string repository,
        RunnerState state,
        string? error = null) =>
        new(
            new RunnerDescriptor(
                path,
                agent,
                $"https://github.com/doonchy16-cloud/{repository.Replace(' ', '-').ToLowerInvariant()}",
                repository,
                1,
                "_work",
                null),
            state,
            error);
}
