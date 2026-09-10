namespace MRC.Pass4.Tests;

internal static class GuiVisualPolishAcceptance
{
    public static int Run()
    {
        var tests = new (string Name, Action Body)[]
        {
            ("disabled operation buttons preserve native custom chrome", DisabledButtonsPreserveDarkChrome),
            ("runner scrollbar uses an explicit dark custom template", RunnerScrollbarUsesDarkTemplate)
        };
        var failures = 0;
        Console.WriteLine($"MRC PASS 4 GUI visual-polish harness — {tests.Length} tests");
        foreach (var test in tests)
        {
            try { test.Body(); Console.WriteLine($"PASS  {test.Name}"); }
            catch (Exception ex) { failures++; Console.WriteLine($"FAIL  {test.Name}\n      {ex.Message}"); }
        }
        Console.WriteLine();
        Console.WriteLine(failures == 0 ? $"PASS  all {tests.Length} PASS 4 GUI visual-polish tests" : $"FAIL  {failures} of {tests.Length} PASS 4 GUI visual-polish tests");
        return failures;
    }

    private static void DisabledButtonsPreserveDarkChrome()
    {
        var xaml = Xaml();
        Require(xaml.Contains("x:Key=\"ApplicationButtonStyle\"", StringComparison.Ordinal), "Native application button style is missing.");
        Require(xaml.Contains("Background=\"{TemplateBinding Background}\"", StringComparison.Ordinal), "Application button template does not preserve style background.");
        Require(xaml.Contains("Property=\"IsEnabled\" Value=\"False\"", StringComparison.Ordinal), "Application button template does not explicitly handle disabled state.");
        Require(xaml.Contains("CornerRadius=\"8\"", StringComparison.Ordinal), "Application buttons do not preserve the approved rounded native chrome.");
        Require(xaml.Contains("BasedOn=\"{StaticResource ApplicationButtonStyle}\"", StringComparison.Ordinal), "Bulk/card controls do not inherit native application button chrome.");
        Require(xaml.Contains("x:Key=\"CardActionButtonStyle\"", StringComparison.Ordinal)
                && xaml.Contains("BasedOn=\"{StaticResource CardActionButtonStyle}\"", StringComparison.Ordinal),
            "Runner card actions do not share a dedicated consistent action family.");
    }

    private static void RunnerScrollbarUsesDarkTemplate()
    {
        var app = File.ReadAllText(Path.Combine(RepoRoot(), "src", "MRC.Gui", "App.xaml"));
        var code = File.ReadAllText(Path.Combine(RepoRoot(), "src", "MRC.Gui", "MainWindow.xaml.cs"));
        Require(app.Contains("x:Key=\"DarkScrollBarStyle\"", StringComparison.Ordinal), "Explicit dark scrollbar style is missing.");
        Require(app.Contains("x:Name=\"PART_Track\"", StringComparison.Ordinal), "Dark scrollbar template does not provide WPF PART_Track.");
        Require(app.Contains("x:Key=\"DarkScrollThumbTemplate\"", StringComparison.Ordinal), "Dark scrollbar thumb template is missing.");
        Require(code.Contains("ApplyRunnerScrollBarStyle", StringComparison.Ordinal) && code.Contains("DarkScrollBarStyle", StringComparison.Ordinal), "Runner card list does not apply the explicit dark scrollbar template at runtime.");
    }

    private static string Xaml() => File.ReadAllText(Path.Combine(RepoRoot(), "src", "MRC.Gui", "MainWindow.xaml"));
    private static string RepoRoot() { var d = new DirectoryInfo(Directory.GetCurrentDirectory()); while (d is not null) { if (File.Exists(Path.Combine(d.FullName, "Auth", "0000_MasterAuth.md"))) return d.FullName; d = d.Parent; } throw new InvalidOperationException("Could not locate MRC repository root."); }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
