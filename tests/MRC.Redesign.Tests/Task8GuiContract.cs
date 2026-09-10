using System.Runtime.CompilerServices;

internal static class Task8GuiContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifyTerminalConsoleStructure();
        Console.WriteLine("PASS  Task8A terminal operations console structure");
    }

    private static void VerifyTerminalConsoleStructure()
    {
        var repoRoot = Directory.GetCurrentDirectory();
        var xamlPath = Path.Combine(repoRoot, "src", "MRC.Gui", "MainWindow.xaml");
        Require(File.Exists(xamlPath), $"MainWindow.xaml was not found at {xamlPath}.");
        var xaml = File.ReadAllText(xamlPath);

        Require(xaml.Contains("Width=\"1200\"", StringComparison.Ordinal)
                && xaml.Contains("Height=\"760\"", StringComparison.Ordinal)
                && xaml.Contains("MinWidth=\"900\"", StringComparison.Ordinal)
                && xaml.Contains("MinHeight=\"560\"", StringComparison.Ordinal),
            "Terminal console does not preserve the v0.0.13 default/minimum window geometry.");

        Require(xaml.Contains("Cascadia Mono", StringComparison.OrdinalIgnoreCase)
                && xaml.Contains("Consolas", StringComparison.OrdinalIgnoreCase),
            "Terminal console does not use the locked Cascadia Mono / Consolas font stack.");
        Require(!xaml.Contains("FontFamily=\"Segoe UI\"", StringComparison.OrdinalIgnoreCase),
            "Legacy Segoe UI root typography is still present.");

        Require(xaml.Contains("x:Name=\"InlineStatusStrip\"", StringComparison.Ordinal),
            "Compact inline status strip is missing.");
        Require(xaml.Contains("x:Name=\"RunnerTableSurface\"", StringComparison.Ordinal),
            "Dominant runner-table surface is missing.");
        Require(xaml.Contains("x:Name=\"SystemFindingsPanel\"", StringComparison.Ordinal),
            "External/system findings strip is missing.");
        Require(xaml.Contains("x:Name=\"TerminalStatusFooter\"", StringComparison.Ordinal),
            "Prompt-style terminal status footer is missing.");

        Require(xaml.Contains("<Setter Property=\"Height\" Value=\"40\"", StringComparison.Ordinal),
            "Runner rows do not use the enlarged v0.0.13 40px readability target.");
        Require(!xaml.Contains("<Setter Property=\"Height\" Value=\"32\"", StringComparison.Ordinal),
            "Legacy 32px runner rows are still present after v0.0.13 enlargement.");

        Require(xaml.Contains("TOTAL", StringComparison.Ordinal)
                && xaml.Contains("IDLE", StringComparison.Ordinal)
                && xaml.Contains("BUSY", StringComparison.Ordinal)
                && xaml.Contains("OFF", StringComparison.Ordinal)
                && xaml.Contains("ERROR", StringComparison.Ordinal)
                && xaml.Contains("TRANS", StringComparison.Ordinal),
            "Inline status strip does not expose the six locked runtime counters.");

        Require(xaml.Contains("[AUTHORIZED]", StringComparison.Ordinal)
                || xaml.Contains("AUTHORIZED", StringComparison.Ordinal),
            "Terminal header does not expose authorization state.");
        Require(xaml.Contains("Background=\"#0B0F14\"", StringComparison.OrdinalIgnoreCase),
            "Near-black terminal background authority is missing.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
