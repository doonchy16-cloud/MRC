using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using MRC.Cli;

internal static class Task11CliSegmentsContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifySegmentedVersionLabelValueLine();
        Console.WriteLine("PASS  Task11A v0.0.13 segmented label/value semantic line");
    }

    private static void VerifySegmentedVersionLabelValueLine()
    {
        var lines = CliPresentation.VersionLines(@"C:\MRC\versions\0.0.13");
        var versionLine = lines.SingleOrDefault(line =>
            line.Text.StartsWith("Version:", StringComparison.Ordinal));

        Require(versionLine is not null, "V0.0.13 RED: Version presentation is missing the Version: line.");

        var segmentsProperty = versionLine!.GetType().GetProperty(
            "Segments",
            BindingFlags.Public | BindingFlags.Instance);

        Require(
            segmentsProperty is not null,
            "V0.0.13 RED: CliLine.Segments is missing; one logical label/value line still has only one semantic tone.");

        var segments = (segmentsProperty!.GetValue(versionLine) as IEnumerable)?
            .Cast<object>()
            .ToArray();

        Require(segments is { Length: >= 2 },
            "V0.0.13 RED: Version: must contain at least a label segment and a value segment.");

        string SegmentText(object segment) =>
            segment.GetType().GetProperty("Text")?.GetValue(segment)?.ToString() ?? string.Empty;
        string SegmentTone(object segment) =>
            segment.GetType().GetProperty("Tone")?.GetValue(segment)?.ToString() ?? string.Empty;

        Require(SegmentText(segments![0]) == "Version: ",
            $"V0.0.13 RED: first Version segment text is '{SegmentText(segments[0])}', expected 'Version: '.");
        Require(SegmentTone(segments[0]) == nameof(CliTone.Normal),
            $"V0.0.13 RED: Version label tone is {SegmentTone(segments[0])}, expected {nameof(CliTone.Normal)} (white).");
        Require(SegmentTone(segments[1]) == nameof(CliTone.Metadata),
            $"V0.0.13 RED: Version value tone is {SegmentTone(segments[1])}, expected {nameof(CliTone.Metadata)} (purple).");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
