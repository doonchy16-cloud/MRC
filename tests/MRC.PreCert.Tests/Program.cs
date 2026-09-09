using MRC.Core;
using MRC.Core.Runners;
using MRC.Core.Runtime;

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var failures = new List<string>();
var tests = new (string Name, Action Body)[]
{
    ("actual Windows identity is authoritative", () =>
    {
        Require(MrcConstants.TargetMachineName == "DOONCHYSCOMPUTI",
            $"TargetMachineName is '{MrcConstants.TargetMachineName}', expected DOONCHYSCOMPUTI.");
        var ok = EnvironmentFence.Evaluate("DOONCHYSCOMPUTI", @"D:\Git_Runners_Main", true);
        Require(ok.IsAuthorized, "DOONCHYSCOMPUTI + exact runner root was not authorized.");
        var oldLabel = EnvironmentFence.Evaluate("Main-PC", @"D:\Git_Runners_Main", true);
        Require(!oldLabel.IsAuthorized, "Friendly label Main-PC must not be accepted as the Windows computer identity.");
    }),
    ("package installer and manifest use actual Windows identity", () =>
    {
        var install = File.ReadAllText(Path.Combine(repoRoot, "scripts", "install.ps1"));
        Require(install.Contains("$targetMachine = 'DOONCHYSCOMPUTI'", StringComparison.Ordinal),
            "scripts/install.ps1 is not pinned to DOONCHYSCOMPUTI.");
        var package = File.ReadAllText(Path.Combine(repoRoot, "scripts", "package.ps1"));
        Require(package.Contains("targetMachine = 'DOONCHYSCOMPUTI'", StringComparison.Ordinal),
            "scripts/package.ps1 still writes the wrong manifest machine identity.");
    }),
    ("bootstrap targets v0.0.10 on actual Windows identity", () =>
    {
        var text = File.ReadAllText(Path.Combine(repoRoot, "install-mrc.ps1"));
        Require(text.Contains("$version = '0.0.10'", StringComparison.Ordinal), "Bootstrap is not pinned to v0.0.10.");
        Require(text.Contains("$targetMachine = 'DOONCHYSCOMPUTI'", StringComparison.Ordinal),
            "Bootstrap is not pinned to DOONCHYSCOMPUTI.");
        Require(text.Contains("releases/download/v$version", StringComparison.Ordinal), "Bootstrap does not use the pinned release asset path.");
    }),
    ("bootstrap verifies SHA before extraction and reuses installer", () =>
    {
        var text = File.ReadAllText(Path.Combine(repoRoot, "install-mrc.ps1"));
        var hash = text.IndexOf("Get-FileHash", StringComparison.Ordinal);
        var compare = text.IndexOf("$actualHash -ne $expectedHash", StringComparison.Ordinal);
        var extract = text.IndexOf("Expand-Archive", StringComparison.Ordinal);
        var install = text.IndexOf("-File $installPath", StringComparison.Ordinal);
        Require(hash >= 0 && compare > hash && extract > compare && install > extract,
            "Bootstrap must verify SHA-256 before extraction and invoke install.ps1 only afterward.");
        Require(text.Contains("Remove-Item -LiteralPath $tempRoot", StringComparison.Ordinal), "Bootstrap does not clean temporary files.");
    }),
    ("uninspectable runner process reports actionable diagnostics", () =>
    {
        var runner = new RunnerDescriptor(
            @"D:\Git_Runners_Main\Example",
            "ExampleRunner",
            "https://github.com/example/repo",
            "repo",
            1,
            "_work",
            null);
        var inventory = new ProcessInventory(
            new[] { new ProcessSnapshot(4242, 100, "Runner.Listener", null, "Access is denied.") },
            true,
            null);
        var association = RunnerProcessAssociator.Associate(runner, inventory);
        Require(!string.IsNullOrWhiteSpace(association.Error), "Uninspectable runner process did not produce an ownership error.");
        Require(association.Error!.Contains("4242", StringComparison.Ordinal), "Runtime error does not identify the failing PID.");
        Require(association.Error.Contains("Runner.Listener", StringComparison.Ordinal), "Runtime error does not identify the failing process name.");
        Require(association.Error.Contains("Access is denied.", StringComparison.Ordinal), "Runtime error does not preserve the Windows inspection error.");
    }),
    ("ERROR rows expose working DETAILS diagnostics", () =>
    {
        var xaml = File.ReadAllText(Path.Combine(repoRoot, "src", "MRC.Gui", "MainWindow.xaml"));
        var errorTriggerStart = xaml.IndexOf("Value=\"{x:Static runtime:RunnerState.ERROR}\"", StringComparison.Ordinal);
        Require(errorTriggerStart >= 0, "ERROR state trigger is missing from runner control style.");
        var errorTriggerEnd = xaml.IndexOf("</DataTrigger>", errorTriggerStart, StringComparison.Ordinal);
        Require(errorTriggerEnd > errorTriggerStart, "ERROR state trigger is malformed.");
        var errorTrigger = xaml[errorTriggerStart..errorTriggerEnd];
        Require(errorTrigger.Contains("Property=\"IsEnabled\" Value=\"True\"", StringComparison.Ordinal),
            "DETAILS remains disabled for ERROR rows.");

        var code = File.ReadAllText(Path.Combine(repoRoot, "src", "MRC.Gui", "MainWindow.xaml.cs"));
        Require(code.Contains("row.State == RunnerState.ERROR", StringComparison.Ordinal), "ERROR click path is missing.");
        Require(code.Contains("row.Error", StringComparison.Ordinal), "ERROR click path does not surface the diagnostic message.");
    })
};

Console.WriteLine($"MRC pre-cert targeted harness — {tests.Length} tests");
foreach (var test in tests)
{
    try
    {
        test.Body();
        Console.WriteLine($"PASS  {test.Name}");
    }
    catch (Exception ex)
    {
        failures.Add(test.Name);
        Console.WriteLine($"FAIL  {test.Name}");
        Console.WriteLine($"      {ex.Message}");
    }
}

if (failures.Count > 0)
{
    Console.WriteLine($"FAIL  {failures.Count}/{tests.Length}: {string.Join(", ", failures)}");
    return 1;
}

Console.WriteLine($"PASS  all {tests.Length} targeted pre-cert tests");
return 0;
