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
        var card = File.ReadAllText(Path.Combine(repoRoot, "src", "MRC.Gui", "Controls", "RunnerControlCard.xaml"));
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
            "Current GUI has not moved to the runner-card command center.");
        Require(!xaml.Contains("x:Name=\"RunnerTableSurface\"", StringComparison.Ordinal),
            "Superseded runner-table surface is still active.");
        Require(xaml.Contains("<controls:RunnerControlCard Row=\"{Binding}\"", StringComparison.Ordinal),
            "MainWindow is not composing the extracted RunnerControlCard.");
        Require(card.Contains("Text=\"{Binding Row.RunnerName, ElementName=Root}\"", StringComparison.Ordinal)
                && card.Contains("FontSize=\"17\"", StringComparison.Ordinal),
            "Runner identity is not presented at the compact reference-card hierarchy.");
        Require(card.Contains("Height=\"46\"", StringComparison.Ordinal),
            "Primary card controls do not preserve the locked 46 px reference action height.");

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
