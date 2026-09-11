using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MRC.Core.Runners;
using MRC.Core.Runtime;
using MRC.Gui;
using MRC.Gui.Controls;
using MRC.Gui.Presentation;

namespace MRC.Pass3.Preview;

internal static class Program
{
    private const int DefaultPreviewWidth = 1180;
    private const int DefaultPreviewHeight = 760;
    private const int MinimumPreviewWidth = 900;
    private const int MinimumPreviewHeight = 560;
    private static readonly DateTimeOffset FixedPreviewTime =
        new(2026, 9, 9, 21, 41, 17, TimeSpan.Zero);

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            var options = ParseOptions(args);
            if (options.Width < MinimumPreviewWidth || options.Height < MinimumPreviewHeight)
            {
                throw new ArgumentOutOfRangeException(nameof(args),
                    $"PASS 3 preview geometry must be at least {MinimumPreviewWidth}x{MinimumPreviewHeight}.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(options.OutputPath)!);

            var application = new App { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            application.InitializeComponent();
            var window = new MainWindow
            {
                Width = options.Width,
                Height = options.Height,
                MinWidth = options.Width,
                MinHeight = options.Height,
                MaxWidth = options.Width,
                MaxHeight = options.Height,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.None,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = -20000,
                Top = -20000
            };

            if (options.MixedState)
                window.ConfigurePreview(BuildMixedPreviewReport());
            else
                window.ConfigurePreview(BuildPreviewReport());

            window.Show();
            PinDeterministicPresentation(window);
            window.Measure(new Size(options.Width, options.Height));
            window.Arrange(new Rect(0, 0, options.Width, options.Height));
            window.UpdateLayout();

            if (options.FocusState is not null)
            {
                FocusRunnerState(window, options.FocusState.Value);
                window.UpdateLayout();
            }

            if (string.Equals(options.Drawer, "control", StringComparison.OrdinalIgnoreCase))
            {
                ShowControlDrawer(window, options.Width);
                window.UpdateLayout();
            }
            else if (string.Equals(options.Drawer, "system", StringComparison.OrdinalIgnoreCase))
            {
                ShowSystemDrawer(window);
                window.UpdateLayout();
            }

            var bitmap = new RenderTargetBitmap(
                options.Width,
                options.Height,
                96,
                96,
                PixelFormats.Pbgra32);
            bitmap.Render(window);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (var stream = File.Create(options.OutputPath))
            {
                encoder.Save(stream);
            }

            window.Close();
            application.Shutdown();

            Console.WriteLine($"PASS 3 preview rendered: {options.OutputPath}");
            Console.WriteLine($"Dimensions: {options.Width}x{options.Height}");
            if (options.FocusState is not null) Console.WriteLine($"Focused state: {options.FocusState.Value}");
            if (options.Drawer is not null) Console.WriteLine($"Drawer: {options.Drawer}");
            if (options.MixedState) Console.WriteLine("Mixed state: enabled");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static PreviewOptions ParseOptions(string[] args)
    {
        if (!args.Any(argument => argument.StartsWith("--", StringComparison.Ordinal)))
            return ParseLegacyOptions(args);

        var width = DefaultPreviewWidth;
        var height = DefaultPreviewHeight;
        string? output = null;
        RunnerState? focusState = null;
        string? drawer = null;
        var mixedState = false;

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--width":
                    width = ParseDimension(NextValue(args, ref index, "--width"), "width");
                    break;
                case "--height":
                    height = ParseDimension(NextValue(args, ref index, "--height"), "height");
                    break;
                case "--output":
                    output = NextValue(args, ref index, "--output");
                    break;
                case "--focus-state":
                    focusState = ParseState(NextValue(args, ref index, "--focus-state"));
                    break;
                case "--drawer":
                    drawer = NextValue(args, ref index, "--drawer");
                    if (!string.Equals(drawer, "control", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(drawer, "system", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new ArgumentException($"Unsupported preview drawer: '{drawer}'.", nameof(args));
                    }
                    break;
                case "--mixed-state":
                    mixedState = true;
                    break;
                default:
                    throw new ArgumentException($"Unknown preview option: '{args[index]}'.", nameof(args));
            }
        }

        var outputPath = Path.GetFullPath(output ??
            Path.Combine("artifacts", "pass3", $"MRC-PASS3-{width}x{height}.png"));
        return new PreviewOptions(width, height, outputPath, focusState, drawer, mixedState);
    }

    private static PreviewOptions ParseLegacyOptions(string[] args)
    {
        var width = args.Length > 1 ? ParseDimension(args[1], "width") : DefaultPreviewWidth;
        var height = args.Length > 2 ? ParseDimension(args[2], "height") : DefaultPreviewHeight;
        var focusState = args.Length > 3 ? ParseState(args[3]) : (RunnerState?)null;
        var outputPath = Path.GetFullPath(args.Length > 0
            ? args[0]
            : Path.Combine("artifacts", "pass3", $"MRC-PASS3-{width}x{height}.png"));
        return new PreviewOptions(width, height, outputPath, focusState, null, false);
    }

    private static string NextValue(string[] args, ref int index, string option)
    {
        if (index + 1 >= args.Length)
            throw new ArgumentException($"Preview option {option} requires a value.", nameof(args));
        index++;
        return args[index];
    }

    private static int ParseDimension(string raw, string name)
    {
        if (!int.TryParse(raw, out var value) || value <= 0)
            throw new ArgumentException($"Invalid preview {name}: '{raw}'.", name);
        return value;
    }

    private static RunnerState ParseState(string raw)
    {
        if (!Enum.TryParse<RunnerState>(raw, ignoreCase: true, out var state))
            throw new ArgumentException($"Invalid preview focus state: '{raw}'.", nameof(raw));
        return state;
    }

    private static void PinDeterministicPresentation(MainWindow window)
    {
        if (window.DataContext is RunnerDashboardViewModel dashboard)
            new RunnerAnimationClock().Tick(FixedPreviewTime, dashboard.Rows);

        if (window.FindName("HeroHeader") is HeroHeader hero)
            hero.TruthText = "local truth • 21:41:17 • 3s refresh • 20 FPS UI";
    }

    private static void ShowControlDrawer(MainWindow window, int width)
    {
        if (window.FindName("ControlDrawerHost") is not FrameworkElement host
            || window.FindName("DrawerScrim") is not UIElement scrim
            || window.FindName("ControlDrawer") is not ControlDrawer drawer)
        {
            throw new InvalidOperationException("Control Drawer composition was not available for preview evidence.");
        }

        host.Width = Math.Min(400, Math.Max(0, width - 32));
        scrim.Visibility = Visibility.Visible;
        host.Visibility = Visibility.Visible;

        // Keep preview visually faithful while remaining non-interactive and runtime-gated.
        drawer.IsEnabled = true;
        drawer.IsHitTestVisible = false;
    }

    private static void ShowSystemDrawer(MainWindow window)
    {
        if (window.FindName("SystemFindingsPanel") is not ToggleButton toggle
            || window.FindName("SystemDrawerHost") is not FrameworkElement host)
        {
            throw new InvalidOperationException("System Drawer composition was not available for preview evidence.");
        }

        host.MaxHeight = Math.Max(0, window.ActualHeight * 0.35);
        host.IsHitTestVisible = false;
        toggle.IsChecked = true;
    }

    private static void FocusRunnerState(MainWindow window, RunnerState focusState)
    {
        if (window.FindName("RunnerList") is not ListBox runnerList)
            throw new InvalidOperationException("RunnerList was not available for focused preview evidence.");

        var target = runnerList.Items
            .OfType<RunnerRowViewModel>()
            .FirstOrDefault(row => row.State == focusState);
        if (target is null)
            throw new InvalidOperationException($"No preview runner exists in state {focusState}.");

        runnerList.ScrollIntoView(target);
        runnerList.UpdateLayout();
    }

    private static RunnerRuntimeReport BuildPreviewReport() =>
        new(
            BuildPreviewSnapshots(),
            BuildSystemFindings());

    private static RunnerRuntimeReport BuildMixedPreviewReport() =>
        new(
            new[]
            {
                Snap(@"D:\Git_Runners_Main\alpha-idle", "Alpha Builder", "Project Atlas", RunnerState.IDLE),
                Snap(@"D:\Git_Runners_Main\beta-off", "Beta Compile", "Project Atlas", RunnerState.OFF),
                Snap(@"D:\Git_Runners_Main\gamma-busy", "Gamma Release", "Project Borealis", RunnerState.BUSY),
                Snap(@"D:\Git_Runners_Main\delta-idle", "Delta QA", "Project Borealis", RunnerState.IDLE),
                Snap(@"D:\Git_Runners_Main\epsilon-off", "Epsilon Docs", "Project Cirrus", RunnerState.OFF),
                Snap(@"D:\Git_Runners_Main\zeta-busy", "Zeta Packaging", "Project Cirrus", RunnerState.BUSY),
                Snap(@"D:\Git_Runners_Main\eta-idle", "Eta Research", "Project Drift", RunnerState.IDLE),
                Snap(@"D:\Git_Runners_Main\theta-off", "Theta Integration", "Project Drift", RunnerState.OFF)
            },
            BuildSystemFindings());

    private static IReadOnlyList<RunnerSystemFinding> BuildSystemFindings() =>
    [
        new RunnerSystemFinding(
            RunnerSystemFindingKind.External,
            31716,
            50276,
            0,
            "Runner.Listener",
            null,
            "EXTERNAL Lotto_MainPC_Runner // Session 0 // Windows service outside D:\\Git_Runners_Main // not controllable by MRC.")
    ];

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
        Snap(@"D:\Git_Runners_Main\kappa-idle", "Kappa Cache", "Project Ember", RunnerState.IDLE),
        Snap(@"D:\Git_Runners_Main\lambda-off", "Lambda Deploy", "Project Flux", RunnerState.OFF),
        Snap(@"D:\Git_Runners_Main\mu-busy", "Mu Worker", "Project Flux", RunnerState.BUSY),
        Snap(@"D:\Git_Runners_Main\nu-idle", "Nu Sync", "Project Grove", RunnerState.IDLE),
        Snap(@"D:\Git_Runners_Main\xi-off", "Xi Monitor", "Project Grove", RunnerState.OFF),
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

    private sealed record PreviewOptions(
        int Width,
        int Height,
        string OutputPath,
        RunnerState? FocusState,
        string? Drawer,
        bool MixedState);
}
