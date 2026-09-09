using System.Runtime.CompilerServices;

internal static class Task10RegressionPipelineContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        var workflowPath = Path.Combine(
            Directory.GetCurrentDirectory(), ".github", "workflows", "redesign-v0.0.12.yml");
        var workflow = File.ReadAllText(workflowPath);

        var redesign = workflow.IndexOf("./scripts/verify-redesign.ps1", StringComparison.Ordinal);
        var pass4 = workflow.IndexOf("./scripts/verify-pass4.ps1", StringComparison.Ordinal);
        var pass3 = workflow.IndexOf("./scripts/verify-pass3.ps1", StringComparison.Ordinal);
        var pass2 = workflow.IndexOf("./scripts/verify-pass2.ps1", StringComparison.Ordinal);
        var pass1 = workflow.IndexOf("./scripts/verify-pass1.ps1", StringComparison.Ordinal);

        Require(redesign >= 0, "v0.0.12 redesign harness is missing from CI.");
        Require(pass4 > redesign, "PASS4 regression must run after the redesign harness.");
        Require(pass3 > pass4, "PASS3 regression must run after PASS4.");
        Require(pass2 > pass3, "PASS2 regression must run after PASS3.");
        Require(pass1 > pass2, "PASS1 regression must run after PASS2.");

        Require(workflow.Contains("Full PASS4 to PASS1 regression ladder", StringComparison.Ordinal),
            "Task 10 full regression ladder is not explicitly identified in CI.");

        Console.WriteLine("PASS  Task10B full PASS4-to-PASS1 regression pipeline contract");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
