using System.Runtime.CompilerServices;

internal static class Task42V0015SystemDrawerContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        var root = Directory.GetCurrentDirectory();
        var drawerXamlPath = Path.Combine(root, "src", "MRC.Gui", "Controls", "SystemDrawer.xaml");
        var drawerCodePath = Path.Combine(root, "src", "MRC.Gui", "Controls", "SystemDrawer.xaml.cs");

        Require(File.Exists(drawerXamlPath), "SystemDrawer.xaml is missing.");
        Require(File.Exists(drawerCodePath), "SystemDrawer.xaml.cs is missing.");

        var drawerXaml = File.ReadAllText(drawerXamlPath);
        var drawerCode = File.ReadAllText(drawerCodePath);
        var windowXaml = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "MainWindow.xaml"));
        var windowCode = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "MainWindow.xaml.cs"));

        Require(drawerXaml.Contains("SystemFindingsSummary", StringComparison.Ordinal),
            "System drawer summary binding is missing.");
        Require(drawerXaml.Contains("SystemFindings", StringComparison.Ordinal),
            "System drawer finding collection binding is missing.");
        Require(drawerXaml.Contains("HasSystemFindings", StringComparison.Ordinal),
            "System drawer does not expose truthful empty/findings state.");

        Require(!drawerXaml.Contains("START", StringComparison.Ordinal)
                && !drawerXaml.Contains("FORCE STOP", StringComparison.Ordinal)
                && !drawerXaml.Contains("RESTART", StringComparison.Ordinal),
            "System drawer gained runner lifecycle controls.");
        Require(!drawerCode.Contains("RunnerOperationsService", StringComparison.Ordinal)
                && !drawerCode.Contains("RunnerEngine", StringComparison.Ordinal)
                && !drawerCode.Contains("RunnerCardAction", StringComparison.Ordinal),
            "System drawer owns lifecycle/runtime authority instead of remaining presentation-only.");

        Require(windowXaml.Contains("x:Name=\"SystemDrawerHost\"", StringComparison.Ordinal)
                && windowXaml.Contains("<controls:SystemDrawer x:Name=\"SystemDrawer\"", StringComparison.Ordinal),
            "MainWindow does not compose the SystemDrawer overlay.");
        Require(windowXaml.Contains("VerticalAlignment=\"Bottom\"", StringComparison.Ordinal)
                && windowXaml.Contains("Visibility=\"Collapsed\"", StringComparison.Ordinal),
            "System drawer is not a collapsed bottom-anchored overlay.");
        Require(windowCode.Contains("SystemDrawerHost.MaxHeight = Math.Max(0, ActualHeight * 0.35);", StringComparison.Ordinal),
            "System drawer does not enforce the locked 35% maximum window-height authority.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
