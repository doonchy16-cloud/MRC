using System.Runtime.CompilerServices;

internal static class Task14GuiEnlargementContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifyEnlargedTerminalComposition();
        Console.WriteLine("PASS  Task14 v0.0.13 enlarged terminal composition + truthful version badge");
    }

    private static void VerifyEnlargedTerminalComposition()
    {
        var repoRoot = Directory.GetCurrentDirectory();
        var xaml = File.ReadAllText(Path.Combine(repoRoot, "src", "MRC.Gui", "MainWindow.xaml"));
        var code = File.ReadAllText(Path.Combine(repoRoot, "src", "MRC.Gui", "MainWindow.xaml.cs"));

        Require(xaml.Contains("Width=\"1200\"", StringComparison.Ordinal)
                && xaml.Contains("Height=\"760\"", StringComparison.Ordinal),
            "V0.0.13 default GUI canvas must be 1200x760.");
        Require(xaml.Contains("MinWidth=\"900\"", StringComparison.Ordinal)
                && xaml.Contains("MinHeight=\"560\"", StringComparison.Ordinal),
            "V0.0.13 must preserve the 900x560 minimum-size acceptance target.");

        Require(!xaml.Contains("<Setter Property=\"Height\" Value=\"32\"", StringComparison.Ordinal),
            "Runner rows still use the undersized 32 px v0.0.12 height.");
        Require(xaml.Contains("<Setter Property=\"Height\" Value=\"40\"", StringComparison.Ordinal),
            "Runner rows must use the enlarged 40 px v0.0.13 presentation height.");
        Require(xaml.Contains("<Grid Height=\"40\" ToolTip=\"{Binding DirectoryPath}\">", StringComparison.Ordinal),
            "Runner row template height is not aligned with the enlarged 40 px container height.");

        Require(xaml.Contains("Text=\"MAIN RUNNER CONTROL\" FontSize=\"24\"", StringComparison.Ordinal),
            "Primary GUI identity has not been enlarged to the v0.0.13 hierarchy.");
        Require(xaml.Contains("Text=\"{Binding Glyph}\" Opacity=\"{Binding AnimationIntensity}\" Style=\"{StaticResource StateForegroundStyle}\" FontSize=\"16\"", StringComparison.Ordinal),
            "Animated runner-state glyph remains undersized.");
        Require(xaml.Contains("Text=\"{Binding RunnerName}\"", StringComparison.Ordinal)
                && xaml.Contains("VerticalAlignment=\"Center\" FontSize=\"11\" FontWeight=\"SemiBold\"", StringComparison.Ordinal),
            "Runner identity text has not been enlarged.");
        Require(xaml.Contains("Text=\"{Binding RepositoryName}\"", StringComparison.Ordinal)
                && xaml.Contains("VerticalAlignment=\"Center\" FontSize=\"11\" Margin=\"0,0,10,0\"", StringComparison.Ordinal),
            "Repository identity text has not been enlarged.");
        Require(xaml.Contains("Property=\"FontSize\" Value=\"11\"", StringComparison.Ordinal),
            "Primary terminal controls have not been enlarged above the old 10 px baseline.");
        Require(xaml.Contains("Background=\"{StaticResource PanelBrush}\" Height=\"32\" Padding=\"8,0\"", StringComparison.Ordinal),
            "Search control has not been enlarged from its v0.0.12 28 px shell.");

        Require(xaml.Contains("x:Name=\"RunnerTableSurface\"", StringComparison.Ordinal)
                && xaml.Contains("<RowDefinition Height=\"*\" />", StringComparison.Ordinal),
            "Runner table is no longer the responsive visual hero.");

        Require(xaml.Contains("x:Name=\"VersionValue\"", StringComparison.Ordinal),
            "GUI version badge is not addressable for truthful runtime identity.");
        Require(!xaml.Contains("Text=\"v0.0.12 // PRE-CERT\"", StringComparison.Ordinal),
            "GUI still hard-codes the old v0.0.12 version badge.");
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
