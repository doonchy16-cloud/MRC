using System.Runtime.CompilerServices;

internal static class Task39V0015HeroHeaderContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        var root = Directory.GetCurrentDirectory();
        var headerPath = Path.Combine(root, "src", "MRC.Gui", "Controls", "HeroHeader.xaml");
        var headerCodePath = Path.Combine(root, "src", "MRC.Gui", "Controls", "HeroHeader.xaml.cs");

        Require(File.Exists(headerPath), "HeroHeader.xaml is missing.");
        Require(File.Exists(headerCodePath), "HeroHeader.xaml.cs is missing.");

        var xaml = File.ReadAllText(headerPath);
        var code = File.ReadAllText(headerCodePath);
        var main = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "MainWindow.xaml"));
        var mainCode = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "MainWindow.xaml.cs"));

        Require(xaml.Contains("MAIN PC • LOCAL-FIRST CONTROL", StringComparison.Ordinal),
            "HeroHeader eyebrow text drifted from the Owner reference.");
        Require(xaml.Contains("MAIN RUNNER CONTROL", StringComparison.Ordinal),
            "HeroHeader title drifted from the Owner reference.");
        Require(xaml.Contains("x:Name=\"LiveInventoryPill\"", StringComparison.Ordinal),
            "HeroHeader live inventory pill is missing.");
        Require(xaml.Contains("Width=\"58\"", StringComparison.Ordinal)
                && xaml.Contains("Height=\"58\"", StringComparison.Ordinal),
            "HeroHeader reference menu-button scale is missing.");
        Require(xaml.Contains("FontSize=\"52\"", StringComparison.Ordinal),
            "HeroHeader does not preserve the 52 px reference hero scale.");

        foreach (var property in new[] { "InventoryText", "TruthText", "VersionText", "Authorized" })
            Require(code.Contains($"DependencyProperty {property}Property", StringComparison.Ordinal)
                    && code.Contains($"nameof({property})", StringComparison.Ordinal),
                $"HeroHeader dependency property {property} is missing.");

        Require(code.Contains("event RoutedEventHandler? MenuRequested", StringComparison.Ordinal)
                && code.Contains("MenuRequested?.Invoke", StringComparison.Ordinal),
            "HeroHeader menu request event surface is missing.");
        Require(!code.Contains("RunnerOperationsService", StringComparison.Ordinal)
                && !code.Contains("RunnerEngine", StringComparison.Ordinal)
                && !code.Contains("ForceStop", StringComparison.Ordinal),
            "HeroHeader must remain presentation-only and cannot own lifecycle authority.");

        Require(main.Contains("<controls:HeroHeader", StringComparison.Ordinal)
                && main.Contains("x:Name=\"HeroHeader\"", StringComparison.Ordinal)
                && main.Contains("MenuRequested=\"HeroHeader_OnMenuRequested\"", StringComparison.Ordinal),
            "MainWindow does not compose the active HeroHeader and menu event.");
        Require(mainCode.Contains("private void UpdateHeroTruth(bool authorized)", StringComparison.Ordinal)
                && mainCode.Contains("LOCAL INVENTORY LIVE", StringComparison.Ordinal)
                && mainCode.Contains("_dashboard.TotalCount", StringComparison.Ordinal)
                && mainCode.Contains("3s refresh • 20 FPS UI", StringComparison.Ordinal)
                && mainCode.Contains("BuildInfo.Version", StringComparison.Ordinal),
            "MainWindow does not populate HeroHeader from truthful managed inventory/version state.");
        Require(mainCode.Contains("HeroHeader_OnMenuRequested", StringComparison.Ordinal)
                && mainCode.Contains("OpenControlDrawer()", StringComparison.Ordinal),
            "HeroHeader menu event does not route only to the control-drawer entry point.");

        Console.WriteLine("PASS  Task39 v0.0.15 reference HeroHeader contract");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
