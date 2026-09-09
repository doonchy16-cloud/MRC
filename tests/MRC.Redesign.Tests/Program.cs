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
