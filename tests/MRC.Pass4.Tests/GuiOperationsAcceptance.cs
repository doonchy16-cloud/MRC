namespace MRC.Pass4.Tests;

internal static class GuiOperationsAcceptance
{
    public static int Run()
    {
        var tests = new (string Name, Action Body)[]
        {
            ("bulk ON/OFF controls exist as a dedicated operations surface", BulkControlsExist),
            ("per-runner primary controls route by truthful state through operations service", PerRunnerControlsRouteSafely),
            ("BUSY force-stop is secondary and requires explicit destructive confirmation", BusyForceStopIsSecondaryAndConfirmed),
            ("TURN ALL OFF confirmation states exact IDLE stop and BUSY remain impact", BulkOffConfirmationStatesExactImpact),
            ("preview mode cannot invoke any runner lifecycle operation", PreviewModeCannotOperate),
            ("operation failures surface clear visible error feedback", OperationFailuresAreVisible),
            ("PASS 4 operations stay out of the search/filter toolbar crowding path", OperationsLayoutIsResponsive),
            ("PASS 4 CI requires fresh default and minimum-size rendered previews", Pass4RenderGateExists)
        };

        var failures = 0;
        Console.WriteLine($"MRC PASS 4 GUI operations harness — {tests.Length} tests");
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
            ? $"PASS  all {tests.Length} PASS 4 GUI operations tests"
            : $"FAIL  {failures} of {tests.Length} PASS 4 GUI operations tests");
        return failures;
    }

    private static void BulkControlsExist()
    {
        var xaml = Xaml();
        var code = Code();
        Require(xaml.Contains("x:Name=\"OperationsPanel\"", StringComparison.Ordinal), "Dedicated operations panel is missing.");
        Require(xaml.Contains("x:Name=\"TurnAllOnButton\"", StringComparison.Ordinal) && xaml.Contains("TURN ALL ON", StringComparison.Ordinal), "TURN ALL ON control is missing.");
        Require(xaml.Contains("x:Name=\"TurnAllOffButton\"", StringComparison.Ordinal) && xaml.Contains("TURN ALL OFF", StringComparison.Ordinal), "TURN ALL OFF control is missing.");
        Require(xaml.Contains("AutomationProperties.Name=\"Turn all OFF runners on\"", StringComparison.Ordinal), "Bulk ON accessible name is missing.");
        Require(xaml.Contains("AutomationProperties.Name=\"Stop all IDLE runners\"", StringComparison.Ordinal), "Bulk OFF accessible name is missing.");
        Require(code.Contains("new RunnerOperationsService(_engine)", StringComparison.Ordinal), "GUI does not construct the PASS 4 operations layer from the verified engine.");
        Require(code.Contains("TurnAllOnButton_OnClick", StringComparison.Ordinal) && code.Contains("TurnAllOffButton_OnClick", StringComparison.Ordinal), "Bulk control handlers are missing.");
    }

    private static void PerRunnerControlsRouteSafely()
    {
        var xaml = Xaml();
        var code = Code();
        Require(xaml.Contains("RunnerPrimaryControl_OnClick", StringComparison.Ordinal), "Per-runner primary action handler is missing.");
        Require(xaml.Contains("CommandParameter=\"{Binding}\"", StringComparison.Ordinal), "Runner controls do not carry the exact row identity.");
        Require(code.Contains("row.State == RunnerState.OFF", StringComparison.Ordinal) && code.Contains("_operations.Start(row.Runner)", StringComparison.Ordinal), "OFF primary action does not route to safe Start.");
        Require(code.Contains("row.State == RunnerState.IDLE", StringComparison.Ordinal) && code.Contains("_operations.StopIdle(row.Runner)", StringComparison.Ordinal), "IDLE primary action does not route to safe StopIdle.");
        Require(!code.Contains("_engine.Start(row.Runner)", StringComparison.Ordinal) && !code.Contains("_engine.StopIdle(row.Runner)", StringComparison.Ordinal), "GUI bypasses RunnerOperationsService and calls engine controls directly.");
    }

    private static void BusyForceStopIsSecondaryAndConfirmed()
    {
        var xaml = Xaml();
        var code = Code();
        Require(xaml.Contains("Content=\"…\"", StringComparison.Ordinal) && xaml.Contains("RunnerMoreControl_OnClick", StringComparison.Ordinal), "BUSY secondary ellipsis action is missing.");
        Require(code.Contains("row.State != RunnerState.BUSY", StringComparison.Ordinal), "Secondary force path is not explicitly BUSY-only.");
        Require(code.Contains("active GitHub Actions job", StringComparison.OrdinalIgnoreCase), "Force-stop warning does not explain active-job interruption risk.");
        Require(code.Contains("MessageBoxButton.YesNo", StringComparison.Ordinal), "Force-stop does not require explicit Yes/No confirmation.");
        Require(code.Contains("MessageBoxImage.Warning", StringComparison.Ordinal), "Force-stop warning is not visually classified as destructive.");
        Require(code.Contains("_operations.ForceStopBusy(row.Runner, confirmed: true)", StringComparison.Ordinal), "Confirmed force-stop does not route through the safe force policy.");
        var bulkOff = MethodBody(code, "TurnAllOffButton_OnClick");
        Require(!bulkOff.Contains("ForceStopBusy", StringComparison.Ordinal), "TURN ALL OFF contains a force-stop path.");
    }

    private static void BulkOffConfirmationStatesExactImpact()
    {
        var code = Code();
        var body = MethodBody(code, "TurnAllOffButton_OnClick");
        Require(body.Contains("idleCount", StringComparison.Ordinal) && body.Contains("busyCount", StringComparison.Ordinal), "Bulk OFF confirmation does not calculate both IDLE and BUSY impact counts.");
        Require(body.Contains("IDLE", StringComparison.Ordinal) && body.Contains("BUSY", StringComparison.Ordinal), "Bulk OFF confirmation does not name both state classes.");
        Require(body.Contains("remain running", StringComparison.OrdinalIgnoreCase), "Bulk OFF confirmation does not state that BUSY runners remain running.");
        Require(body.IndexOf("MessageBox.Show", StringComparison.Ordinal) < body.IndexOf("_operations.TurnAllOff()", StringComparison.Ordinal), "TURN ALL OFF executes before confirmation.");
    }

    private static void PreviewModeCannotOperate()
    {
        var code = Code();
        foreach (var method in new[] { "TurnAllOnButton_OnClick", "TurnAllOffButton_OnClick", "RunnerPrimaryControl_OnClick", "RunnerMoreControl_OnClick" })
        {
            var body = MethodBody(code, method);
            Require(body.Contains("_previewMode", StringComparison.Ordinal) && body.Contains("_operations is null", StringComparison.Ordinal), $"{method} is not guarded against preview/unavailable operation mode.");
        }
        Require(code.Contains("OperationsPanel.IsEnabled = false", StringComparison.Ordinal), "Preview mode does not visibly disable bulk operations.");
        Require(code.Contains("RunnerList.IsHitTestVisible = false", StringComparison.Ordinal), "Preview mode does not disable runner-row operation hit testing.");
    }

    private static void OperationFailuresAreVisible()
    {
        var code = Code();
        Require(code.Contains("RenderControlResult", StringComparison.Ordinal), "Per-runner operation feedback renderer is missing.");
        Require(code.Contains("RunnerControlOutcome.Error", StringComparison.Ordinal), "Operation error outcome is not handled explicitly.");
        Require(code.Contains("#FF8A3D", StringComparison.Ordinal), "Operation error feedback does not use the canonical visible error color.");
        Require(code.Contains("result.Errors", StringComparison.Ordinal), "Bulk operation error counts are not surfaced.");
    }

    private static void OperationsLayoutIsResponsive()
    {
        var xaml = Xaml();
        var operationsIndex = xaml.IndexOf("x:Name=\"OperationsPanel\"", StringComparison.Ordinal);
        var searchIndex = xaml.IndexOf("x:Name=\"SearchBox\"", StringComparison.Ordinal);
        Require(operationsIndex >= 0 && searchIndex >= 0 && operationsIndex < searchIndex, "Operations panel is not a distinct region before the search/filter toolbar.");
        var searchRegionStart = xaml.LastIndexOf("<Grid Grid.Row=", searchIndex, StringComparison.Ordinal);
        var searchRegionEnd = xaml.IndexOf("</Grid>", searchIndex, StringComparison.Ordinal);
        Require(searchRegionStart >= 0 && searchRegionEnd > searchRegionStart, "Could not isolate search toolbar region.");
        var searchRegion = xaml[searchRegionStart..searchRegionEnd];
        Require(!searchRegion.Contains("TURN ALL ON", StringComparison.Ordinal) && !searchRegion.Contains("TURN ALL OFF", StringComparison.Ordinal), "Bulk controls crowd the search/filter toolbar.");
    }

    private static void Pass4RenderGateExists()
    {
        var root = RepoRoot();
        var script = Path.Combine(root, "scripts", "render-pass4-preview.ps1");
        Require(File.Exists(script), "PASS 4 rendered-preview script is missing.");
        var scriptText = File.ReadAllText(script);
        Require(scriptText.Contains("1180x760", StringComparison.Ordinal) && scriptText.Contains("900x560", StringComparison.Ordinal), "PASS 4 render script does not require both canonical geometries.");
        var workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "pass4.yml"));
        Require(workflow.Contains("Render PASS 4 preview", StringComparison.Ordinal), "PASS 4 CI does not render the operations UI.");
        Require(workflow.Contains("Upload PASS 4 preview", StringComparison.Ordinal), "PASS 4 CI does not publish rendered operations evidence.");
        Require(workflow.Contains("MRC-PASS4-*.png", StringComparison.Ordinal), "PASS 4 CI does not require both preview PNG artifacts.");
    }

    private static string Xaml() => File.ReadAllText(Path.Combine(RepoRoot(), "src", "MRC.Gui", "MainWindow.xaml"));
    private static string Code() => File.ReadAllText(Path.Combine(RepoRoot(), "src", "MRC.Gui", "MainWindow.xaml.cs"));

    private static string MethodBody(string code, string methodName)
    {
        var start = code.IndexOf(methodName, StringComparison.Ordinal);
        Require(start >= 0, $"Method {methodName} is missing.");
        var brace = code.IndexOf('{', start);
        Require(brace >= 0, $"Method {methodName} body is missing.");
        var depth = 0;
        for (var index = brace; index < code.Length; index++)
        {
            if (code[index] == '{') depth++;
            else if (code[index] == '}')
            {
                depth--;
                if (depth == 0) return code[brace..(index + 1)];
            }
        }
        throw new InvalidOperationException($"Method {methodName} body is unterminated.");
    }

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

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
