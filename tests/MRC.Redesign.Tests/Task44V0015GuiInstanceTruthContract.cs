using System.Runtime.CompilerServices;
using MRC.Cli;

internal static class Task44V0015GuiInstanceTruthContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        var result = new GuiLauncher(new SequenceActivator(false, true)).Launch();

        Require(result.Outcome == GuiLaunchOutcome.AlreadyRunningActivated,
            $"Second bounded activation probe was not honored; outcome was {result.Outcome}: {result.Message}");
        Require(result.Success,
            "Existing-GUI activation must remain a successful bare-MRC outcome.");
        Require(result.Message.Contains("already running", StringComparison.OrdinalIgnoreCase),
            $"Existing-GUI activation message lost single-instance truth: {result.Message}");
    }

    private sealed class SequenceActivator : IGuiInstanceActivator
    {
        private readonly Queue<bool> _results;

        public SequenceActivator(params bool[] results) => _results = new Queue<bool>(results);

        public GuiActivationResult TryActivate(TimeSpan timeout)
        {
            var success = _results.Count > 0 && _results.Dequeue();
            return new GuiActivationResult(success, success ? "activated" : "not found");
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
