using System.Reflection;
using MRC.Core;
using MRC.Core.Runners;
using MRC.Core.Runtime;

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static string TempRoot()
{
    var path = Path.Combine(Path.GetTempPath(), "mrc-redesign-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(path);
    return path;
}

var failures = new List<string>();
var tests = new (string Name, Action Body)[]
{
    ("external uninspectable runner does not poison managed association", () =>
    {
        var runner = new RunnerDescriptor(@"D:\Git_Runners_Main\BBAP_runner", "BBAP_Runner", "https://github.com/doonchy16-cloud/BBAP", "BBAP", 1, "_work", null);
        var listener = new ProcessSnapshot(44536, 32108, "Runner.Listener", @"D:\Git_Runners_Main\BBAP_runner\bin\Runner.Listener.exe");
        var externalUninspectable = new ProcessSnapshot(31716, 50276, "Runner.Listener", null, "Access is denied. | Access is denied.");
        var inventory = new ProcessInventory(new[] { listener, externalUninspectable }, true, null);
        var association = RunnerProcessAssociator.Associate(runner, inventory);
        Require(string.IsNullOrWhiteSpace(association.Error), $"An unrelated uninspectable runner poisoned BBAP ownership: {association.Error}");
        Require(association.Listener?.ProcessId == 44536, "The exact BBAP listener was not associated.");
        var state = RunnerStateEvaluator.Evaluate(runner, association, null, DateTimeOffset.UtcNow);
        Require(state == RunnerState.IDLE, $"Expected BBAP to be IDLE with one listener and zero workers, got {state}.");
    }),
    ("apparently OFF runner remains fail-closed when runner ownership is uninspectable", () =>
    {
        var runner = new RunnerDescriptor(@"D:\Git_Runners_Main\Example", "ExampleRunner", "https://github.com/example/repo", "repo", 2, "_work", null);
        var inventory = new ProcessInventory(new[] { new ProcessSnapshot(4242, 100, "Runner.Listener", null, "Access is denied.") }, true, null);
        var association = RunnerProcessAssociator.Associate(runner, inventory);
        Require(!string.IsNullOrWhiteSpace(association.Error), "A runner with no exact listener must not be declared OFF while unresolved runner-process ownership exists.");
        var state = RunnerStateEvaluator.Evaluate(runner, association, null, DateTimeOffset.UtcNow);
        Require(state == RunnerState.ERROR, $"Expected fail-closed ERROR, got {state}.");
    }),
    ("runtime exposes managed external unattributed ownership classes", () =>
    {
        var core = typeof(RunnerEngine).Assembly;
        var ownershipType = core.GetType("MRC.Core.Runtime.RunnerProcessOwnershipKind");
        Require(ownershipType is not null && ownershipType.IsEnum, "RunnerProcessOwnershipKind is missing or not an enum.");
        var names = Enum.GetNames(ownershipType!);
        Require(names.SequenceEqual(new[] { "Managed", "External", "Unattributed" }), $"Ownership classes are wrong: {string.Join(", ", names)}");
        var observedType = core.GetType("MRC.Core.Runtime.RunnerObservedProcess");
        Require(observedType is not null, "RunnerObservedProcess model is missing.");
        Require(observedType!.GetProperty("ProcessId") is not null, "RunnerObservedProcess.ProcessId is missing.");
        Require(observedType.GetProperty("Ownership") is not null, "RunnerObservedProcess.Ownership is missing.");
        Require(observedType.GetProperty("ManagedRunnerPath") is not null, "RunnerObservedProcess.ManagedRunnerPath is missing.");
        Require(observedType.GetProperty("Reason") is not null, "RunnerObservedProcess.Reason is missing.");
    }),
    ("runtime snapshot preserves Windows session id", () =>
    {
        var sessionProperty = typeof(ProcessSnapshot).GetProperty("SessionId");
        Require(sessionProperty is not null, "ProcessSnapshot.SessionId is missing.");
        Require(sessionProperty!.PropertyType == typeof(int?), $"ProcessSnapshot.SessionId must be nullable int, got {sessionProperty.PropertyType}.");
    }),
    ("runner engine exposes managed snapshots plus separate system findings", () =>
    {
        var method = typeof(RunnerEngine).GetMethod("RefreshReport");
        Require(method is not null && method.GetParameters().Length == 0, "RunnerEngine.RefreshReport() is missing or has the wrong signature.");
        var reportType = typeof(RunnerEngine).Assembly.GetType("MRC.Core.Runtime.RunnerRuntimeReport");
        Require(reportType is not null, "RunnerRuntimeReport is missing.");
        Require(reportType!.GetProperty("ManagedRunners") is not null, "RunnerRuntimeReport.ManagedRunners is missing.");
        Require(reportType.GetProperty("SystemFindings") is not null, "RunnerRuntimeReport.SystemFindings is missing.");
    }),
    ("folder classifier separates non-runner and broken runner candidate", () =>
    {
        var core = typeof(RunnerEngine).Assembly;
        var classifier = core.GetType("MRC.Core.Runners.RunnerFolderClassifier");
        Require(classifier is not null, "RunnerFolderClassifier is missing.");
        var classify = classifier!.GetMethod("Classify", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Require(classify is not null, "RunnerFolderClassifier.Classify(path) is missing.");

        var root = TempRoot();
        try
        {
            var normal = Path.Combine(root, "dash-echo");
            Directory.CreateDirectory(normal);
            var nonRunner = classify!.Invoke(null, new object[] { normal });
            Require(nonRunner is not null, "Non-runner classification returned null.");
            var kind = nonRunner!.GetType().GetProperty("Kind")?.GetValue(nonRunner)?.ToString();
            Require(kind == "NonRunnerFolder", $"Expected NonRunnerFolder, got {kind ?? "<null>"}.");

            var partial = Path.Combine(root, "partial-runner");
            Directory.CreateDirectory(partial);
            File.WriteAllText(Path.Combine(partial, ".runner"), "{}");
            File.WriteAllText(Path.Combine(partial, "run.cmd"), "@echo off");
            var broken = classify.Invoke(null, new object[] { partial });
            var brokenKind = broken?.GetType().GetProperty("Kind")?.GetValue(broken)?.ToString();
            Require(brokenKind == "BrokenRunnerCandidate", $"Expected BrokenRunnerCandidate, got {brokenKind ?? "<null>"}.");
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }),
    ("diagnose subsystem is read-only and exposes deep forensic fields", () =>
    {
        var core = typeof(RunnerEngine).Assembly;
        var service = core.GetType("MRC.Core.Diagnostics.DiagnoseService");
        Require(service is not null, "DiagnoseService is missing.");
        var run = service!.GetMethod("Run", BindingFlags.Public | BindingFlags.Instance)
                  ?? service.GetMethod("RunAsync", BindingFlags.Public | BindingFlags.Instance);
        Require(run is not null, "DiagnoseService Run/RunAsync entry point is missing.");

        var report = core.GetType("MRC.Core.Diagnostics.DiagnoseReport");
        Require(report is not null, "DiagnoseReport is missing.");
        foreach (var property in new[] { "MachineName", "UserName", "SessionId", "IsElevated", "Runners", "SystemProcesses", "Folders", "Services" })
        {
            Require(report!.GetProperty(property) is not null, $"DiagnoseReport.{property} is missing.");
        }

        Require(core.GetType("MRC.Core.Diagnostics.WindowsServiceInspector") is not null,
            "WindowsServiceInspector is missing; Session-0 service correlation cannot be proven.");
    })
};

Console.WriteLine($"MRC v0.0.12 redesign harness — {tests.Length} test(s)");
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

Console.WriteLine($"PASS  all {tests.Length} redesign tests");
return 0;
