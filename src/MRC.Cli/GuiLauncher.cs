using System.Diagnostics;

namespace MRC.Cli;

public interface IGuiLauncher
{
    GuiLaunchResult Launch();
}

public enum GuiLaunchOutcome
{
    Opened = 0,
    AlreadyRunningActivated = 1,
    Failed = 2
}

public sealed record GuiLaunchResult(GuiLaunchOutcome Outcome, string Message)
{
    public bool Success => Outcome != GuiLaunchOutcome.Failed;

    public GuiLaunchResult(bool success, string message)
        : this(success ? GuiLaunchOutcome.Opened : GuiLaunchOutcome.Failed, message)
    {
    }
}

public sealed class GuiLauncher : IGuiLauncher
{
    private readonly IGuiInstanceActivator _activator;

    public GuiLauncher()
        : this(new GuiInstanceActivator())
    {
    }

    public GuiLauncher(IGuiInstanceActivator activator)
    {
        _activator = activator ?? throw new ArgumentNullException(nameof(activator));
    }

    public GuiLaunchResult Launch()
    {
        var activation = _activator.TryActivate(TimeSpan.FromMilliseconds(300));
        if (!activation.Success)
        {
            activation = _activator.TryActivate(TimeSpan.FromMilliseconds(300));
        }

        if (activation.Success)
        {
            return new GuiLaunchResult(
                GuiLaunchOutcome.AlreadyRunningActivated,
                "Main Runner Control is already running; existing window activation requested.");
        }

        var guiPath = Path.Combine(AppContext.BaseDirectory, "MRC.Gui.exe");
        if (!File.Exists(guiPath))
        {
            return new GuiLaunchResult(GuiLaunchOutcome.Failed, $"MRC GUI executable was not found: {guiPath}");
        }

        try
        {
            Process.Start(new ProcessStartInfo(guiPath)
            {
                WorkingDirectory = AppContext.BaseDirectory,
                UseShellExecute = true
            });

            return new GuiLaunchResult(GuiLaunchOutcome.Opened, "MRC GUI launch requested.");
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return new GuiLaunchResult(GuiLaunchOutcome.Failed, $"MRC GUI could not be launched: {ex.Message}");
        }
    }
}
