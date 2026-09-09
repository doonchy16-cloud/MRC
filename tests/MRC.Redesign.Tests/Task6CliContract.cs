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

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class SuccessfulLauncher : IGuiLauncher
    {
        public GuiLaunchResult Launch() => new(true, "MRC GUI launch requested.");
    }
}
