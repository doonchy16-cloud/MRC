using System.Diagnostics;

namespace MRC.Cli;

public interface IGuiLauncher
{
    GuiLaunchResult Launch();
}

public sealed record GuiLaunchResult(bool Success, string Message);

public sealed class GuiLauncher : IGuiLauncher
{
    public GuiLaunchResult Launch()
    {
        var guiPath = Path.Combine(AppContext.BaseDirectory, "MRC.Gui.exe");
        if (!File.Exists(guiPath))
        {
            return new GuiLaunchResult(false, $"MRC GUI executable was not found: {guiPath}");
        }

        try
        {
            Process.Start(new ProcessStartInfo(guiPath)
            {
                WorkingDirectory = AppContext.BaseDirectory,
                UseShellExecute = true
            });

            return new GuiLaunchResult(true, "MRC GUI launch requested.");
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return new GuiLaunchResult(false, $"MRC GUI could not be launched: {ex.Message}");
        }
    }
}
