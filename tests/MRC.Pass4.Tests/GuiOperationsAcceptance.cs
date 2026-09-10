namespace MRC.Pass4.Tests;

internal static class GuiOperationsAcceptance
{
    public static int Run()
    {
        var tests = new (string Name, Action Body)[]
        {
            ("bulk ON/OFF controls exist in the native application command bar", BulkControlsExist),
            ("explicit per-runner controls route through shared operations authority", PerRunnerControlsRouteSafely),
            ("BUSY force-stop remains separate and explicitly confirmed", BusyForceStopIsSecondaryAndConfirmed),
            ("TURN ALL OFF confirmation states exact IDLE stop and BUSY remain impact", BulkOffConfirmationStatesExactImpact),
            ("preview mode cannot invoke any runner lifecycle operation", PreviewModeCannotOperate),
            ("operation failures surface clear visible error feedback", OperationFailuresAreVisible),
            ("bulk/search/filter controls occupy distinct responsive command-bar columns", OperationsLayoutIsResponsive),
            ("inherited CI retains canonical rendered-preview evidence coverage", RenderGateExists)
        };

        var failures = 0;
        Console.WriteLine($"MRC PASS 4 GUI operations harness — {tests.Length} tests");
        foreach (var test in tests)
        {
            try { test.Body(); Console.WriteLine($"PASS  {test.Name}"); }
            catch (Exception ex) { failures++; Console.WriteLine($"FAIL  {test.Name}\n      {ex.Message}"); }
        }
        Console.WriteLine();
        Console.WriteLine(failures == 0 ? $"PASS  all {tests.Length} PASS 4 GUI operations tests" : $"FAIL  {failures} of {tests.Length} PASS 4 GUI operations tests");
        return failures;
    }

    private static void BulkControlsExist()
    {
        var xaml = Xaml(); var code = Code();
        Require(xaml.Contains("x:Name=\"OperationsPanel\""), "Operations command bar is missing.");
        Require(xaml.Contains("x:Name=\"TurnAllOnButton\"") && xaml.Contains("Content=\"TURN ALL ON\""), "TURN ALL ON control is missing.");
        Require(xaml.Contains("x:Name=\"TurnAllOffButton\"") && xaml.Contains("Content=\"TURN ALL OFF\""), "TURN ALL OFF control is missing.");
        Require(xaml.Contains("AutomationProperties.Name=\"Turn all verified OFF runners on\""), "Bulk ON accessible name is missing or no longer states verified OFF scope.");
        Require(xaml.Contains("AutomationProperties.Name=\"Stop all verified IDLE runners\""), "Bulk OFF accessible name is missing or no longer states verified IDLE scope.");
        Require(code.Contains("new RunnerOperationsService(_engine)"), "GUI does not construct the verified operations layer.");
        Require(code.Contains("TurnAllOnButton_OnClick") && code.Contains("TurnAllOffButton_OnClick"), "Bulk control handlers are missing.");
    }

    private static void PerRunnerControlsRouteSafely()
    {
        var card = CardXaml();
        var cardCode = CardCode();
        var code = Code();

        Require(card.Contains("Value=\"START\"") && card.Contains("Click=\"Primary_OnClick\""), "Explicit START card control is missing.");
        Require(card.Contains("Content=\"STOP\"") && card.Contains("Click=\"Stop_OnClick\""), "Explicit STOP card control is missing.");
        Require(card.Contains("Content=\"RESTART\"") && card.Contains("Click=\"Restart_OnClick\""), "Explicit RESTART card control is missing.");
        Require(card.Contains("Row.CanStart") && card.Contains("Row.CanStop") && card.Contains("Row.CanRestart"),
            "Extracted runner controls do not bind exact row state authority.");
        Require(cardCode.Contains("RunnerCardAction.Start")
                && cardCode.Contains("RunnerCardAction.Stop")
                && cardCode.Contains("RunnerCardAction.Restart"),
            "Extracted card does not emit typed lifecycle actions.");

        var route = MethodBody(code, "RunnerControlCard_OnActionRequested");
        Require(route.Contains("e.Row.CanStart") && route.Contains("_operations.Start(e.Row.Runner)"),
            "START does not honor state authority and route through safe Start.");
        Require(route.Contains("e.Row.CanStop") && route.Contains("_operations.StopIdle(e.Row.Runner)"),
            "STOP does not honor state authority and route through safe StopIdle.");
        Require(route.Contains("e.Row.CanRestart") && route.Contains("_operations.Restart(e.Row.Runner)"),
            "RESTART does not honor state authority and route through verified Restart.");
        Require(!cardCode.Contains("RunnerOperationsService") && !cardCode.Contains("RunnerEngine"),
            "Extracted presentation card owns lifecycle/runtime authority instead of emitting actions.");
        Require(!code.Contains("_engine.Start(")
                && !code.Contains("_engine.StopIdle(")
                && !code.Contains("_engine.Restart("),
            "GUI bypasses RunnerOperationsService lifecycle authority.");
    }

    private static void BusyForceStopIsSecondaryAndConfirmed()
    {
        var card = CardXaml();
        var cardCode = CardCode();
        var code = Code();

        Require(card.Contains("Value=\"FORCE STOP\"")
                && card.Contains("RunnerState.BUSY")
                && cardCode.Contains("RunnerCardAction.ForceStop"),
            "BUSY destructive action is missing from the state-authorized primary slot.");

        var route = MethodBody(code, "RunnerControlCard_OnActionRequested");
        Require(route.Contains("case RunnerCardAction.ForceStop")
                && route.Contains("e.Row.State == RunnerState.BUSY")
                && route.Contains("ExecuteForceStopAsync(e.Row)"),
            "BUSY card action does not remain state-gated and separately routed to confirmed force-stop policy.");

        var force = MethodBody(code, "ExecuteForceStopAsync");
        Require(force.Contains("row.State != RunnerState.BUSY"), "Force-stop execution helper is not BUSY-only.");
        Require(force.Contains("_operations.ForceStopBusy(row.Runner, confirmed: false)"),
            "Force-stop path no longer performs a non-destructive confirmation preflight.");
        Require(force.Contains("active GitHub Actions job", StringComparison.OrdinalIgnoreCase),
            "Force-stop warning omits active-job interruption risk.");
        Require(force.Contains("MessageBoxButton.YesNo") && force.Contains("MessageBoxImage.Warning"),
            "Force-stop lacks explicit destructive confirmation.");
        Require(force.Contains("_operations.ForceStopBusy(row.Runner, confirmed: true)"),
            "Confirmed force-stop does not route through safe force policy.");
        Require(!MethodBody(code, "TurnAllOffButton_OnClick").Contains("ForceStopBusy"),
            "TURN ALL OFF contains a force-stop path.");
    }

    private static void BulkOffConfirmationStatesExactImpact()
    {
        var body = MethodBody(Code(), "TurnAllOffButton_OnClick");
        Require(body.Contains("idleCount") && body.Contains("busyCount"), "Bulk OFF confirmation does not calculate IDLE/BUSY impact counts.");
        Require(body.Contains("IDLE") && body.Contains("BUSY") && body.Contains("remain running", StringComparison.OrdinalIgnoreCase), "Bulk OFF confirmation does not explain BUSY protection.");
        Require(body.IndexOf("MessageBox.Show", StringComparison.Ordinal) < body.IndexOf("_operations.TurnAllOff()", StringComparison.Ordinal), "TURN ALL OFF executes before confirmation.");
    }

    private static void PreviewModeCannotOperate()
    {
        var code = Code();
        foreach (var method in new[] { "TurnAllOnButton_OnClick", "TurnAllOffButton_OnClick" })
        {
            var body = MethodBody(code, method);
            Require(body.Contains("_previewMode") && body.Contains("_operations is null"), $"{method} is not preview/unavailable guarded.");
        }

        var route = MethodBody(code, "RunnerControlCard_OnActionRequested");
        Require(route.Contains("_previewMode")
                && route.Contains("_operations is null")
                && route.Contains("_operationInProgress"),
            "Active extracted-card dispatcher does not block preview, unavailable operations, or overlapping operations.");

        var force = MethodBody(code, "ExecuteForceStopAsync");
        Require(force.Contains("_previewMode") && force.Contains("_operations is null"),
            "Force-stop helper is not independently preview/unavailable guarded.");
        Require(!CardCode().Contains("RunnerOperationsService") && !CardCode().Contains("RunnerEngine"),
            "Extracted card can directly invoke lifecycle/runtime authority.");
        Require(code.Contains("OperationsPanel.IsEnabled = false") && code.Contains("RunnerList.IsHitTestVisible = false"),
            "Preview mode does not visibly disable lifecycle controls.");
    }

    private static void OperationFailuresAreVisible()
    {
        var code = Code();
        Require(code.Contains("RenderControlResult") && code.Contains("RunnerControlOutcome.Error"), "Per-runner operation errors are not handled explicitly.");
        Require(code.Contains("#FF8A3D") && code.Contains("result.Errors"), "Operation error color/count feedback is missing.");
    }

    private static void OperationsLayoutIsResponsive()
    {
        var xaml = Xaml();
        Require(xaml.Contains("x:Name=\"OperationsPanel\" Grid.Row=\"2\""), "Command bar is not in the approved application layout.");
        Require(xaml.Contains("Grid.Column=\"1\" Orientation=\"Horizontal\" HorizontalAlignment=\"Right\""), "Bulk operations do not have a distinct command-bar column.");
        Require(xaml.Contains("Grid.Column=\"2\" BorderBrush=\"{StaticResource BorderBrush}\""), "Search does not have a distinct command-bar column.");
        Require(xaml.Contains("x:Name=\"FilterPanel\" Orientation=\"Horizontal\""), "State filters are not kept in their own region.");
    }

    private static void RenderGateExists()
    {
        var root = RepoRoot();
        var script = Path.Combine(root, "scripts", "render-v0.0.12-preview.ps1");
        Require(File.Exists(script), "Inherited rendered-preview script is missing.");
        var text = File.ReadAllText(script);
        Require(text.Contains("1180x760") && text.Contains("900x560"), "Render script does not retain canonical default/minimum evidence geometries.");
        var workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "redesign-v0.0.12.yml"));
        Require(workflow.Contains("Render canonical v0.0.12 previews") && workflow.Contains("Upload v0.0.12 pre-cert evidence"), "Inherited redesign CI no longer preserves its rendered evidence gate.");
    }

    private static string Xaml() => File.ReadAllText(Path.Combine(RepoRoot(), "src", "MRC.Gui", "MainWindow.xaml"));
    private static string Code() => File.ReadAllText(Path.Combine(RepoRoot(), "src", "MRC.Gui", "MainWindow.xaml.cs"));
    private static string CardXaml() => File.ReadAllText(Path.Combine(RepoRoot(), "src", "MRC.Gui", "Controls", "RunnerControlCard.xaml"));
    private static string CardCode() => File.ReadAllText(Path.Combine(RepoRoot(), "src", "MRC.Gui", "Controls", "RunnerControlCard.xaml.cs"));
    private static string MethodBody(string code, string methodName)
    {
        var start = code.IndexOf(methodName, StringComparison.Ordinal); Require(start >= 0, $"Method {methodName} is missing.");
        var brace = code.IndexOf('{', start); Require(brace >= 0, $"Method {methodName} body is missing.");
        var depth = 0;
        for (var i = brace; i < code.Length; i++) { if (code[i] == '{') depth++; else if (code[i] == '}' && --depth == 0) return code[brace..(i + 1)]; }
        throw new InvalidOperationException($"Method {methodName} body is unterminated.");
    }
    private static string RepoRoot() { var d = new DirectoryInfo(Directory.GetCurrentDirectory()); while (d is not null) { if (File.Exists(Path.Combine(d.FullName, "Auth", "0000_MasterAuth.md"))) return d.FullName; d = d.Parent; } throw new InvalidOperationException("Could not locate MRC repository root."); }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
