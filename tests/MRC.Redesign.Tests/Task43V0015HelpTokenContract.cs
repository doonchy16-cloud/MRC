using System.Runtime.CompilerServices;
using MRC.Cli;

internal static class Task43V0015HelpTokenContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        var lines = CliPresentation.HelpLines(108);
        var version = lines.Single(item => item.Text.StartsWith("MRC --version", StringComparison.Ordinal));
        Require(version.Segments.Any(segment => segment.Text == "MRC" && segment.Tone == CliTone.Command),
            "MRC token is not rendered in command tone.");
        Require(version.Segments.Any(segment => segment.Text.Contains("--version", StringComparison.Ordinal)
                                                && segment.Tone == CliTone.Heading),
            "Option token is not rendered in cyan heading tone.");

        var bare = lines.Single(item => item.Text.StartsWith("MRC", StringComparison.Ordinal)
                                       && item.Text.TrimEnd() == "MRC");
        Require(bare.Segments.Any(segment => segment.Text.TrimEnd() == "MRC" && segment.Tone == CliTone.Command),
            "Bare MRC command is not rendered in command tone.");

        var heading = lines.First();
        Require(heading.Text == "MRC // COMMAND REFERENCE", "Help heading text drifted.");
        Require(heading.Segments.Any(segment => segment.Text == "MRC" && segment.Tone == CliTone.Command),
            "Help heading MRC token is not rendered in command tone.");
        Require(heading.Segments.Any(segment => segment.Text == " // COMMAND REFERENCE" && segment.Tone == CliTone.Heading),
            "Help heading suffix is not rendered in cyan heading tone.");

        Require(CliPalette.ColorFor(CliTone.Command) == ConsoleColor.Yellow,
            "Command tone is not mapped to ConsoleColor.Yellow.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
