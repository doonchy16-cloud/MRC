using MRC.Core;
using MRC.Core.Runners;
using MRC.Core.Runtime;

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var failures = new List<string>();
var tests = new (string Name, Action Body)[]
{
    ("external uninspectable runner does not poison managed association", () =>
    {
        var runner = new RunnerDescriptor(
            @"D:\Git_Runners_Main\BBAP_runner",
            "BBAP_Runner",
            "https://github.com/doonchy16-cloud/BBAP",
            "BBAP",
            1,
            "_work",
            null);

        var listener = new ProcessSnapshot(
            44536,
            32108,
            "Runner.Listener",
            @"D:\Git_Runners_Main\BBAP_runner\bin\Runner.Listener.exe");

        var externalUninspectable = new ProcessSnapshot(
            31716,
            50276,
            "Runner.Listener",
            null,
            "Access is denied. | Access is denied.");

        var inventory = new ProcessInventory(
            new[] { listener, externalUninspectable },
            true,
            null);

        var association = RunnerProcessAssociator.Associate(runner, inventory);
        Require(string.IsNullOrWhiteSpace(association.Error),
            $"An unrelated uninspectable runner poisoned BBAP ownership: {association.Error}");
        Require(association.Listener?.ProcessId == 44536,
            "The exact BBAP listener was not associated.");

        var state = RunnerStateEvaluator.Evaluate(runner, association, null, DateTimeOffset.UtcNow);
        Require(state == RunnerState.IDLE,
            $"Expected BBAP to be IDLE with one listener and zero workers, got {state}.");
    }),
    ("runtime exposes managed external unattributed ownership classes", () =>
    {
        var core = typeof(RunnerEngine).Assembly;
        var ownershipType = core.GetType("MRC.Core.Runtime.RunnerProcessOwnershipKind");
        Require(ownershipType is not null, "RunnerProcessOwnershipKind is missing.");
        Require(ownershipType!.IsEnum, "RunnerProcessOwnershipKind must be an enum.");
        var names = Enum.GetNames(ownershipType);
        Require(names.SequenceEqual(new[] { "Managed", "External", "Unattributed" }),
            $"Ownership classes are wrong: {string.Join(", ", names)}");

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
        Require(sessionProperty!.PropertyType == typeof(int?),
            $"ProcessSnapshot.SessionId must be nullable int, got {sessionProperty.PropertyType}.");
    }),
    ("runner engine exposes managed snapshots plus separate system findings", () =>
    {
        var method = typeof(RunnerEngine).GetMethod("RefreshReport");
        Require(method is not null, "RunnerEngine.RefreshReport() is missing.");
        Require(method!.GetParameters().Length == 0, "RunnerEngine.RefreshReport() must not require arguments.");

        var reportType = typeof(RunnerEngine).Assembly.GetType("MRC.Core.Runtime.RunnerRuntimeReport");
        Require(reportType is not null, "RunnerRuntimeReport is missing.");
        Require(reportType!.GetProperty("ManagedRunners") is not null, "RunnerRuntimeReport.ManagedRunners is missing.");
        Require(reportType.GetProperty("SystemFindings") is not null, "RunnerRuntimeReport.SystemFindings is missing.");
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
