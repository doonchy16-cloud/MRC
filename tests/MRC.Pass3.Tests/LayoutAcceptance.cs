namespace MRC.Pass3.Tests;

internal static class LayoutAcceptance
{
    public static int Run()
    {
        var tests = new (string Name, Action Body)[]
        {
            ("window geometry matches locked authority", WindowGeometry),
            ("terminal palette and monospace identity are present", TerminalPalette),
            ("terminal console regions and controls exist", CanonicalRegions),
            ("runner table is dense and ellipsizes long identities", DenseRunnerTable),
            ("state has glyph text semantic color and visible intensity", MultiChannelState),
            ("refresh and animation use exactly two shared UI timers", TimerArchitecture),
            ("runner rows bind to stable presentation collections", StableBinding),
            ("search filters and state text remain keyboard-readable", AccessibilitySignals),
            ("system findings stay separate from managed runner rows", SystemFindingsSeparation),
            ("PASS4 controls preserve verified state and safety architecture", Pass4IntegrationFence)
        };
        var failures = 0;
        Console.WriteLine($"MRC PASS 3 terminal-shell harness — {tests.Length} tests");
        foreach (var test in tests)
        {
            try { test.Body(); Console.WriteLine($"PASS  {test.Name}"); }
            catch (Exception ex) { failures++; Console.WriteLine($"FAIL  {test.Name}\n      {ex.Message}"); }
        }
        Console.WriteLine(failures == 0 ? $"PASS  all {tests.Length} PASS 3 terminal-shell tests" : $"FAIL  {failures} of {tests.Length} PASS 3 terminal-shell tests");
        Console.WriteLine(); return failures;
    }

    private static void WindowGeometry()
    {
        var x = Xaml();
        foreach (var marker in new[] { "Width=\"1180\"", "Height=\"760\"", "MinWidth=\"900\"", "MinHeight=\"560\"" }) Require(x.Contains(marker), $"Missing geometry marker {marker}.");
    }

    private static void TerminalPalette()
    {
        var x = Xaml();
        foreach (var color in new[] { "#0B0F14", "#0E141B", "#121B24", "#25313D", "#F4F7FA", "#AAB6C3", "#667381", "#35D9FF", "#6BE6FF", "#74A9D8", "#C792EA", "#39E58C", "#FFD166", "#FF5C6C", "#FF8A3D", "#4CA7FF", "#B48CFF" }) Require(x.Contains(color, StringComparison.OrdinalIgnoreCase), $"Terminal palette color {color} is missing.");
        Require(x.Contains("FontFamily=\"Cascadia Mono, Consolas\""), "Locked monospace font stack is missing.");
    }

    private static void CanonicalRegions()
    {
        var x = Xaml();
        foreach (var marker in new[] { "MAIN RUNNER CONTROL", "HOST ", "ROOT ", "TOTAL ", "IDLE ", "BUSY ", "OFF ", "ERROR ", "TRANS ", "FilterPanel", "SearchBox", "STAT", "RUNNER", "REPOSITORY", "STATE", "CONTROL", "SystemFindingsPanel", "TerminalStatusFooter", "RefreshStatusValue" }) Require(x.Contains(marker, StringComparison.OrdinalIgnoreCase), $"Terminal UI marker '{marker}' is missing.");
        Require(!x.Contains("metric card", StringComparison.OrdinalIgnoreCase), "Legacy dashboard-card language remains.");
    }

    private static void DenseRunnerTable()
    {
        var x = Xaml();
        Require(x.Contains("Height=\"32\""), "Runner rows must target 32 px.");
        Require(x.Contains("TextTrimming=\"CharacterEllipsis\""), "Long identity text must ellipsize.");
        foreach (var width in new[] { "Width=\"48\"", "Width=\"235\"", "Width=\"95\"", "Width=\"108\"" }) Require(x.Contains(width), $"Compact table width {width} is missing.");
    }

    private static void MultiChannelState()
    {
        var x = Xaml();
        Require(x.Contains("Text=\"{Binding Glyph}\"") && x.Contains("Opacity=\"{Binding AnimationIntensity}\""), "State glyph/intensity binding is incomplete.");
        Require(x.Contains("Text=\"{Binding StateText}\""), "State text binding is missing.");
        foreach (var state in new[] { "IDLE", "BUSY", "OFF", "ERROR", "STARTING", "STOPPING" }) Require(x.Contains(state), $"Visual handling for {state} is missing.");
    }

    private static void TimerArchitecture()
    {
        var code = CodeBehind(); var rows = File.ReadAllText(Path.Combine(RepoRoot(), "src", "MRC.Gui", "Presentation", "RunnerRowViewModel.cs"));
        Require(code.Contains("_refreshTimer") && code.Contains("_animationTimer") && code.Contains("TimeSpan.FromSeconds(3)"), "Shared refresh/animation timers or refresh cadence are missing.");
        Require(Count(code, "new DispatcherTimer") == 2, "MainWindow must create exactly one refresh and one shared animation timer.");
        Require(!rows.Contains("DispatcherTimer"), "Runner rows must never allocate timers.");
    }

    private static void StableBinding()
    {
        var x = Xaml();
        foreach (var marker in new[] { "ItemsSource=\"{Binding VisibleRows}\"", "Text=\"{Binding RunnerName}\"", "Text=\"{Binding RepositoryName}\"", "Text=\"{Binding StateText}\"" }) Require(x.Contains(marker), $"Stable binding '{marker}' is missing.");
    }

    private static void AccessibilitySignals()
    {
        var x = Xaml();
        Require(x.Contains("AutomationProperties.Name"), "Accessible automation names are missing.");
        foreach (var filter in new[] { "ALL", "IDLE", "BUSY", "OFF", "ERROR" }) Require(x.Contains($"Tag=\"{filter}\""), $"Filter {filter} must be directly selectable.");
    }

    private static void SystemFindingsSeparation()
    {
        var x = Xaml(); var code = CodeBehind();
        Require(x.Contains("SystemFindingsPanel") && x.Contains("SystemFindingsSummary"), "External/unattributed system findings surface is missing.");
        Require(code.Contains("_engine.RefreshReport()") && code.Contains("_dashboard.ApplyRuntimeReport(report)"), "GUI does not consume managed rows plus separate system findings report.");
    }

    private static void Pass4IntegrationFence()
    {
        var x = Xaml(); var code = CodeBehind();
        Require(code.Contains("new RunnerOperationsService(_engine)"), "Controls must route through RunnerOperationsService.");
        Require(!code.Contains("_engine.Start(") && !code.Contains("_engine.StopIdle(") && !code.Contains("_engine.ForceStopBusy("), "GUI bypasses operations safety orchestration.");
        Require(x.Contains("RunnerPrimaryControl_OnClick") && x.Contains("RunnerMoreControl_OnClick"), "Primary/secondary runner controls are not integrated into the table.");
    }

    private static string Xaml()=>File.ReadAllText(Path.Combine(RepoRoot(),"src","MRC.Gui","MainWindow.xaml"));
    private static string CodeBehind()=>File.ReadAllText(Path.Combine(RepoRoot(),"src","MRC.Gui","MainWindow.xaml.cs"));
    private static string RepoRoot(){var d=new DirectoryInfo(Directory.GetCurrentDirectory());while(d is not null){if(File.Exists(Path.Combine(d.FullName,"Auth","0000_MasterAuth.md")))return d.FullName;d=d.Parent;}throw new InvalidOperationException("Could not locate MRC repository root.");}
    private static int Count(string text,string value){var count=0;var i=0;while((i=text.IndexOf(value,i,StringComparison.Ordinal))>=0){count++;i+=value.Length;}return count;}
    private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
}
