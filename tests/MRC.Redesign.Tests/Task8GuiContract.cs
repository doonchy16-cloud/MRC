using System.Runtime.CompilerServices;

internal static class Task8GuiContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifyHybridCommandCenterStructure();
        Console.WriteLine("PASS  Task8A hybrid native command center structure");
    }

    private static void VerifyHybridCommandCenterStructure()
    {
        var repoRoot = Directory.GetCurrentDirectory();
        var xamlPath = Path.Combine(repoRoot, "src", "MRC.Gui", "MainWindow.xaml");
        Require(File.Exists(xamlPath), $"MainWindow.xaml was not found at {xamlPath}.");
        var xaml = File.ReadAllText(xamlPath);
        var headerXaml = File.ReadAllText(Path.Combine(repoRoot, "src", "MRC.Gui", "Controls", "HeroHeader.xaml"));
        var headerCode = File.ReadAllText(Path.Combine(repoRoot, "src", "MRC.Gui", "Controls", "HeroHeader.xaml.cs"));

        Require(xaml.Contains("Width=\"1200\"", StringComparison.Ordinal)
                && xaml.Contains("Height=\"760\"", StringComparison.Ordinal)
                && xaml.Contains("MinWidth=\"900\"", StringComparison.Ordinal)
                && xaml.Contains("MinHeight=\"560\"", StringComparison.Ordinal),
            "Command Center does not preserve the approved default/minimum window geometry.");

        Require(xaml.Contains("FontFamily=\"Segoe UI\"", StringComparison.OrdinalIgnoreCase),
            "Primary v0.0.14 application typography is not using the native UI font.");
        Require(xaml.Contains("Cascadia Mono", StringComparison.OrdinalIgnoreCase)
                && xaml.Contains("Consolas", StringComparison.OrdinalIgnoreCase),
            "Selective terminal/diagnostic monospace typography is missing.");

        Require(xaml.Contains("x:Name=\"RunnerCardSurface\"", StringComparison.Ordinal),
            "Dominant managed runner-card surface is missing.");
        Require(!xaml.Contains("x:Name=\"RunnerTableSurface\"", StringComparison.Ordinal),
            "Superseded fake-terminal runner table is still active.");
        Require(xaml.Contains("x:Name=\"SystemFindingsPanel\"", StringComparison.Ordinal),
            "External/system findings surface is missing.");
        Require(xaml.Contains("x:Name=\"RefreshStatusValue\"", StringComparison.Ordinal),
            "Command Center status feedback is missing.");

        Require(xaml.Contains("TOTAL", StringComparison.Ordinal)
                && xaml.Contains("IDLE", StringComparison.Ordinal)
                && xaml.Contains("BUSY", StringComparison.Ordinal)
                && xaml.Contains("OFF", StringComparison.Ordinal)
                && xaml.Contains("ERROR", StringComparison.Ordinal)
                && xaml.Contains("TRANSITION", StringComparison.Ordinal),
            "Command Center does not expose all six runtime counters.");

        Require(xaml.Contains("<controls:HeroHeader", StringComparison.Ordinal)
                && headerXaml.Contains("x:Name=\"LiveInventoryPill\"", StringComparison.Ordinal)
                && headerXaml.Contains("Authorized", StringComparison.Ordinal)
                && headerCode.Contains("DependencyProperty AuthorizedProperty", StringComparison.Ordinal),
            "Command Center header does not expose typed authorization state.");
        Require(xaml.Contains("Background=\"#071015\"", StringComparison.OrdinalIgnoreCase),
            "Reference-first near-black control-room background authority is missing.");
        Require(xaml.Contains("x:Name=\"AmbientGlowLayer\"", StringComparison.Ordinal),
            "Reference-first dimensional ambient layer is missing.");
        Require((xaml.Contains("CornerRadius=\"14\"", StringComparison.Ordinal)
                 || xaml.Contains("Property=\"CornerRadius\" Value=\"14\"", StringComparison.Ordinal))
                && xaml.Contains("x:Key=\"StateBadgeStyle\"", StringComparison.Ordinal),
            "Purpose-built application card/badge language is missing.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
