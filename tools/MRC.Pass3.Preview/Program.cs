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
    private const int PreviewWidth = 1180;
    private const int PreviewHeight = 760;

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            var outputPath = Path.GetFullPath(args.Length > 0
                ? args[0]
                : Path.Combine("artifacts", "pass3", "MRC-PASS3-1180x760.png"));
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            var window = new MainWindow
            {
                Width = PreviewWidth,
                Height = PreviewHeight,
                MinWidth = PreviewWidth,
                MinHeight = PreviewHeight,
                MaxWidth = PreviewWidth,
                MaxHeight = PreviewHeight,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.None,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = -20000,
                Top = -20000
            };

            window.ConfigurePreview(BuildPreviewSnapshots());
            window.Show();
            window.Measure(new Size(PreviewWidth, PreviewHeight));
            window.Arrange(new Rect(0, 0, PreviewWidth, PreviewHeight));
            window.UpdateLayout();

            var bitmap = new RenderTargetBitmap(
                PreviewWidth,
                PreviewHeight,
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
            Console.WriteLine($"Dimensions: {PreviewWidth}x{PreviewHeight}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static IReadOnlyList<RunnerSnapshot> BuildPreviewSnapshots() =>
    [
        Snap(@"D:\GitHub_Runners\alpha-idle", "Alpha Builder", "Project Atlas", RunnerState.IDLE),
        Snap(@"D:\GitHub_Runners\beta-busy", "Beta Compile", "Project Atlas", RunnerState.BUSY),
        Snap(@"D:\GitHub_Runners\gamma-off", "Gamma Release", "Project Borealis", RunnerState.OFF),
        Snap(@"D:\GitHub_Runners\delta-error", "Delta QA", "Project Borealis", RunnerState.ERROR, "Listener process evidence is inconsistent."),
        Snap(@"D:\GitHub_Runners\epsilon-starting", "Epsilon Docs", "Project Cirrus", RunnerState.STARTING),
        Snap(@"D:\GitHub_Runners\zeta-stopping", "Zeta Packaging", "Project Cirrus", RunnerState.STOPPING),
        Snap(@"D:\GitHub_Runners\eta-idle", "Eta Research", "Project Drift", RunnerState.IDLE),
        Snap(@"D:\GitHub_Runners\theta-busy", "Theta Integration", "Project Drift", RunnerState.BUSY),
        Snap(@"D:\GitHub_Runners\iota-off", "Iota Backup", "Project Ember", RunnerState.OFF),
        Snap(
            @"D:\GitHub_Runners\VERY-LONG-RUNNER-FOLDER-NAME-FOR-ELLIPSIS-VALIDATION-0123456789",
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
