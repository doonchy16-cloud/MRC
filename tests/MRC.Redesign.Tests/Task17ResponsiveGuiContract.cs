using System.Runtime.CompilerServices;

internal static class Task17ResponsiveGuiContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifyAdaptiveCardResponsiveness();
        VerifySearchSemantics();
        VerifyBoundedSystemStrip();
        VerifyLargeScreenPreviewEvidence();
        Console.WriteLine("PASS  Task17 adaptive command-center responsiveness + drawer-search/system-strip semantics");
    }

    private static void VerifyAdaptiveCardResponsiveness()
    {
        var root = Directory.GetCurrentDirectory();
        var code = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "MainWindow.xaml.cs"));
        var xaml = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "MainWindow.xaml"));
        var dashboard = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "Presentation", "RunnerDashboardViewModel.cs"));

        Require(code.Contains("ApplyResponsiveLayout", StringComparison.Ordinal),
            "MainWindow has no adaptive layout authority for v0.0.14.");
        Require(code.Contains("SizeChanged", StringComparison.Ordinal),
            "Adaptive layout is not recomputed when the window size changes.");
        Require(code.Contains("_dashboard.CardColumnCount", StringComparison.Ordinal),
            "Window width does not drive the card-column count.");
        Require(dashboard.Contains("CardColumnCount", StringComparison.Ordinal),
            "Dashboard exposes no adaptive card-column state.");
        Require(xaml.Contains("<UniformGrid Columns=\"{Binding CardColumnCount}\"", StringComparison.Ordinal),
            "Runner-card surface is not bound to adaptive column count.");
        Require(!code.Contains("RootSurface.LayoutTransform", StringComparison.Ordinal),
            "Superseded whole-surface scaling remains active in v0.0.14.");
    }

    private static void VerifySearchSemantics()
    {
        var root = Directory.GetCurrentDirectory();
        var drawer = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "Controls", "ControlDrawer.xaml"));
        var drawerCode = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "Controls", "ControlDrawer.xaml.cs"));
        var code = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "MainWindow.xaml.cs"));

        Require(drawer.Contains("Text=\"SEARCH\"", StringComparison.Ordinal),
            "Drawer search field lacks an explicit SEARCH label.");
        Require(drawer.Contains("ToolTip=\"Press / to focus search\"", StringComparison.Ordinal),
            "Slash search shortcut is not explained as a shortcut.");
        Require(code.Contains("Key.OemQuestion", StringComparison.Ordinal)
                && code.Contains("OpenControlDrawer(focusSearch: true)", StringComparison.Ordinal)
                && drawerCode.Contains("SearchBox.Focus()", StringComparison.Ordinal)
                && drawerCode.Contains("SearchBox.SelectAll()", StringComparison.Ordinal),
            "Slash cue is decorative only; opening and focusing drawer search is not implemented.");
    }

    private static void VerifyBoundedSystemStrip()
    {
        var xaml = File.ReadAllText(Path.Combine(
            Directory.GetCurrentDirectory(), "src", "MRC.Gui", "MainWindow.xaml"));

        Require(xaml.Contains("x:Name=\"SystemFindingsPanel\"", StringComparison.Ordinal),
            "SYSTEM findings panel is missing.");
        Require(xaml.Contains("x:Name=\"SystemFindingsValue\"", StringComparison.Ordinal)
                && xaml.Contains("TextTrimming=\"CharacterEllipsis\"", StringComparison.Ordinal),
            "SYSTEM evidence is not constrained to an ellipsizing bounded presentation.");
    }

    private static void VerifyLargeScreenPreviewEvidence()
    {
        var script = File.ReadAllText(Path.Combine(
            Directory.GetCurrentDirectory(), "scripts", "render-v0.0.13-preview.ps1"));

        Require(script.Contains("MRC-v0.0.13-2048x1222.png", StringComparison.Ordinal)
                && script.Contains("-Width 2048 -Height 1222", StringComparison.Ordinal),
            "Inherited preview pipeline does not cover the large-screen 2048x1222 acceptance viewport.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
