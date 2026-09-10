namespace MRC.Pass3.Tests;

internal static class LayoutAcceptance
{
    public static int Run()
    {
        var tests = new (string Name, Action Body)[]
        {
            ("window geometry matches locked authority", WindowGeometry),
            ("native control-room palette and selective monospace identity are present", ControlRoomPalette),
            ("command center regions and controls exist", CanonicalRegions),
            ("runner cards are readable responsive and ellipsize long identities", ResponsiveRunnerCards),
            ("state has glyph text semantic color and visible intensity", MultiChannelState),
            ("refresh and animation use exactly two shared UI timers", TimerArchitecture),
            ("runner cards bind to stable presentation collections", StableBinding),
            ("search filters and state text remain keyboard-readable", AccessibilitySignals),
            ("system findings stay separate from managed runner cards", SystemFindingsSeparation),
            ("PASS4 controls preserve verified state and safety architecture", Pass4IntegrationFence)
        };
        var failures = 0;
        Console.WriteLine($"MRC PASS 3 command-center harness — {tests.Length} tests");
        foreach (var test in tests)
        {
            try { test.Body(); Console.WriteLine($"PASS  {test.Name}"); }
            catch (Exception ex) { failures++; Console.WriteLine($"FAIL  {test.Name}\n      {ex.Message}"); }
        }
        Console.WriteLine(failures == 0 ? $"PASS  all {tests.Length} PASS 3 command-center tests" : $"FAIL  {failures} of {tests.Length} PASS 3 command-center tests");
        Console.WriteLine(); return failures;
    }

    private static void WindowGeometry()
    {
        var x = Xaml();
        foreach (var marker in new[] { "Width=\"1200\"", "Height=\"760\"", "MinWidth=\"900\"", "MinHeight=\"560\"" })
            Require(x.Contains(marker), $"Missing geometry marker {marker}.");
    }

    private static void ControlRoomPalette()
    {
        var x = Xaml();
        foreach (var color in new[]
                 {
                     "#071015", "#0B141B", "#111C25", "#253542", "#F7F9FB", "#B2BDC7",
                     "#728391", "#43DDF8", "#73CFE4", "#8FB1CC", "#D09AF4", "#35E08A",
                     "#FFD166", "#FF6474", "#FF934F", "#55AFFF", "#BA92FF", "#FFC857"
                 })
            Require(x.Contains(color, StringComparison.OrdinalIgnoreCase), $"Command Center palette color {color} is missing.");

        Require(x.Contains("FontFamily=\"Segoe UI\"", StringComparison.OrdinalIgnoreCase),
            "Primary native application font identity is missing.");
        Require(x.Contains("FontFamily=\"Cascadia Mono, Consolas\"", StringComparison.OrdinalIgnoreCase),
            "Selective technical monospace font stack is missing.");
    }

    private static void CanonicalRegions()
    {
        var x = Xaml();
        foreach (var marker in new[]
                 {
                     "MAIN RUNNER CONTROL", "HOST", "ROOT", "TOTAL", "IDLE", "BUSY", "OFF", "ERROR",
                     "TRANSITION", "FilterPanel", "SearchBox", "RunnerCardSurface", "RunnerList",
                     "SystemFindingsPanel", "RefreshStatusValue"
                 })
            Require(x.Contains(marker, StringComparison.OrdinalIgnoreCase), $"Command Center marker '{marker}' is missing.");

        Require(!x.Contains("RunnerTableSurface", StringComparison.Ordinal),
            "Superseded runner table surface remains active.");
        Require(x.Contains("Content=\"TURN ALL ON\"", StringComparison.Ordinal)
                && x.Contains("Content=\"TURN ALL OFF\"", StringComparison.Ordinal),
            "Native bulk controls are missing.");
    }

    private static void ResponsiveRunnerCards()
    {
        var x = Xaml();
        var code = CodeBehind();
        var dashboard = File.ReadAllText(Path.Combine(RepoRoot(), "src", "MRC.Gui", "Presentation", "RunnerDashboardViewModel.cs"));

        Require(x.Contains("x:Key=\"RunnerCardStyle\"", StringComparison.Ordinal)
                && (x.Contains("Property=\"CornerRadius\" Value=\"14\"", StringComparison.Ordinal)
                    || x.Contains("CornerRadius=\"14\"", StringComparison.Ordinal)),
            "Purpose-built runner card surface is missing.");
        Require(x.Contains("<UniformGrid Columns=\"{Binding CardColumnCount}\"", StringComparison.Ordinal),
            "Runner cards are not hosted in the adaptive multi-column panel.");
        Require(dashboard.Contains("CardColumnCount", StringComparison.Ordinal)
                && dashboard.Contains("Math.Clamp(value, 1, 4)", StringComparison.Ordinal)
                && code.Contains("ApplyResponsiveLayout", StringComparison.Ordinal)
                && code.Contains("ActualWidth >= 1600", StringComparison.Ordinal)
                && code.Contains("? 4", StringComparison.Ordinal)
                && code.Contains("ActualWidth >= 1100", StringComparison.Ordinal)
                && code.Contains("? 3", StringComparison.Ordinal)
                && code.Contains("ActualWidth >= 800", StringComparison.Ordinal)
                && code.Contains("? 2", StringComparison.Ordinal),
            "4/3/2/1 responsive layout authority is incomplete.");
        Require(x.Contains("Text=\"{Binding RunnerName}\"", StringComparison.Ordinal)
                && x.Contains("FontSize=\"19\"", StringComparison.Ordinal),
            "Runner identity is not presented at the reference-first card hierarchy.");
        Require(x.Contains("TextTrimming=\"CharacterEllipsis\"", StringComparison.Ordinal),
            "Long identity text must ellipsize.");
        Require(x.Contains("Property=\"MinHeight\" Value=\"44\"", StringComparison.Ordinal)
                || x.Contains("MinHeight=\"44\"", StringComparison.Ordinal),
            "Card action controls do not preserve the 44 px reference hit-target height.");
        Require(x.Contains("Property=\"MaxHeight\" Value=\"210\"", StringComparison.Ordinal)
                || x.Contains("MaxHeight=\"210\"", StringComparison.Ordinal),
            "Runner cards are not bounded against giant large-screen stretching.");
        Require(!code.Contains("RootSurface.LayoutTransform", StringComparison.Ordinal),
            "Superseded whole-window scaling remains active.");
    }

    private static void MultiChannelState()
    {
        var x = Xaml();
        Require(x.Contains("Text=\"{Binding Glyph}\"") && x.Contains("Opacity=\"{Binding AnimationIntensity}\""),
            "State glyph/intensity binding is incomplete.");
        Require(x.Contains("Text=\"{Binding StateText}\""), "State text binding is missing.");
        Require(x.Contains("x:Key=\"StateBadgeStyle\"", StringComparison.Ordinal), "State badge visual language is missing.");
        foreach (var state in new[] { "IDLE", "BUSY", "OFF", "ERROR", "STARTING", "STOPPING" })
            Require(x.Contains(state), $"Visual handling for {state} is missing.");
    }

    private static void TimerArchitecture()
    {
        var code = CodeBehind();
        var rows = File.ReadAllText(Path.Combine(RepoRoot(), "src", "MRC.Gui", "Presentation", "RunnerRowViewModel.cs"));
        Require(code.Contains("_refreshTimer") && code.Contains("_animationTimer") && code.Contains("TimeSpan.FromSeconds(3)"),
            "Shared refresh/animation timers or refresh cadence are missing.");
        Require(code.Contains("TimeSpan.FromMilliseconds(50)"), "20 FPS shared animation clock target is missing.");
        Require(Count(code, "new DispatcherTimer") == 2, "MainWindow must create exactly one refresh and one shared animation timer.");
        Require(!rows.Contains("DispatcherTimer"), "Runner cards must never allocate per-row timers.");
    }

    private static void StableBinding()
    {
        var x = Xaml();
        foreach (var marker in new[]
                 {
                     "ItemsSource=\"{Binding VisibleRows}\"", "Text=\"{Binding RunnerName}\"",
                     "Text=\"{Binding RepositoryName}\"", "Text=\"{Binding StateText}\""
                 })
            Require(x.Contains(marker), $"Stable binding '{marker}' is missing.");
    }

    private static void AccessibilitySignals()
    {
        var x = Xaml();
        var code = CodeBehind();
        Require(x.Contains("AutomationProperties.Name"), "Accessible automation names are missing.");
        foreach (var filter in new[] { "ALL", "IDLE", "BUSY", "OFF", "ERROR" })
            Require(x.Contains($"Tag=\"{filter}\""), $"Filter {filter} must be directly selectable.");
        Require(x.Contains("Text=\"SEARCH\"", StringComparison.Ordinal)
                && x.Contains("ToolTip=\"Press / to focus search\"", StringComparison.Ordinal),
            "Search label or keyboard-shortcut help is missing.");
        Require(code.Contains("SearchBox.Focus()", StringComparison.Ordinal),
            "Slash shortcut does not actually focus search.");
    }

    private static void SystemFindingsSeparation()
    {
        var x = Xaml(); var code = CodeBehind();
        Require(x.Contains("SystemFindingsPanel") && x.Contains("SystemFindingsSummary"),
            "External/unattributed system findings surface is missing.");
        Require(x.Contains("RunnerCardSurface", StringComparison.Ordinal),
            "Managed control surface is missing, so findings separation cannot be established.");
        Require(code.Contains("_engine.RefreshReport()") && code.Contains("_dashboard.ApplyRuntimeReport(report)"),
            "GUI does not consume managed rows plus separate system findings report.");
    }

    private static void Pass4IntegrationFence()
    {
        var x = Xaml(); var code = CodeBehind();
        Require(code.Contains("new RunnerOperationsService(_engine)"), "Controls must route through RunnerOperationsService.");
        Require(!code.Contains("_engine.Start(")
                && !code.Contains("_engine.StopIdle(")
                && !code.Contains("_engine.Restart(")
                && !code.Contains("_engine.ForceStopBusy("),
            "GUI bypasses operations safety orchestration.");

        Require(x.Contains("Content=\"START\"") && x.Contains("IsEnabled=\"{Binding CanStart}\"") && x.Contains("RunnerStart_OnClick"),
            "Explicit START control is not integrated with state authority.");
        Require(x.Contains("Content=\"STOP\"") && x.Contains("IsEnabled=\"{Binding CanStop}\"") && x.Contains("RunnerStop_OnClick"),
            "Explicit STOP control is not integrated with state authority.");
        Require(x.Contains("Content=\"RESTART\"") && x.Contains("IsEnabled=\"{Binding CanRestart}\"") && x.Contains("RunnerRestart_OnClick"),
            "Explicit RESTART control is not integrated with state authority.");
        Require(x.Contains("Content=\"DETAILS\"") && x.Contains("RunnerDetails_OnClick"),
            "Explicit DETAILS diagnostics control is missing.");
        Require(x.Contains("Content=\"FORCE STOP\"") && x.Contains("RunnerMoreControl_OnClick"),
            "Separate BUSY force-stop control is missing.");
    }

    private static string Xaml()=>File.ReadAllText(Path.Combine(RepoRoot(),"src","MRC.Gui","MainWindow.xaml"));
    private static string CodeBehind()=>File.ReadAllText(Path.Combine(RepoRoot(),"src","MRC.Gui","MainWindow.xaml.cs"));
    private static string RepoRoot(){var d=new DirectoryInfo(Directory.GetCurrentDirectory());while(d is not null){if(File.Exists(Path.Combine(d.FullName,"Auth","0000_MasterAuth.md")))return d.FullName;d=d.Parent;}throw new InvalidOperationException("Could not locate MRC repository root.");}
    private static int Count(string text,string value){var count=0;var i=0;while((i=text.IndexOf(value,i,StringComparison.Ordinal))>=0){count++;i+=value.Length;}return count;}
    private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
}
