using System.Reflection;
using System.Runtime.CompilerServices;
using MRC.Cli;

internal static class Task6CliContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifyBareLaunchFeedback().GetAwaiter().GetResult();
        Console.WriteLine("PASS  Task6A bare-MRC launch feedback");

        VerifyVersionTruth().GetAwaiter().GetResult();
        Console.WriteLine("PASS  Task6B truthful version output");

        VerifySemanticPalette();
        Console.WriteLine("PASS  Task6C1 semantic CLI palette");
    }

    private static async Task VerifyBareLaunchFeedback()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var dispatcher = new CliDispatcher(new SuccessfulLauncher());

        var exitCode = await dispatcher.ExecuteAsync(Array.Empty<string>(), output, error);
        var text = output.ToString();

        Require(exitCode == 0, $"Bare MRC success returned exit code {exitCode}.");
        Require(string.IsNullOrWhiteSpace(error.ToString()), $"Bare MRC success wrote to stderr: {error}");
        Require(!string.IsNullOrWhiteSpace(text), "Bare MRC still returns silently after a successful GUI launch.");
        Require(text.Contains("opened", StringComparison.OrdinalIgnoreCase)
                || text.Contains("launch", StringComparison.OrdinalIgnoreCase),
            $"Bare MRC feedback is not meaningful: '{text.Trim()}'.");
    }

    private static async Task VerifyVersionTruth()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var dispatcher = new CliDispatcher(new SuccessfulLauncher());

        var exitCode = await dispatcher.ExecuteAsync(new[] { "--version" }, output, error);
        var text = output.ToString();

        Require(exitCode == 0, $"MRC --version returned exit code {exitCode}.");
        Require(string.IsNullOrWhiteSpace(error.ToString()), $"MRC --version wrote to stderr: {error}");
        Require(text.Contains("Version: 0.0.12", StringComparison.Ordinal),
            $"Version output does not identify v0.0.12: {text}");
        Require(text.Contains("Channel: precert", StringComparison.OrdinalIgnoreCase),
            $"Version output does not identify the precert channel: {text}");
        Require(text.Contains("Stage: PRE-CERTIFICATION", StringComparison.OrdinalIgnoreCase),
            $"Version output does not truthfully identify PRE-CERTIFICATION stage: {text}");
        Require(text.Contains("Final target: 0.1.0", StringComparison.OrdinalIgnoreCase),
            $"Version output does not preserve final target v0.1.0: {text}");
        Require(!text.Contains("\u001b[", StringComparison.Ordinal),
            "Redirected --version output contains ANSI escape sequences.");
    }

    private static void VerifySemanticPalette()
    {
        var assembly = typeof(CliDispatcher).Assembly;
        var toneType = assembly.GetType("MRC.Cli.CliTone");
        Require(toneType is not null && toneType.IsEnum, "CliTone is missing.");

        var expectedNames = new[]
        {
            "Normal", "Heading", "Success", "Warning", "Error", "Path", "Metadata", "Secondary"
        };
        Require(Enum.GetNames(toneType!).SequenceEqual(expectedNames),
            $"CliTone values are wrong: {string.Join(", ", Enum.GetNames(toneType!))}.");

        var palette = assembly.GetType("MRC.Cli.CliPalette");
        Require(palette is not null, "CliPalette is missing.");
        var colorFor = palette!.GetMethod("ColorFor", BindingFlags.Public | BindingFlags.Static);
        Require(colorFor is not null, "CliPalette.ColorFor is missing.");

        var expected = new Dictionary<string, ConsoleColor>(StringComparer.Ordinal)
        {
            ["Normal"] = ConsoleColor.White,
            ["Heading"] = ConsoleColor.Cyan,
            ["Success"] = ConsoleColor.Green,
            ["Warning"] = ConsoleColor.Yellow,
            ["Error"] = ConsoleColor.Red,
            ["Path"] = ConsoleColor.Cyan,
            ["Metadata"] = ConsoleColor.Magenta,
            ["Secondary"] = ConsoleColor.DarkGray
        };

        foreach (var pair in expected)
        {
            var tone = Enum.Parse(toneType!, pair.Key);
            var actual = colorFor!.Invoke(null, new[] { tone });
            Require(actual is ConsoleColor color && color == pair.Value,
                $"CliPalette maps {pair.Key} to {actual ?? "<null>"}; expected {pair.Value}.");
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class SuccessfulLauncher : IGuiLauncher
    {
        public GuiLaunchResult Launch() => new(true, "MRC GUI launch requested.");
    }
}
