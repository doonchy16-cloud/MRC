using System.IO.Pipes;
using MRC.Core.InstanceControl;

namespace MRC.Cli;

public sealed record GuiActivationResult(bool Success, string Message);

public interface IGuiInstanceActivator
{
    GuiActivationResult TryActivate(TimeSpan timeout);
}

public sealed class GuiInstanceActivator : IGuiInstanceActivator
{
    private readonly string _pipeName;

    public GuiInstanceActivator()
        : this(InstanceActivationProtocol.PipeName)
    {
    }

    public GuiInstanceActivator(string pipeName)
    {
        if (string.IsNullOrWhiteSpace(pipeName)) throw new ArgumentException("Pipe name is required.", nameof(pipeName));
        _pipeName = pipeName;
    }

    public GuiActivationResult TryActivate(TimeSpan timeout)
    {
        var timeoutMs = (int)Math.Clamp(timeout.TotalMilliseconds, 1, int.MaxValue);
        try
        {
            using var client = new NamedPipeClientStream(".", _pipeName, PipeDirection.Out, PipeOptions.None);
            client.Connect(timeoutMs);
            using var writer = new StreamWriter(client) { AutoFlush = true };
            writer.WriteLine(InstanceActivationProtocol.ActivationMessage);
            return new GuiActivationResult(true, "Existing MRC window activation requested.");
        }
        catch (Exception ex) when (ex is TimeoutException or IOException or UnauthorizedAccessException)
        {
            return new GuiActivationResult(false, $"No activatable MRC GUI instance answered: {ex.Message}");
        }
    }
}
