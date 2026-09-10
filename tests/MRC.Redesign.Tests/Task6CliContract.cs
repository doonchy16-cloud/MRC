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

        VerifyRedirectSafeRenderer();
        Console.WriteLine("PASS  Task6C2 redirect-safe CLI renderer");

        VerifyVersionToneContract();
        Console.WriteLine("PASS  Task6C3 semantic version presentation");

        VerifyHelpSemantics().GetAwaiter().GetResult();
        Console.WriteLine("PASS  Task6D2B truthful diagnose/Doctor help semantics");
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
        Require(text.Contains("Version: 0.0.15", StringComparison.Ordinal),
            $"Version output does not identify v0.0.15: {text}");
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
            "Normal", "Heading", "Success", "Warning", "Error", "Path", "Metadata", "Secondary", "Command"
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
            ["Secondary"] = ConsoleColor.DarkGray,
            ["Command"] = ConsoleColor.Yellow
        };

        foreach (var pair in expected)
        {
            var tone = Enum.Parse(toneType!, pair.Key);
            var actual = colorFor!.Invoke(null, new[] { tone });
            Require(actual is ConsoleColor color && color == pair.Value,
                $"CliPalette maps {pair.Key} to {actual ?? "<null>"}; expected {pair.Value}.");
        }
    }

    private static void VerifyRedirectSafeRenderer()
    {
        var assembly = typeof(CliDispatcher).Assembly;
        var rendererType = assembly.GetType("MRC.Cli.CliRenderer");
        Require(rendererType is not null, "CliRenderer is missing.");

        var constructor = rendererType!.GetConstructor(new[] { typeof(TextWriter) });
        Require(constructor is not null, "CliRenderer(TextWriter) constructor is missing.");

        var writer = new StringWriter();
        var renderer = constructor!.Invoke(new object[] { writer });
        var colorEnabled = rendererType.GetProperty("ColorEnabled", BindingFlags.Public | BindingFlags.Instance);
        Require(colorEnabled is not null, "CliRenderer.ColorEnabled is missing.");
        Require(colorEnabled!.GetValue(renderer) is false,
            "CliRenderer incorrectly enables terminal colors for a redirected StringWriter.");

        var writeLine = rendererType.GetMethod(
            "WriteLineAsync",
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: new[] { typeof(string), typeof(CliTone) },
            modifiers: null);
        Require(writeLine is not null, "CliRenderer.WriteLineAsync(string, CliTone) is missing.");

        var task = writeLine!.Invoke(renderer, new object[] { "renderer-probe", CliTone.Heading }) as Task;
        Require(task is not null, "CliRenderer.WriteLineAsync did not return a Task.");
        task!.GetAwaiter().GetResult();

        var text = writer.ToString();
        Require(text.Contains("renderer-probe", StringComparison.Ordinal),
            "CliRenderer did not write redirected text.");
        Require(!text.Contains("\u001b[", StringComparison.Ordinal),
            "CliRenderer leaked ANSI escape sequences into redirected text.");
    }

    private static void VerifyVersionToneContract()
    {
        var assembly = typeof(CliDispatcher).Assembly;
        var presentation = assembly.GetType("MRC.Cli.CliPresentation");
        Require(presentation is not null, "CliPresentation is missing.");

        var versionLines = presentation!.GetMethod(
            "VersionLines",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(string) },
            modifiers: null);
        Require(versionLines is not null, "CliPresentation.VersionLines(string) is missing.");

        var result = versionLines!.Invoke(null, new object[] { @"C:\MRC\versions\0.0.15" }) as System.Collections.IEnumerable;
        Require(result is not null, "CliPresentation.VersionLines returned null.");

        var lines = result!.Cast<object>().ToArray();
        Require(lines.Length >= 7, $"Version presentation is incomplete: {lines.Length} line(s).");

        string Text(object line) => line.GetType().GetProperty("Text")?.GetValue(line)?.ToString() ?? string.Empty;
        string Tone(object line) => line.GetType().GetProperty("Tone")?.GetValue(line)?.ToString() ?? string.Empty;

        Require(Tone(lines[0]) == "Heading", $"Version heading tone is {Tone(lines[0])}, expected Heading.");
        foreach (var prefix in new[] { "Version:", "Channel:", "Stage:", "Final target:" })
        {
            var line = lines.SingleOrDefault(item => Text(item).StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            Require(line is not null, $"Version presentation is missing '{prefix}'.");
            Require(Tone(line!) == "Metadata", $"'{prefix}' tone is {Tone(line!)}, expected Metadata.");
        }

        foreach (var prefix in new[] { "Install location:", "Runner root:" })
        {
            var line = lines.SingleOrDefault(item => Text(item).StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            Require(line is not null, $"Version presentation is missing '{prefix}'.");
            Require(Tone(line!) == "Path", $"'{prefix}' tone is {Tone(line!)}, expected Path.");
        }
    }

    private static async Task VerifyHelpSemantics()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var dispatcher = new CliDispatcher(new SuccessfulLauncher());

        var exitCode = await dispatcher.ExecuteAsync(new[] { "--help" }, output, error);
        var text = output.ToString();

        Require(exitCode == 0, $"MRC --help returned exit code {exitCode}.");
        Require(string.IsNullOrWhiteSpace(error.ToString()), $"MRC --help wrote to stderr: {error}");
        Require(text.Contains("-diagnose", StringComparison.OrdinalIgnoreCase),
            $"Help omits the deep diagnose command: {text}");
        Require(text.Contains("read-only", StringComparison.OrdinalIgnoreCase),
            $"Help does not identify diagnose as read-only: {text}");

        var doctorLine = text
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(line => line.Contains("-doctor", StringComparison.OrdinalIgnoreCase));
        Require(doctorLine is not null, $"Help omits Doctor: {text}");
        Require(doctorLine!.Contains("repair", StringComparison.OrdinalIgnoreCase),
            $"Doctor help does not describe repair behavior: {doctorLine}");
        Require(doctorLine.Contains("automatic", StringComparison.OrdinalIgnoreCase)
                || doctorLine.Contains("low-risk", StringComparison.OrdinalIgnoreCase),
            $"Doctor help does not identify automatic low-risk repair semantics: {doctorLine}");
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
