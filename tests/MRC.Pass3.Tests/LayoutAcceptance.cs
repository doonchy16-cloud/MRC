namespace MRC.Pass3.Tests;

internal static class LayoutAcceptance
{
    public static int Run()
    {
        var tests = new (string Name, Action Body)[]
        {
            ("window geometry matches locked authority", WindowGeometry),
            ("locked palette is present exactly", LockedPalette),
            ("canonical dashboard regions and controls exist", CanonicalRegions),
            ("runner table is dense and ellipsizes long identity text", DenseRunnerTable),
            ("state is represented by stripe glyph text and semantic color", MultiChannelState),
            ("refresh and animation use exactly two shared UI timers", TimerArchitecture),
            ("runner rows bind to stable presentation collections", StableBinding),
            ("search filters and state text remain keyboard-readable", AccessibilitySignals),
            ("narrow layout keeps boundary diagnostics without clipped toolbar copy", NarrowLayoutDiagnosticPolish),
            ("PASS 4 operations are not smuggled into PASS 3", Pass4ScopeFence)
        };

        var failures = 0;
        Console.WriteLine($"MRC PASS 3 dashboard-shell harness — {tests.Length} tests");
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

        Console.WriteLine(failures == 0
            ? $"PASS  all {tests.Length} PASS 3 dashboard-shell tests"
            : $"FAIL  {failures} of {tests.Length} PASS 3 dashboard-shell tests");
        Console.WriteLine();
        return failures;
    }

    private static void WindowGeometry()
    {
        var xaml = Xaml();
        Require(xaml.Contains("Width=\"1180\"", StringComparison.Ordinal), "Default width must be 1180.");
        Require(xaml.Contains("Height=\"760\"", StringComparison.Ordinal), "Default height must be 760.");
        Require(xaml.Contains("MinWidth=\"900\"", StringComparison.Ordinal), "Minimum width must be 900.");
        Require(xaml.Contains("MinHeight=\"560\"", StringComparison.Ordinal), "Minimum height must be 560.");
    }

    private static void LockedPalette()
    {
        var xaml = Xaml();
        foreach (var color in new[]
        {
            "#0B0F14", "#111821", "#18212B", "#263342",
            "#F4F7FA", "#AAB6C3", "#667381", "#35D9FF", "#6BE6FF", "#74A9D8", "#C792EA",
            "#39E58C", "#FFD166", "#FF5C6C", "#FF8A3D", "#4CA7FF", "#B48CFF",
            "#16222D", "#17344A"
        })
        {
            Require(xaml.Contains(color, StringComparison.OrdinalIgnoreCase), $"Locked palette color {color} is missing.");
        }
    }

    private static void CanonicalRegions()
    {
        var xaml = Xaml();
        foreach (var marker in new[]
        {
            "MAIN RUNNER CONTROL", "Machine", "Runner root",
            "TOTAL", "IDLE", "BUSY", "OFF", "ERROR",
            "Search runners", "ALL", "STATUS", "RUNNER NAME", "REPOSITORY", "STATE", "CONTROL",
            "RefreshStatusValue"
        })
        {
            Require(xaml.Contains(marker, StringComparison.OrdinalIgnoreCase), $"Canonical UI marker '{marker}' is missing.");
        }
    }

    private static void DenseRunnerTable()
    {
        var xaml = Xaml();
        Require(xaml.Contains("Height=\"42\"", StringComparison.Ordinal), "Runner row height must target 42 px.");
        Require(xaml.Contains("TextTrimming=\"CharacterEllipsis\"", StringComparison.Ordinal), "Long identity text must ellipsize.");
        foreach (var width in new[] { "Width=\"55\"", "Width=\"250\"", "Width=\"100\"", "Width=\"110\"" })
        {
            Require(xaml.Contains(width, StringComparison.Ordinal), $"Canonical table width marker {width} is missing.");
        }
    }

    private static void MultiChannelState()
    {
        var xaml = Xaml();
        Require(xaml.Contains("StateStripe", StringComparison.Ordinal), "State-colored stripe is missing.");
        Require(xaml.Contains("Glyph", StringComparison.Ordinal), "State glyph binding is missing.");
        Require(xaml.Contains("StateText", StringComparison.Ordinal), "State text binding is missing.");
        foreach (var state in new[] { "IDLE", "BUSY", "OFF", "ERROR", "STARTING", "STOPPING" })
        {
            Require(xaml.Contains(state, StringComparison.Ordinal), $"Visual handling for {state} is missing.");
        }
    }

    private static void TimerArchitecture()
    {
        var code = CodeBehind();
        var rows = File.ReadAllText(Path.Combine(RepoRoot(), "src", "MRC.Gui", "Presentation", "RunnerRowViewModel.cs"));
        Require(code.Contains("_refreshTimer", StringComparison.Ordinal), "Shared refresh timer is missing.");
        Require(code.Contains("_animationTimer", StringComparison.Ordinal), "Shared animation timer is missing.");
        Require(code.Contains("TimeSpan.FromSeconds(3)", StringComparison.Ordinal), "Refresh cadence must be approximately three seconds.");
        Require(Count(code, "new DispatcherTimer") == 2, "MainWindow must create exactly one refresh timer and one shared animation timer.");
        Require(!rows.Contains("DispatcherTimer", StringComparison.Ordinal), "Runner rows must never allocate timers.");
    }

    private static void StableBinding()
    {
        var xaml = Xaml();
        Require(xaml.Contains("ItemsSource=\"{Binding VisibleRows}\"", StringComparison.Ordinal), "Rows must bind to VisibleRows.");
        Require(xaml.Contains("Text=\"{Binding RunnerName}\"", StringComparison.Ordinal), "Runner name binding is missing.");
        Require(xaml.Contains("Text=\"{Binding RepositoryName}\"", StringComparison.Ordinal), "Repository binding is missing.");
        Require(xaml.Contains("Text=\"{Binding StateText}\"", StringComparison.Ordinal), "State text binding is missing.");
    }

    private static void AccessibilitySignals()
    {
        var xaml = Xaml();
        Require(xaml.Contains("x:Name=\"SearchBox\"", StringComparison.Ordinal), "Search box must be named for interaction/testing.");
        Require(xaml.Contains("AutomationProperties.Name", StringComparison.Ordinal), "Accessible automation names are missing.");
        foreach (var filter in new[] { "ALL", "IDLE", "BUSY", "OFF", "ERROR" })
        {
            Require(xaml.Contains($"Tag=\"{filter}\"", StringComparison.Ordinal), $"Filter {filter} must be directly selectable.");
        }
    }

    private static void NarrowLayoutDiagnosticPolish()
    {
        var xaml = Xaml();
        var code = CodeBehind();
        Require(!xaml.Contains("x:Name=\"DetailsValue\"", StringComparison.Ordinal),
            "Inline diagnostics remain in the crowded search/filter toolbar and can visibly truncate at minimum width.");
        Require(code.Contains("AutomationProperties.SetHelpText(BoundaryValue", StringComparison.Ordinal),
            "Full boundary diagnostics must remain available as accessible help text on the authorization badge.");
        Require(code.Contains("BoundaryValue.ToolTip", StringComparison.Ordinal),
            "Full boundary diagnostics must remain available by hover on the authorization badge.");
    }

    private static void Pass4ScopeFence()
    {
        var xaml = Xaml();
        var code = CodeBehind();
        Require(!xaml.Contains("TURN ALL ON", StringComparison.OrdinalIgnoreCase), "PASS 4 bulk ON leaked into PASS 3.");
        Require(!xaml.Contains("TURN ALL OFF", StringComparison.OrdinalIgnoreCase), "PASS 4 bulk OFF leaked into PASS 3.");
        Require(!code.Contains("_engine.Start(", StringComparison.Ordinal), "PASS 4 per-runner start wiring leaked into PASS 3.");
        Require(!code.Contains("_engine.StopIdle(", StringComparison.Ordinal), "PASS 4 per-runner stop wiring leaked into PASS 3.");
    }

    private static string Xaml() => File.ReadAllText(Path.Combine(RepoRoot(), "src", "MRC.Gui", "MainWindow.xaml"));
    private static string CodeBehind() => File.ReadAllText(Path.Combine(RepoRoot(), "src", "MRC.Gui", "MainWindow.xaml.cs"));

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
