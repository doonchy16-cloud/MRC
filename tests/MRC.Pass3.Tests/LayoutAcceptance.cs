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
            ("state has text semantic color and visible shared intensity", MultiChannelState),
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
        var drawer = DrawerXaml();

        foreach (var marker in new[]
                 {
                     "MAIN RUNNER CONTROL", "HOST", "ROOT", "RunnerCardSurface", "RunnerList",
                     "SystemFindingsPanel", "RefreshStatusValue", "ControlDrawerHost"
                 })
            Require(x.Contains(marker, StringComparison.OrdinalIgnoreCase), $"Primary Command Center marker '{marker}' is missing.");

        foreach (var marker in new[]
                 {
                     "TOTAL", "IDLE", "BUSY", "OFF", "ERROR", "TRANSITION", "SearchBox",
                     "TURN ALL ON", "TURN ALL OFF", "REFRESH LOCAL TRUTH"
                 })
            Require(drawer.Contains(marker, StringComparison.OrdinalIgnoreCase), $"Control Drawer marker '{marker}' is missing.");

        foreach (var filter in new[] { "ALL", "IDLE", "BUSY", "OFF", "ERROR" })
            Require(drawer.Contains($"Tag=\"{filter}\"", StringComparison.Ordinal), $"Control Drawer filter '{filter}' is missing.");

        Require(!x.Contains("RunnerTableSurface", StringComparison.Ordinal),
            "Superseded runner table surface remains active.");
        Require(x.Contains("<controls:ControlDrawer x:Name=\"ControlDrawer\"", StringComparison.Ordinal)
                && x.Contains("TurnAllOnRequested=\"TurnAllOnButton_OnClick\"", StringComparison.Ordinal)
                && x.Contains("TurnAllOffRequested=\"TurnAllOffButton_OnClick\"", StringComparison.Ordinal),
            "MainWindow does not compose and route the secondary control drawer.");
        Require(x.Contains("<controls:RunnerControlCard", StringComparison.Ordinal)
                && x.Contains("ActionRequested=\"RunnerControlCard_OnActionRequested\"", StringComparison.Ordinal),
            "Command center no longer composes the active extracted runner card.");
    }

    private static void ResponsiveRunnerCards()
    {
        var x = Xaml();
        var card = CardXaml();
        var theme = ThemeXaml();
        var code = CodeBehind();
        var dashboard = File.ReadAllText(Path.Combine(RepoRoot(), "src", "MRC.Gui", "Presentation", "RunnerDashboardViewModel.cs"));
        var responsive = File.ReadAllText(Path.Combine(RepoRoot(), "src", "MRC.Gui", "Presentation", "RunnerResponsiveLayout.cs"));

        Require(theme.Contains("x:Key=\"ReplicaCardSurfaceStyle\"", StringComparison.Ordinal)
                && theme.Contains("CornerRadius\" Value=\"16\"", StringComparison.Ordinal),
            "Purpose-built compact runner card surface is missing.");
        Require(x.Contains("<UniformGrid Columns=\"{Binding CardColumnCount}\"", StringComparison.Ordinal),
            "Runner cards are not hosted in the adaptive multi-column panel.");
        Require(dashboard.Contains("CardColumnCount", StringComparison.Ordinal)
                && dashboard.Contains("Math.Clamp(value, 1, 4)", StringComparison.Ordinal)
                && code.Contains("ApplyResponsiveLayout", StringComparison.Ordinal)
                && code.Contains("RunnerResponsiveLayout.ColumnCountForWidth(ActualWidth)", StringComparison.Ordinal)
                && responsive.Contains("MinimumCardSlotWidth = ReferenceReplicaMetrics.MinimumCardSlotWidth", StringComparison.Ordinal)
                && responsive.Contains("ReferenceReplicaMetrics.ColumnCountForWidth(windowWidth)", StringComparison.Ordinal),
            "Safe-width 1-through-4 responsive layout authority is incomplete.");
        Require(card.Contains("Text=\"{Binding Row.RunnerName, ElementName=Root}\"", StringComparison.Ordinal)
                && card.Contains("FontSize=\"17\"", StringComparison.Ordinal),
            "Runner identity is not presented at the compact reference-first card hierarchy.");
        Require(card.Contains("TextTrimming=\"CharacterEllipsis\"", StringComparison.Ordinal),
            "Long identity text must ellipsize.");
        Require(card.Contains("Height=\"150\"", StringComparison.Ordinal)
                && card.Contains("x:Name=\"ThreeSlotActionRail\"", StringComparison.Ordinal)
                && card.Contains("Grid.Row=\"1\" Height=\"46\"", StringComparison.Ordinal),
            "Compact runner card does not preserve the locked 150 px card and 46 px action-rail geometry.");
        Require(!code.Contains("RootSurface.LayoutTransform", StringComparison.Ordinal),
            "Superseded whole-window scaling remains active.");
    }

    private static void MultiChannelState()
    {
        var card = CardXaml();
        var orb = OrbXaml();
        var theme = ThemeXaml();
        Require(card.Contains("<controls:StateOrb", StringComparison.Ordinal)
                && card.Contains("Intensity=\"{Binding Row.AnimationIntensity, ElementName=Root}\"", StringComparison.Ordinal)
                && orb.Contains("Opacity=\"{Binding Intensity, ElementName=Root}\"", StringComparison.Ordinal),
            "Active StateOrb does not render the shared AnimationIntensity signal.");
        Require(card.Contains("Text=\"{Binding Row.StateText, ElementName=Root}\"", StringComparison.Ordinal),
            "State text binding is missing from the active card.");
        Require(theme.Contains("x:Key=\"ReplicaStateBadgeStyle\"", StringComparison.Ordinal),
            "State badge visual language is missing.");
        foreach (var state in new[] { "IDLE", "BUSY", "OFF", "ERROR", "STARTING", "STOPPING" })
            Require(theme.Contains($"RunnerState.{state}", StringComparison.Ordinal), $"Visual handling for {state} is missing.");
    }

    private static void TimerArchitecture()
    {
        var code = CodeBehind();
        var rows = File.ReadAllText(Path.Combine(RepoRoot(), "src", "MRC.Gui", "Presentation", "RunnerRowViewModel.cs"));
        var cardCode = CardCode();
        var orbCode = OrbCode();
        Require(code.Contains("_refreshTimer") && code.Contains("_animationTimer") && code.Contains("TimeSpan.FromSeconds(3)"),
            "Shared refresh/animation timers or refresh cadence are missing.");
        Require(code.Contains("TimeSpan.FromMilliseconds(50)"), "20 FPS shared animation clock target is missing.");
        Require(Count(code, "new DispatcherTimer") == 2, "MainWindow must create exactly one refresh and one shared animation timer.");
        Require(!rows.Contains("DispatcherTimer") && !cardCode.Contains("DispatcherTimer") && !orbCode.Contains("DispatcherTimer"),
            "Runner presentation components must never allocate per-row/per-card/per-orb timers.");
    }

    private static void StableBinding()
    {
        var x = Xaml();
        var card = CardXaml();
        Require(x.Contains("ItemsSource=\"{Binding VisibleRows}\"", StringComparison.Ordinal),
            "Runner list is not bound to the stable visible-row collection.");
        foreach (var marker in new[]
                 {
                     "Text=\"{Binding Row.RunnerName, ElementName=Root}\"",
                     "Text=\"{Binding Row.RepositoryName, ElementName=Root, Mode=OneWay}\"",
                     "Text=\"{Binding Row.StateText, ElementName=Root}\""
                 })
            Require(card.Contains(marker), $"Stable extracted-card binding '{marker}' is missing.");
    }

    private static void AccessibilitySignals()
    {
        var x = Xaml();
        var drawer = DrawerXaml();
        var drawerCode = DrawerCode();
        var card = CardXaml();
        var code = CodeBehind();
        Require(x.Contains("AutomationProperties.Name")
                && drawer.Contains("AutomationProperties.Name")
                && card.Contains("AutomationProperties.Name"),
            "Accessible automation names are missing from window/drawer/card composition.");
        foreach (var filter in new[] { "ALL", "IDLE", "BUSY", "OFF", "ERROR" })
            Require(drawer.Contains($"Tag=\"{filter}\""), $"Filter {filter} must be directly selectable.");
        Require(drawer.Contains("Text=\"SEARCH\"", StringComparison.Ordinal)
                && drawer.Contains("ToolTip=\"Press / to focus search\"", StringComparison.Ordinal),
            "Search label or keyboard-shortcut help is missing from the control drawer.");
        Require(code.Contains("Key.OemQuestion", StringComparison.Ordinal)
                && code.Contains("OpenControlDrawer(focusSearch: true)", StringComparison.Ordinal)
                && code.Contains("ControlDrawer.FocusSearch()", StringComparison.Ordinal),
            "Slash shortcut does not open the drawer and delegate search focus.");
        Require(drawerCode.Contains("public void FocusSearch()", StringComparison.Ordinal)
                && drawerCode.Contains("SearchBox.Focus()", StringComparison.Ordinal)
                && drawerCode.Contains("SearchBox.SelectAll()", StringComparison.Ordinal),
            "ControlDrawer focus delegation does not actually focus and select the search field.");
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
        var x = Xaml();
        var card = CardXaml();
        var cardCode = CardCode();
        var code = CodeBehind();
        Require(code.Contains("new RunnerOperationsService(_engine)"), "Controls must route through RunnerOperationsService.");
        Require(!code.Contains("_engine.Start(")
                && !code.Contains("_engine.StopIdle(")
                && !code.Contains("_engine.Restart(")
                && !code.Contains("_engine.ForceStopBusy("),
            "GUI bypasses operations safety orchestration.");
        Require(!cardCode.Contains("RunnerOperationsService") && !cardCode.Contains("RunnerEngine"),
            "Extracted card owns runtime/lifecycle authority instead of emitting presentation actions.");

        Require(card.Contains("Value=\"START\"") && card.Contains("Row.CanStart") && cardCode.Contains("RunnerCardAction.Start"),
            "Explicit START control is not integrated with state authority.");
        Require(card.Contains("Content=\"STOP\"") && card.Contains("Row.CanStop") && cardCode.Contains("RunnerCardAction.Stop"),
            "Explicit STOP control is not integrated with state authority.");
        Require(card.Contains("Content=\"RESTART\"") && card.Contains("Row.CanRestart") && cardCode.Contains("RunnerCardAction.Restart"),
            "Explicit RESTART control is not integrated with state authority.");
        Require(card.Contains("ToolTip=\"DETAILS\"") && cardCode.Contains("RunnerCardAction.Details"),
            "DETAILS diagnostics affordance is missing from the active card.");
        Require(card.Contains("Value=\"FORCE STOP\"")
                && card.Contains("RunnerState.BUSY")
                && cardCode.Contains("RunnerCardAction.ForceStop"),
            "BUSY FORCE STOP action is missing from the state-authorized active card.");
        Require(x.Contains("ActionRequested=\"RunnerControlCard_OnActionRequested\"", StringComparison.Ordinal)
                && code.Contains("case RunnerCardAction.ForceStop", StringComparison.Ordinal)
                && code.Contains("ExecuteForceStopAsync(e.Row)", StringComparison.Ordinal),
            "Typed card actions are not routed through the guarded MainWindow operations boundary.");
    }

    private static string Xaml()=>File.ReadAllText(Path.Combine(RepoRoot(),"src","MRC.Gui","MainWindow.xaml"));
    private static string CodeBehind()=>File.ReadAllText(Path.Combine(RepoRoot(),"src","MRC.Gui","MainWindow.xaml.cs"));
    private static string DrawerXaml()=>File.ReadAllText(Path.Combine(RepoRoot(),"src","MRC.Gui","Controls","ControlDrawer.xaml"));
    private static string DrawerCode()=>File.ReadAllText(Path.Combine(RepoRoot(),"src","MRC.Gui","Controls","ControlDrawer.xaml.cs"));
    private static string CardXaml()=>File.ReadAllText(Path.Combine(RepoRoot(),"src","MRC.Gui","Controls","RunnerControlCard.xaml"));
    private static string CardCode()=>File.ReadAllText(Path.Combine(RepoRoot(),"src","MRC.Gui","Controls","RunnerControlCard.xaml.cs"));
    private static string OrbXaml()=>File.ReadAllText(Path.Combine(RepoRoot(),"src","MRC.Gui","Controls","StateOrb.xaml"));
    private static string OrbCode()=>File.ReadAllText(Path.Combine(RepoRoot(),"src","MRC.Gui","Controls","StateOrb.xaml.cs"));
    private static string ThemeXaml()=>File.ReadAllText(Path.Combine(RepoRoot(),"src","MRC.Gui","Themes","ReferenceReplica.xaml"));
    private static string RepoRoot(){var d=new DirectoryInfo(Directory.GetCurrentDirectory());while(d is not null){if(File.Exists(Path.Combine(d.FullName,"Auth","0000_MasterAuth.md")))return d.FullName;d=d.Parent;}throw new InvalidOperationException("Could not locate MRC repository root.");}
    private static int Count(string text,string value){var count=0;var i=0;while((i=text.IndexOf(value,i,StringComparison.Ordinal))>=0){count++;i+=value.Length;}return count;}
    private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
}
