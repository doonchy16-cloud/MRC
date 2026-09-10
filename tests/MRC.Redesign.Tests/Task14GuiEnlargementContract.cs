using System.Runtime.CompilerServices;

internal static class Task14GuiEnlargementContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifyCurrentApplicationComposition();
        Console.WriteLine("PASS  Task14 current application composition + truthful version badge");
    }

    private static void VerifyCurrentApplicationComposition()
    {
        var repoRoot = Directory.GetCurrentDirectory();
        var xaml = File.ReadAllText(Path.Combine(repoRoot, "src", "MRC.Gui", "MainWindow.xaml"));
        var code = File.ReadAllText(Path.Combine(repoRoot, "src", "MRC.Gui", "MainWindow.xaml.cs"));

        Require(xaml.Contains("Width=\"1200\"", StringComparison.Ordinal)
                && xaml.Contains("Height=\"760\"", StringComparison.Ordinal),
            "Current default GUI canvas must remain 1200x760.");
        Require(xaml.Contains("MinWidth=\"900\"", StringComparison.Ordinal)
                && xaml.Contains("MinHeight=\"560\"", StringComparison.Ordinal),
            "Current GUI must preserve the 900x560 minimum-size acceptance target.");

        Require(xaml.Contains("Text=\"MAIN RUNNER CONTROL\"", StringComparison.Ordinal),
            "Primary MRC product identity is missing.");
        Require(xaml.Contains("x:Name=\"RunnerCardSurface\"", StringComparison.Ordinal),
            "Current GUI has not moved to the v0.0.14 runner-card command center.");
        Require(!xaml.Contains("x:Name=\"RunnerTableSurface\"", StringComparison.Ordinal),
            "Superseded v0.0.13 runner-table surface is still active.");
        Require(xaml.Contains("Text=\"{Binding RunnerName}\"", StringComparison.Ordinal)
                && xaml.Contains("FontSize=\"18\"", StringComparison.Ordinal),
            "Runner identity is not presented at the enlarged command-center hierarchy.");
        Require(xaml.Contains("Property=\"MinHeight\" Value=\"38\"", StringComparison.Ordinal)
                || xaml.Contains("MinHeight=\"38\"", StringComparison.Ordinal),
            "Primary card controls do not preserve readable hit-target height.");

        Require(xaml.Contains("x:Name=\"VersionValue\"", StringComparison.Ordinal),
            "GUI version badge is not addressable for truthful runtime identity.");
        Require(code.Contains("VersionValue.Text", StringComparison.Ordinal)
                && code.Contains("BuildInfo.Version", StringComparison.Ordinal),
            "GUI version badge is not populated from BuildInfo.Version.");
        Require(!code.Contains("v0.0.12 preview", StringComparison.Ordinal)
                && !code.Contains("Deterministic v0.0.12", StringComparison.Ordinal),
            "Preview mode still hard-codes stale v0.0.12 identity text.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
