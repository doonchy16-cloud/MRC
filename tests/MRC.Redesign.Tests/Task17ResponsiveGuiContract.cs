using System.Runtime.CompilerServices;

internal static class Task17ResponsiveGuiContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifyLargeScreenResponsiveness();
        VerifySearchSemantics();
        VerifyBoundedSystemStrip();
        VerifyLargeScreenPreviewEvidence();
        Console.WriteLine("PASS  Task17 responsive large-screen composition + search/system-strip semantics");
    }

    private static void VerifyLargeScreenResponsiveness()
    {
        var root = Directory.GetCurrentDirectory();
        var code = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "MainWindow.xaml.cs"));

        Require(code.Contains("ApplyResponsiveScale", StringComparison.Ordinal),
            "MainWindow has no responsive scaling authority for large windows.");
        Require(code.Contains("SizeChanged", StringComparison.Ordinal),
            "Responsive scale is not recomputed when the window size changes.");
        Require(code.Contains("Math.Clamp", StringComparison.Ordinal)
                && code.Contains("ActualWidth / 1200d", StringComparison.Ordinal)
                && code.Contains("ActualHeight / 760d", StringComparison.Ordinal),
            "Responsive scale is not derived from the approved 1200x760 baseline.");
        Require(code.Contains("RootSurface.LayoutTransform", StringComparison.Ordinal)
                && code.Contains("new ScaleTransform(scale, scale)", StringComparison.Ordinal),
            "Responsive scale is not applied centrally to the terminal surface.");
        Require(code.Contains("1d, 1.5d", StringComparison.Ordinal),
            "Responsive scale must stay bounded between 1.0x and 1.5x.");
    }

    private static void VerifySearchSemantics()
    {
        var root = Directory.GetCurrentDirectory();
        var xaml = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "MainWindow.xaml"));
        var code = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "MainWindow.xaml.cs"));

        Require(xaml.Contains("Text=\"SEARCH\"", StringComparison.Ordinal),
            "Search field still lacks an explicit SEARCH label.");
        Require(xaml.Contains("ToolTip=\"Press / to focus search\"", StringComparison.Ordinal),
            "Slash search shortcut is not explained as a shortcut.");
        Require(code.Contains("Key.OemQuestion", StringComparison.Ordinal)
                && code.Contains("SearchBox.Focus()", StringComparison.Ordinal),
            "Slash cue is decorative only; the search focus shortcut is not implemented.");
    }

    private static void VerifyBoundedSystemStrip()
    {
        var xaml = File.ReadAllText(Path.Combine(
            Directory.GetCurrentDirectory(), "src", "MRC.Gui", "MainWindow.xaml"));

        Require(xaml.Contains("x:Name=\"SystemFindingsPanel\"", StringComparison.Ordinal),
            "SYSTEM findings panel is missing.");
        Require(xaml.Contains("<Grid.ColumnDefinitions><ColumnDefinition Width=\"Auto\" /><ColumnDefinition Width=\"*\" /></Grid.ColumnDefinitions>", StringComparison.Ordinal),
            "SYSTEM strip does not allocate a bounded star column for long evidence.");
        Require(xaml.Contains("x:Name=\"SystemFindingsValue\" Grid.Column=\"1\"", StringComparison.Ordinal)
                && xaml.Contains("TextTrimming=\"CharacterEllipsis\"", StringComparison.Ordinal),
            "SYSTEM evidence is not constrained to an ellipsizing bounded column.");
    }

    private static void VerifyLargeScreenPreviewEvidence()
    {
        var script = File.ReadAllText(Path.Combine(
            Directory.GetCurrentDirectory(), "scripts", "render-v0.0.13-preview.ps1"));

        Require(script.Contains("MRC-v0.0.13-2048x1222.png", StringComparison.Ordinal)
                && script.Contains("-Width 2048 -Height 1222", StringComparison.Ordinal),
            "Preview pipeline does not cover the large-screen 2048x1222 acceptance viewport.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
