using System.Runtime.CompilerServices;
using MRC.Cli;

internal static class Task12HelpContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifyHelpLayoutAndSemanticHierarchy();
        Console.WriteLine("PASS  Task12 v0.0.13 help layout and semantic hierarchy");
    }

    private static void VerifyHelpLayoutAndSemanticHierarchy()
    {
        var lines = CliPresentation.HelpLines();
        Require(lines.Count >= 10, $"Help presentation is unexpectedly sparse: {lines.Count} lines.");

        var version = lines.SingleOrDefault(line => line.Text.Contains("MRC --version", StringComparison.Ordinal));
        Require(version is not null, "Help does not present canonical MRC --version.");
        Require(version!.Text.Contains("Show installed version information", StringComparison.Ordinal),
            "Version description is still orphaned onto a separate line.");
        Require(version.Text.Length <= 100, $"Version help row is too wide: {version.Text.Length} characters.");
        Require(version.Segments.Any(segment => segment.Text.Contains("MRC --version", StringComparison.Ordinal) && segment.Tone == CliTone.Heading),
            "Canonical --version command is not visually primary.");
        Require(version.Segments.Any(segment => segment.Text.Contains("Show installed version information", StringComparison.Ordinal) && segment.Tone == CliTone.Normal),
            "Version help description is not ordinary white prose.");

        var versionAliases = lines.SingleOrDefault(line => line.Text.Contains("-v", StringComparison.Ordinal)
                                                           && line.Text.Contains("-version", StringComparison.Ordinal)
                                                           && !line.Text.Contains("--version", StringComparison.Ordinal));
        Require(versionAliases is not null, "Version aliases are not presented separately from the canonical command.");
        Require(versionAliases!.Segments.All(segment => segment.Tone is CliTone.Normal or CliTone.Secondary),
            "Version aliases are too visually dominant.");

        foreach (var canonical in new[] { "--help", "--doctor", "--diagnose", "--update", "--check" })
        {
            var row = lines.SingleOrDefault(line => line.Text.Contains($"MRC {canonical}", StringComparison.Ordinal));
            Require(row is not null, $"Help is missing canonical MRC {canonical}.");
            Require(row!.Text.Length <= 100, $"Help row for {canonical} is too wide: {row.Text.Length} characters.");
            Require(row.Segments.Any(segment => segment.Text.Contains($"MRC {canonical}", StringComparison.Ordinal) && segment.Tone == CliTone.Heading),
                $"Canonical {canonical} is not visually primary.");
        }

        var doctor = lines.Single(line => line.Text.Contains("MRC --doctor", StringComparison.Ordinal));
        Require(doctor.Text.Contains("automatic", StringComparison.OrdinalIgnoreCase)
                && doctor.Text.Contains("low-risk", StringComparison.OrdinalIgnoreCase)
                && doctor.Text.Contains("repair", StringComparison.OrdinalIgnoreCase),
            "Doctor help lost its automatic low-risk repair semantics.");

        var diagnose = lines.Single(line => line.Text.Contains("MRC --diagnose", StringComparison.Ordinal));
        Require(diagnose.Text.Contains("read-only", StringComparison.OrdinalIgnoreCase),
            "Diagnose help lost its read-only semantics.");

        var check = lines.Single(line => line.Text.Contains("MRC --check", StringComparison.Ordinal));
        Require(check.Text.Contains("check", StringComparison.OrdinalIgnoreCase)
                && check.Text.Contains("update", StringComparison.OrdinalIgnoreCase),
            "--check help does not explain its read-only update-check purpose.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
