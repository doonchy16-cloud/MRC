using System.Runtime.CompilerServices;
using MRC.Cli;
using MRC.Core;

internal static class Task21V014BareLaunchContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifySemanticOpenedPresentation();
        VerifySemanticRestorePresentation();
        VerifyDispatcherUsesCentralPresentationAsync().GetAwaiter().GetResult();
        Console.WriteLine("PASS  Task21 v0.0.14 bare MRC launch uses rich semantic CLI presentation");
    }

    private static void VerifySemanticOpenedPresentation()
    {
        var lines = CliPresentation.GuiLaunchLines(new GuiLaunchResult(GuiLaunchOutcome.Opened, "MRC GUI launch requested."));

        Require(lines.Count >= 5, $"Bare MRC OPENED presentation is too flat: expected at least 5 lines, got {lines.Count}.");
        Require(lines[0].Tone == CliTone.Heading && lines[0].Text.Contains("MRC", StringComparison.Ordinal),
            "Bare MRC presentation is missing a cyan/Heading MRC identity line.");

        AssertField(lines, "Version: ", BuildInfo.Version, CliTone.Metadata);
        AssertField(lines, "Status: ", "OPENED", CliTone.Success);
        AssertField(lines, "Window: ", "NEW INSTANCE", CliTone.Heading);
        AssertField(lines, "Result: ", "READY", CliTone.Success);
    }

    private static void VerifySemanticRestorePresentation()
    {
        var lines = CliPresentation.GuiLaunchLines(new GuiLaunchResult(
            GuiLaunchOutcome.AlreadyRunningActivated,
            "Main Runner Control is already running; existing window activation requested."));

        Require(lines.Count >= 5, "Bare MRC restore presentation must remain multi-line.");
        AssertField(lines, "Status: ", "ALREADY RUNNING", CliTone.Success);
        AssertField(lines, "Window: ", "RESTORED + FOCUSED", CliTone.Heading);
        AssertField(lines, "Result: ", "READY", CliTone.Success);
    }

    private static async Task VerifyDispatcherUsesCentralPresentationAsync()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var dispatcher = new CliDispatcher(new FakeLauncher(new GuiLaunchResult(GuiLaunchOutcome.Opened, "launch requested")));

        var exitCode = await dispatcher.ExecuteAsync(Array.Empty<string>(), output, error);
        var text = output.ToString();

        Require(exitCode == 0, $"Bare MRC OPENED exit code was {exitCode}.");
        Require(string.IsNullOrWhiteSpace(error.ToString()), $"Bare MRC OPENED wrote unexpected stderr: {error}");
        Require(text.Contains("Version: ", StringComparison.Ordinal)
                && text.Contains("Status: OPENED", StringComparison.Ordinal)
                && text.Contains("Window: NEW INSTANCE", StringComparison.Ordinal)
                && text.Contains("Result: READY", StringComparison.Ordinal),
            $"Bare MRC dispatcher output did not use the rich launch presentation:\n{text}");
        Require(text.Count(ch => ch == '\n') >= 4, "Bare MRC dispatcher output is still effectively one-line.");
        Require(!text.Contains("\u001b[", StringComparison.Ordinal), "Redirected bare MRC output contains ANSI escape sequences.");
    }

    private static void AssertField(IReadOnlyList<CliLine> lines, string label, string value, CliTone valueTone)
    {
        var line = lines.SingleOrDefault(candidate => candidate.Text == label + value)
            ?? throw new InvalidOperationException($"Missing bare MRC field '{label}{value}'.");

        Require(line.Segments.Count >= 2, $"Field '{label}' is not segmented.");
        Require(line.Segments[0].Text == label && line.Segments[0].Tone == CliTone.Normal,
            $"Field label '{label}' must be white/Normal.");
        Require(line.Segments[1].Text == value && line.Segments[1].Tone == valueTone,
            $"Field value '{value}' has tone {line.Segments[1].Tone}; expected {valueTone}.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class FakeLauncher : IGuiLauncher
    {
        private readonly GuiLaunchResult _result;
        public FakeLauncher(GuiLaunchResult result) => _result = result;
        public GuiLaunchResult Launch() => _result;
    }
}
