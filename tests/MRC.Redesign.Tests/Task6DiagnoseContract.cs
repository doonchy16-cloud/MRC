using System.Reflection;
using System.Runtime.CompilerServices;
using MRC.Core;

internal static class Task6DiagnoseContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifyManagedRunnerForensics();
        Console.WriteLine("PASS  Task6D1 managed-runner forensic evidence");
    }

    private static void VerifyManagedRunnerForensics()
    {
        var core = typeof(RunnerEngine).Assembly;
        var finding = core.GetType("MRC.Core.Diagnostics.DiagnoseRunnerFinding");
        Require(finding is not null, "DiagnoseRunnerFinding is missing.");

        foreach (var property in new[]
                 {
                     "ListenerProcessId",
                     "ListenerParentProcessId",
                     "ListenerSessionId",
                     "ListenerProcessName",
                     "ListenerExecutablePath",
                     "OwnershipProof",
                     "InspectionError",
                     "WorkerProcessIds"
                 })
        {
            Require(finding!.GetProperty(property, BindingFlags.Public | BindingFlags.Instance) is not null,
                $"DiagnoseRunnerFinding.{property} is missing.");
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
