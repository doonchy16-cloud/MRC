namespace MRC.Pass4.Tests;

internal static class GuiVisualPolishAcceptance
{
    public static int Run()
    {
        var tests = new (string Name, Action Body)[]
        {
            ("disabled operation buttons preserve terminal custom chrome", DisabledButtonsPreserveTerminalChrome),
            ("runner scrollbar stays narrow and dark", RunnerScrollbarUsesTerminalStyle)
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

    private static void DisabledButtonsPreserveTerminalChrome()
    {
        var xaml = Xaml();
        Require(xaml.Contains("x:Key=\"TerminalButtonTemplate\"", StringComparison.Ordinal),
            "Terminal custom button template is missing.");
        Require(xaml.Contains("Background=\"{TemplateBinding Background}\"", StringComparison.Ordinal),
            "Terminal button template does not preserve the style background.");
        Require(xaml.Contains("Property=\"IsEnabled\" Value=\"False\"", StringComparison.Ordinal),
            "Terminal button template does not explicitly handle disabled state.");
        Require(Count(xaml, "{StaticResource TerminalButtonTemplate}") >= 2,
            "Bulk/filter and row-control button families must share the terminal template.");
        Require(xaml.Contains("CornerRadius=\"0\"", StringComparison.Ordinal),
            "Terminal controls must preserve the approved flat, non-card chrome.");
    }

    private static void RunnerScrollbarUsesTerminalStyle()
    {
        var xaml = Xaml();
        Require(xaml.Contains("<Style TargetType=\"{x:Type ScrollBar}\">", StringComparison.Ordinal),
            "Runner scrollbar does not have an explicit terminal style.");
        Require(xaml.Contains("<Setter Property=\"Width\" Value=\"8\"", StringComparison.Ordinal),
            "Terminal scrollbar must remain narrow at 8 px.");
        Require(xaml.Contains("<Setter Property=\"Background\" Value=\"{StaticResource BackgroundBrush}\"", StringComparison.Ordinal),
            "Terminal scrollbar background is not bound to the near-black shell.");
        Require(xaml.Contains("<Setter Property=\"Foreground\" Value=\"{StaticResource MutedBrush}\"", StringComparison.Ordinal),
            "Terminal scrollbar foreground is not bound to the muted operational palette.");
        Require(xaml.Contains("ScrollViewer.VerticalScrollBarVisibility=\"Auto\"", StringComparison.Ordinal),
            "Runner table is not explicitly using the styled vertical scrollbar path.");
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
