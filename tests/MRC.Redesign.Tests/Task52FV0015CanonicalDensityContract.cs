using System.Runtime.CompilerServices;

internal static class Task52FV0015CanonicalDensityContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        var root = Directory.GetCurrentDirectory();
        var preview = File.ReadAllText(Path.Combine(root, "tools", "MRC.Pass3.Preview", "Program.cs"));

        var start = preview.IndexOf("private static IReadOnlyList<RunnerSnapshot> BuildPreviewSnapshots()", StringComparison.Ordinal);
        Require(start >= 0, "F-V15-011: canonical preview snapshot fixture is missing.");
        var end = preview.IndexOf("private static RunnerSnapshot Snap(", start, StringComparison.Ordinal);
        Require(end > start, "F-V15-011: canonical preview snapshot fixture boundary is malformed.");
        var fixture = preview[start..end];

        var snapCount = CountOccurrences(fixture, "Snap(");
        Require(snapCount == 15,
            $"F-V15-011: canonical reference evidence must render exactly 15 managed runner cards; found {snapCount}.");

        foreach (var state in new[] { "RunnerState.IDLE", "RunnerState.BUSY", "RunnerState.OFF", "RunnerState.ERROR", "RunnerState.STARTING", "RunnerState.STOPPING" })
        {
            Require(fixture.Contains(state, StringComparison.Ordinal),
                $"F-V15-011: 15-runner canonical density must preserve state coverage for {state}.");
        }

        Require(fixture.Contains("VERY-LONG-RUNNER-NAME-FOR-ELLIPSIS-VALIDATION", StringComparison.Ordinal),
            "F-V15-011: canonical density correction must preserve long-identity stress coverage.");

        Console.WriteLine("PASS  F-V15-011 canonical 15-runner reference-density contract");
    }

    private static int CountOccurrences(string value, string token)
    {
        var count = 0;
        var index = 0;
        while ((index = value.IndexOf(token, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += token.Length;
        }
        return count;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
