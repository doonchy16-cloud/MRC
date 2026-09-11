using System.Runtime.CompilerServices;

internal static class Task49FV0015RunnerFieldChromeContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        var root = Directory.GetCurrentDirectory();
        var mainWindow = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "MainWindow.xaml"));

        const string marker = "x:Name=\"RunnerCardSurface\"";
        var start = mainWindow.IndexOf(marker, StringComparison.Ordinal);
        Require(start >= 0, "F-V15-008: runner-card surface host is missing.");

        var tagStart = mainWindow.LastIndexOf('<', start);
        var tagEnd = mainWindow.IndexOf('>', start);
        Require(tagStart >= 0 && tagEnd > start, "F-V15-008: runner-card surface opening tag is malformed.");
        var openingTag = mainWindow[tagStart..tagEnd];

        Require(openingTag.Contains("Background=\"Transparent\"", StringComparison.Ordinal),
            "F-V15-008: runner field must float directly over the ambient background instead of painting one giant dark dashboard panel.");
        Require(openingTag.Contains("BorderThickness=\"0\"", StringComparison.Ordinal),
            "F-V15-008: runner field must not draw a giant rounded perimeter frame around the cards.");
        Require(openingTag.Contains("ClipToBounds=\"True\"", StringComparison.Ordinal),
            "F-V15-008: floating runner field must preserve bounded scrolling/clipping behavior.");

        Require(mainWindow.Contains("<ListBox x:Name=\"RunnerList\"", StringComparison.Ordinal)
                && mainWindow.Contains("ItemsSource=\"{Binding VisibleRows}\"", StringComparison.Ordinal)
                && mainWindow.Contains("<UniformGrid Columns=\"{Binding CardColumnCount}\"", StringComparison.Ordinal),
            "F-V15-008: visual convergence must preserve the existing runner collection and responsive grid authority.");
        Require(mainWindow.Contains("<controls:AmbientBackground Panel.ZIndex=\"0\" />", StringComparison.Ordinal),
            "F-V15-008: cards must continue to float over the approved native ambient background.");

        Console.WriteLine("PASS  F-V15-008 floating runner-field chrome contract");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
