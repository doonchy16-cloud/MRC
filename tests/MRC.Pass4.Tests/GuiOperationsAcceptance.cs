namespace MRC.Pass4.Tests;

internal static class GuiOperationsAcceptance
{
    public static int Run()
    {
        var tests = new (string Name, Action Body)[]
        {
            ("bulk ON/OFF controls exist in the compact terminal command bar", BulkControlsExist),
            ("per-runner primary controls route by truthful state through operations service", PerRunnerControlsRouteSafely),
            ("BUSY force-stop is secondary and requires explicit destructive confirmation", BusyForceStopIsSecondaryAndConfirmed),
            ("TURN ALL OFF confirmation states exact IDLE stop and BUSY remain impact", BulkOffConfirmationStatesExactImpact),
            ("preview mode cannot invoke any runner lifecycle operation", PreviewModeCannotOperate),
            ("operation failures surface clear visible error feedback", OperationFailuresAreVisible),
            ("bulk/search/filter controls occupy distinct compact command-bar columns", OperationsLayoutIsResponsive),
            ("v0.0.12 CI requires fresh default and minimum-size rendered previews", RenderGateExists)
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
        Require(xaml.Contains("x:Name=\"OperationsPanel\""), "Compact operations command bar is missing.");
        Require(xaml.Contains("x:Name=\"TurnAllOnButton\"") && xaml.Contains("Content=\"[ ALL ON ]\""), "[ ALL ON ] control is missing.");
        Require(xaml.Contains("x:Name=\"TurnAllOffButton\"") && xaml.Contains("Content=\"[ ALL OFF ]\""), "[ ALL OFF ] control is missing.");
        Require(xaml.Contains("AutomationProperties.Name=\"Turn all OFF runners on\""), "Bulk ON accessible name is missing.");
        Require(xaml.Contains("AutomationProperties.Name=\"Stop all IDLE runners\""), "Bulk OFF accessible name is missing.");
        Require(code.Contains("new RunnerOperationsService(_engine)"), "GUI does not construct the verified operations layer.");
        Require(code.Contains("TurnAllOnButton_OnClick") && code.Contains("TurnAllOffButton_OnClick"), "Bulk control handlers are missing.");
    }

    private static void PerRunnerControlsRouteSafely()
    {
        var xaml = Xaml(); var code = Code();
        Require(xaml.Contains("RunnerPrimaryControl_OnClick"), "Per-runner primary action handler is missing.");
        Require(xaml.Contains("CommandParameter=\"{Binding}\""), "Runner controls do not carry exact row identity.");
        Require(code.Contains("row.State == RunnerState.OFF") && code.Contains("_operations.Start(row.Runner)"), "OFF primary action does not route to safe Start.");
        Require(code.Contains("row.State == RunnerState.IDLE") && code.Contains("_operations.StopIdle(row.Runner)"), "IDLE primary action does not route to safe StopIdle.");
        Require(!code.Contains("_engine.Start(row.Runner)") && !code.Contains("_engine.StopIdle(row.Runner)"), "GUI bypasses RunnerOperationsService.");
    }

    private static void BusyForceStopIsSecondaryAndConfirmed()
    {
        var xaml = Xaml(); var code = Code();
        Require(xaml.Contains("Content=\"!\"") && xaml.Contains("RunnerMoreControl_OnClick"), "BUSY secondary destructive action is missing.");
        Require(code.Contains("row.State != RunnerState.BUSY"), "Secondary force path is not BUSY-only.");
        Require(code.Contains("active GitHub Actions job", StringComparison.OrdinalIgnoreCase), "Force-stop warning omits active-job interruption risk.");
        Require(code.Contains("MessageBoxButton.YesNo") && code.Contains("MessageBoxImage.Warning"), "Force-stop lacks explicit destructive confirmation.");
        Require(code.Contains("_operations.ForceStopBusy(row.Runner, confirmed: true)"), "Confirmed force-stop does not route through safe force policy.");
        Require(!MethodBody(code, "TurnAllOffButton_OnClick").Contains("ForceStopBusy"), "TURN ALL OFF contains a force-stop path.");
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
        foreach (var method in new[] { "TurnAllOnButton_OnClick", "TurnAllOffButton_OnClick", "RunnerPrimaryControl_OnClick", "RunnerMoreControl_OnClick" })
        {
            var body = MethodBody(code, method);
            Require(body.Contains("_previewMode") && body.Contains("_operations is null"), $"{method} is not preview/unavailable guarded.");
        }
        Require(code.Contains("OperationsPanel.IsEnabled = false") && code.Contains("RunnerList.IsHitTestVisible = false"), "Preview mode does not visibly disable lifecycle controls.");
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
        Require(xaml.Contains("x:Name=\"OperationsPanel\" Grid.Row=\"2\""), "Command bar is not in the compact terminal layout.");
        Require(xaml.Contains("Grid.Column=\"1\" Orientation=\"Horizontal\" HorizontalAlignment=\"Right\""), "Bulk operations do not have a distinct command-bar column.");
        Require(xaml.Contains("Grid.Column=\"2\" BorderBrush=\"{StaticResource BorderBrush}\""), "Search does not have a distinct command-bar column.");
        Require(xaml.Contains("x:Name=\"FilterPanel\" Orientation=\"Horizontal\""), "State filters are not kept in their own compact region.");
    }

    private static void RenderGateExists()
    {
        var root = RepoRoot();
        var script = Path.Combine(root, "scripts", "render-v0.0.12-preview.ps1");
        Require(File.Exists(script), "v0.0.12 rendered-preview script is missing.");
        var text = File.ReadAllText(script);
        Require(text.Contains("1180x760") && text.Contains("900x560"), "Render script does not require both canonical geometries.");
        var workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "redesign-v0.0.12.yml"));
        Require(workflow.Contains("Render canonical v0.0.12 previews") && workflow.Contains("Upload v0.0.12 pre-cert evidence"), "Final redesign CI does not render/upload canonical GUI evidence.");
    }

    private static string Xaml() => File.ReadAllText(Path.Combine(RepoRoot(), "src", "MRC.Gui", "MainWindow.xaml"));
    private static string Code() => File.ReadAllText(Path.Combine(RepoRoot(), "src", "MRC.Gui", "MainWindow.xaml.cs"));
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
