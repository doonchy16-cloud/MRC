using System.Runtime.CompilerServices;
using MRC.Cli;

internal static class Task6CliContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifyBareLaunchFeedback().GetAwaiter().GetResult();
        Console.WriteLine("PASS  Task6A bare-MRC launch feedback");
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

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class SuccessfulLauncher : IGuiLauncher
    {
        public GuiLaunchResult Launch() => new(true, "MRC GUI launch requested.");
    }
}
