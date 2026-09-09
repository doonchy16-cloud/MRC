namespace MRC.Pass4.Tests;

internal static class GuiVisualPolishAcceptance
{
    public static int Run()
    {
        var tests = new (string Name, Action Body)[]
        {
            ("disabled operation buttons preserve dark custom chrome", DisabledButtonsPreserveDarkChrome),
            ("runner scrollbar uses an explicit dark custom template", RunnerScrollbarUsesDarkTemplate)
        };

        var failures = 0;
        Console.WriteLine($"MRC PASS 4 GUI visual-polish harness — {tests.Length} tests");
        foreach (var test in tests)
        {
            try
            {
                test.Body();
                Console.WriteLine($"PASS  {test.Name}");
            }
            catch (Exception ex)
            {
                failures++;
                Console.WriteLine($"FAIL  {test.Name}");
                Console.WriteLine($"      {ex.Message}");
            }
        }

        Console.WriteLine();
        Console.WriteLine(failures == 0
            ? $"PASS  all {tests.Length} PASS 4 GUI visual-polish tests"
            : $"FAIL  {failures} of {tests.Length} PASS 4 GUI visual-polish tests");
        return failures;
    }

    private static void DisabledButtonsPreserveDarkChrome()
    {
        var xaml = Xaml();
        Require(xaml.Contains("x:Key=\"DarkButtonTemplate\"", StringComparison.Ordinal),
            "No custom dark button template exists; disabled WPF buttons render bright system chrome.");
        Require(xaml.Contains("Background=\"{TemplateBinding Background}\"", StringComparison.Ordinal),
            "Dark button template does not preserve the style background.");
        Require(xaml.Contains("Property=\"IsEnabled\" Value=\"False\"", StringComparison.Ordinal),
            "Dark button template does not explicitly handle disabled state.");
        Require(Count(xaml, "Template\" Value=\"{StaticResource DarkButtonTemplate}\"") >= 2,
            "Both filter/bulk and row-control button families must use the dark template.");
    }

    private static void RunnerScrollbarUsesDarkTemplate()
    {
        var xaml = Xaml();
        Require(xaml.Contains("x:Key=\"DarkScrollBarStyle\"", StringComparison.Ordinal),
            "Runner scrollbar still uses raw Windows system chrome.");
        Require(xaml.Contains("x:Name=\"PART_Track\"", StringComparison.Ordinal),
            "Dark scrollbar template does not provide the required WPF track part.");
        Require(xaml.Contains("x:Key=\"DarkScrollThumbTemplate\"", StringComparison.Ordinal),
            "Dark scrollbar thumb template is missing.");
        Require(xaml.Contains("Style=\"{StaticResource DarkScrollBarStyle}\"", StringComparison.Ordinal),
            "Runner ListView does not explicitly apply the dark scrollbar style.");
    }

    private static string Xaml() => File.ReadAllText(Path.Combine(RepoRoot(), "src", "MRC.Gui", "MainWindow.xaml"));

    private static string RepoRoot()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Auth", "0000_MasterAuth.md"))) return current.FullName;
            current = current.Parent;
        }
        throw new InvalidOperationException("Could not locate MRC repository root.");
    }

    private static int Count(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }
        return count;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
