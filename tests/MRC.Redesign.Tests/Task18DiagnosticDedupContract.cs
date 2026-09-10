using System.Reflection;
using System.Runtime.CompilerServices;
using MRC.Core.Runtime;

internal static class Task18DiagnosticDedupContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifyIdenticalEvidenceIsDeduplicated();
        VerifyDistinctEvidenceIsPreserved();
        Console.WriteLine("PASS  Task18 diagnostic evidence composition deduplicates only identical normalized failures");
    }

    private static void VerifyIdenticalEvidenceIsDeduplicated()
    {
        var result = Combine("  Access is denied.  ", "access is denied.");
        Require(result == "Access is denied.",
            $"Identical normalized inspection evidence must appear once; got '{result}'.");
    }

    private static void VerifyDistinctEvidenceIsPreserved()
    {
        var result = Combine("Path inspection failed.", "Parent inspection failed.");
        Require(result == "Path inspection failed. | Parent inspection failed.",
            $"Distinct inspection evidence must remain ordered and complete; got '{result}'.");
    }

    private static string? Combine(string? first, string? second)
    {
        var providerType = typeof(RunnerState).Assembly.GetType(
            "MRC.Core.Runtime.WindowsProcessSnapshotProvider",
            throwOnError: true)!;
        var method = providerType.GetMethod(
            "Combine",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("WindowsProcessSnapshotProvider.Combine evidence boundary is missing.");

        return (string?)method.Invoke(null, new object?[] { first, second });
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
