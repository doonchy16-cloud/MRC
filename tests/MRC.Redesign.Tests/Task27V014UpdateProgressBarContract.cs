using System.Runtime.CompilerServices;
using MRC.Cli;
using MRC.Core.Updating;

internal static class Task27V014UpdateProgressBarContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifyTruthfulPercentDrivenBar();
        VerifyNullPercentDoesNotInventProgress();
        VerifyReusableFormatterAuthority();
        Console.WriteLine("PASS  Task27 v0.0.14 truthful update progress bar contract");
    }

    private static void VerifyTruthfulPercentDrivenBar()
    {
        var zero = CliPresentation.UpdateProgressLine(new UpdateProgress(UpdateProgressStage.Resolve, "Resolving release metadata", 0));
        var half = CliPresentation.UpdateProgressLine(new UpdateProgress(UpdateProgressStage.Download, "Downloading package", 50));
        var full = CliPresentation.UpdateProgressLine(new UpdateProgress(UpdateProgressStage.Complete, "Update complete", 100));

        Require(zero.Text.Contains("[░░░░░░░░░░░░░░░░░░░░]", StringComparison.Ordinal)
                && zero.Text.Contains("  0%", StringComparison.Ordinal),
            $"0% progress bar is not empty and truthful: {zero.Text}");
        Require(half.Text.Contains("[██████████░░░░░░░░░░]", StringComparison.Ordinal)
                && half.Text.Contains(" 50%", StringComparison.Ordinal),
            $"50% progress bar is not half-filled and truthful: {half.Text}");
        Require(full.Text.Contains("[████████████████████]", StringComparison.Ordinal)
                && full.Text.Contains("100%", StringComparison.Ordinal),
            $"100% progress bar is not fully filled and truthful: {full.Text}");

        Require(half.Segments.Any(segment => segment.Text.Contains("██████████", StringComparison.Ordinal)
                                             && segment.Tone == CliTone.Heading),
            "Filled Download progress is not using the stage semantic tone.");
        Require(half.Segments.Any(segment => segment.Text.Contains("░░░░░░░░░░", StringComparison.Ordinal)
                                             && segment.Tone == CliTone.Secondary),
            "Unfilled progress is not visually secondary.");
        Require(full.Segments.Any(segment => segment.Text.Contains("Complete", StringComparison.Ordinal)
                                             && segment.Tone == CliTone.Success),
            "Complete stage lost its semantic success tone.");
    }

    private static void VerifyNullPercentDoesNotInventProgress()
    {
        var line = CliPresentation.UpdateProgressLine(new UpdateProgress(
            UpdateProgressStage.Compare,
            "Comparing installed and available versions",
            Percent: null));

        Require(!line.Text.Contains('█') && !line.Text.Contains('░') && !line.Text.Contains('%'),
            $"Null updater percentage fabricated progress presentation: {line.Text}");
        Require(line.Text.Contains("Compare", StringComparison.Ordinal)
                && line.Text.Contains("Comparing installed and available versions", StringComparison.Ordinal),
            "Null-percent update progress lost the truthful stage/message fallback.");
    }

    private static void VerifyReusableFormatterAuthority()
    {
        var root = RepoRoot();
        var formatter = Path.Combine(root, "src", "MRC.Cli", "CliProgressBarFormatter.cs");
        Require(File.Exists(formatter), "Central reusable CLI progress-bar formatter is missing.");

        var presentation = File.ReadAllText(Path.Combine(root, "src", "MRC.Cli", "CliPresentation.cs"));
        Require(presentation.Contains("CliProgressBarFormatter", StringComparison.Ordinal),
            "Update progress still uses ad hoc percentage text instead of the reusable formatter.");

        var source = File.ReadAllText(formatter);
        Require(source.Contains("DefaultCells = 20", StringComparison.Ordinal),
            "Progress bar does not have the locked bounded 20-cell default width.");
        Require(!source.Contains("Timer", StringComparison.OrdinalIgnoreCase)
                && !source.Contains("Delay", StringComparison.OrdinalIgnoreCase),
            "Progress-bar formatter contains time-driven/fake-progress behavior.");
    }

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Auth", "0000_MasterAuth.md"))) return directory.FullName;
            directory = directory.Parent;
        }
        throw new InvalidOperationException("Could not locate MRC repository root.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
