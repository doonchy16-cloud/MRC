using System.Runtime.CompilerServices;

internal static class Task51FV0015RunnerScrollbarChromeContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        var root = Directory.GetCurrentDirectory();
        var mainWindow = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "MainWindow.xaml"));
        var app = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "App.xaml"));
        var preview = File.ReadAllText(Path.Combine(root, "tools", "MRC.Pass3.Preview", "Program.cs"));

        Require(app.Contains("x:Key=\"DarkScrollBarStyle\"", StringComparison.Ordinal)
                && app.Contains("x:Key=\"DarkScrollThumbTemplate\"", StringComparison.Ordinal),
            "F-V15-010: dark scrollbar template authority is missing.");

        var runnerStart = mainWindow.IndexOf("<ListBox x:Name=\"RunnerList\"", StringComparison.Ordinal);
        Require(runnerStart >= 0, "F-V15-010: RunnerList is missing.");
        var runnerEnd = mainWindow.IndexOf("</ListBox>", runnerStart, StringComparison.Ordinal);
        Require(runnerEnd > runnerStart, "F-V15-010: RunnerList block is malformed.");
        var runnerBlock = mainWindow[runnerStart..runnerEnd];

        Require(runnerBlock.Contains("<ListBox.Resources>", StringComparison.Ordinal)
                && runnerBlock.Contains("TargetType=\"{x:Type ScrollBar}\"", StringComparison.Ordinal)
                && runnerBlock.Contains("BasedOn=\"{StaticResource DarkScrollBarStyle}\"", StringComparison.Ordinal),
            "F-V15-010: RunnerList must consume DarkScrollBarStyle at the visual resource boundary; merely defining or attempting runtime discovery is insufficient.");

        Require(preview.Contains("new App", StringComparison.Ordinal)
                && preview.Contains("InitializeComponent();", StringComparison.Ordinal),
            "F-V15-010: deterministic preview must initialize the real MRC App resources before constructing MainWindow.");

        Require(runnerBlock.Contains("ScrollViewer.VerticalScrollBarVisibility=\"Auto\"", StringComparison.Ordinal)
                && runnerBlock.Contains("ScrollViewer.HorizontalScrollBarVisibility=\"Disabled\"", StringComparison.Ordinal),
            "F-V15-010: dark scrollbar convergence must preserve vertical-auto/horizontal-disabled scrolling semantics.");

        Console.WriteLine("PASS  F-V15-010 RunnerList dark-scrollbar consumption + preview-resource contract");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
