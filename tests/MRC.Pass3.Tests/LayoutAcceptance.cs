namespace MRC.Pass3.Tests;

internal static class LayoutAcceptance
{
    public static int Run()
    {
        var tests = new (string Name, Action Body)[]
        {
            ("window geometry matches locked authority", WindowGeometry),
            ("terminal palette and typography are present", TerminalPalette),
            ("canonical terminal regions and controls exist", CanonicalRegions),
            ("runner table is compact and ellipsizes long identity text", DenseRunnerTable),
            ("state uses glyph text and semantic color", MultiChannelState),
            ("refresh and animation use exactly two shared UI timers", TimerArchitecture),
            ("runner rows bind to stable presentation collections", StableBinding),
            ("search filters and state text remain keyboard-readable", AccessibilitySignals),
            ("narrow layout keeps diagnostics and system findings available", NarrowLayoutDiagnosticPolish),
            ("PASS 4 controls preserve the verified safety architecture", Pass4IntegrationFence)
        };

        var failures = 0;
        Console.WriteLine($"MRC PASS 3 terminal-shell harness — {tests.Length} tests");
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
            ? $"PASS  all {tests.Length} PASS 3 terminal-shell tests"
            : $"FAIL  {failures} of {tests.Length} PASS 3 terminal-shell tests");
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

    private static void TerminalPalette()
    {
        var xaml = Xaml();
        foreach (var color in new[]
        {
            "#0B0F14", "#0E141B", "#121B24", "#25313D",
            "#F4F7FA", "#AAB6C3", "#667381", "#35D9FF", "#6BE6FF", "#74A9D8", "#C792EA",
            "#39E58C", "#FFD166", "#FF5C6C", "#FF8A3D", "#4CA7FF", "#B48CFF"
        })
        {
            Require(xaml.Contains(color, StringComparison.OrdinalIgnoreCase), $"Terminal palette color {color} is missing.");
        }
        Require(xaml.Contains("Cascadia Mono", StringComparison.OrdinalIgnoreCase)
                && xaml.Contains("Consolas", StringComparison.OrdinalIgnoreCase),
            "Terminal monospace font stack is missing.");
        Require(!xaml.Contains("FontFamily=\"Segoe UI\"", StringComparison.OrdinalIgnoreCase),
            "Legacy dashboard typography is still present.");
    }

    private static void CanonicalRegions()
    {
        var xaml = Xaml();
        foreach (var marker in new[]
        {
            "MAIN RUNNER CONTROL", "HOST", "ROOT", "AUTHORIZED",
            "TOTAL", "IDLE", "BUSY", "OFF", "ERROR", "TRANS",
            "InlineStatusStrip", "RunnerTableSurface", "SystemFindingsPanel", "TerminalStatusFooter",
            "Search runners", "ALL", "STAT", "RUNNER", "REPOSITORY", "STATE", "CONTROL",
            "RefreshStatusValue"
        })
        {
            Require(xaml.Contains(marker, StringComparison.OrdinalIgnoreCase), $"Canonical terminal UI marker '{marker}' is missing.");
        }
    }

    private static void DenseRunnerTable()
    {
        var xaml = Xaml();
        Require(xaml.Contains("<Setter Property=\"Height\" Value=\"32\"", StringComparison.Ordinal),
            "Runner row height must be the approved compact 32 px.");
        Require(!xaml.Contains("<Setter Property=\"Height\" Value=\"42\"", StringComparison.Ordinal),
            "Superseded 42 px runner rows remain.");
        Require(xaml.Contains("TextTrimming=\"CharacterEllipsis\"", StringComparison.Ordinal), "Long identity text must ellipsize.");
        foreach (var width in new[] { "Width=\"48\"", "Width=\"235\"", "Width=\"95\"", "Width=\"108\"" })
        {
            Require(xaml.Contains(width, StringComparison.Ordinal), $"Approved terminal table width marker {width} is missing.");
        }
    }

    private static void MultiChannelState()
    {
        var xaml = Xaml();
        Require(xaml.Contains("{Binding Glyph}", StringComparison.Ordinal), "State glyph binding is missing.");
        Require(xaml.Contains("{Binding StateText}", StringComparison.Ordinal), "State text binding is missing.");
        Require(xaml.Contains("Opacity=\"{Binding AnimationIntensity}\"", StringComparison.Ordinal),
            "Shared-clock animation intensity is not rendered on the state glyph.");
        foreach (var brush in new[] { "IdleBrush", "BusyBrush", "OffBrush", "ErrorBrush", "StartingBrush", "StoppingBrush" })
        {
            Require(xaml.Contains(brush, StringComparison.Ordinal), $"Semantic state color {brush} is missing.");
        }
    }

    private static void TimerArchitecture()
    {
        var code = CodeBehind();
        var rows = File.ReadAllText(Path.Combine(RepoRoot(), "src", "MRC.Gui", "Presentation", "RunnerRowViewModel.cs"));
        Require(code.Contains("_refreshTimer", StringComparison.Ordinal), "Shared refresh timer is missing.");
        Require(code.Contains("_animationTimer", StringComparison.Ordinal), "Shared animation timer is missing.");
        Require(code.Contains("TimeSpan.FromSeconds(3)", StringComparison.Ordinal), "Refresh cadence must be approximately three seconds.");
        Require(code.Contains("TimeSpan.FromMilliseconds(55)", StringComparison.Ordinal), "Shared animation tick must remain 55 ms.");
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
        Require(code.Contains("AutomationProperties.SetHelpText(BoundaryValue", StringComparison.Ordinal),
            "Full boundary diagnostics must remain available as accessible help text on the authorization badge.");
        Require(code.Contains("BoundaryValue.ToolTip", StringComparison.Ordinal),
            "Full boundary diagnostics must remain available by hover on the authorization badge.");
        Require(xaml.Contains("SystemFindingsSummary", StringComparison.Ordinal),
            "External/system findings are not available in the compact layout.");
        Require(xaml.Contains("TextTrimming=\"CharacterEllipsis\"", StringComparison.Ordinal),
            "Compact layout does not protect long text at minimum width.");
    }

    private static void Pass4IntegrationFence()
    {
        var xaml = Xaml();
        var code = CodeBehind();
        Require(code.Contains("new RunnerOperationsService(_engine)", StringComparison.Ordinal),
            "PASS 4 controls must route through RunnerOperationsService rather than replacing PASS 3 state truth.");
        Require(!code.Contains("_engine.Start(", StringComparison.Ordinal), "GUI bypasses PASS 4 safety orchestration for runner start.");
        Require(!code.Contains("_engine.StopIdle(", StringComparison.Ordinal), "GUI bypasses PASS 4 safety orchestration for runner stop.");
        Require(!code.Contains("_engine.ForceStopBusy(", StringComparison.Ordinal), "GUI bypasses PASS 4 safety orchestration for force-stop.");
        Require(xaml.Contains("RunnerPrimaryControl_OnClick", StringComparison.Ordinal), "PASS 4 primary runner control is not integrated into the verified table.");
        Require(xaml.Contains("RunnerMoreControl_OnClick", StringComparison.Ordinal), "PASS 4 secondary BUSY path is not integrated into the verified table.");
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
