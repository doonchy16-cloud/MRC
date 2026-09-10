using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using MRC.Core.Runtime;

namespace MRC.Core.Control;

internal sealed class WindowsRunnerGracefulShutdown : IRunnerGracefulShutdown
{
    private const uint CtrlCEvent = 0;

    public RunnerGracefulShutdownResult TryShutdown(ProcessSnapshot listener, TimeSpan timeout)
    {
        ArgumentNullException.ThrowIfNull(listener);
        if (!OperatingSystem.IsWindows())
        {
            return new RunnerGracefulShutdownResult(
                RunnerGracefulShutdownOutcome.Unavailable,
                "Graceful console shutdown is supported only on Windows.");
        }

        Process process;
        try
        {
            process = Process.GetProcessById(listener.ProcessId);
        }
        catch (ArgumentException)
        {
            return new RunnerGracefulShutdownResult(
                RunnerGracefulShutdownOutcome.Exited,
                "Runner listener had already exited.");
        }
        catch (Exception ex)
        {
            return new RunnerGracefulShutdownResult(
                RunnerGracefulShutdownOutcome.Failed,
                $"Could not open listener PID {listener.ProcessId}: {ex.Message}");
        }

        using (process)
        {
            try
            {
                if (process.HasExited)
                {
                    return new RunnerGracefulShutdownResult(
                        RunnerGracefulShutdownOutcome.Exited,
                        "Runner listener had already exited.");
                }

                if (!AttachConsole((uint)listener.ProcessId))
                {
                    return new RunnerGracefulShutdownResult(
                        RunnerGracefulShutdownOutcome.Unavailable,
                        $"Could not attach to listener console: {new Win32Exception(Marshal.GetLastWin32Error()).Message}");
                }

                var handlerDisabled = false;
                try
                {
                    // This mirrors the GitHub Actions Windows runner service shutdown path:
                    // protect the controller from the generated Ctrl+C, signal the attached
                    // listener console, then wait for the listener to complete its own shutdown.
                    if (!SetConsoleCtrlHandler(null, true))
                    {
                        return new RunnerGracefulShutdownResult(
                            RunnerGracefulShutdownOutcome.Failed,
                            $"Could not protect MRC from console control signal: {new Win32Exception(Marshal.GetLastWin32Error()).Message}");
                    }
                    handlerDisabled = true;

                    if (!GenerateConsoleCtrlEvent(CtrlCEvent, 0))
                    {
                        return new RunnerGracefulShutdownResult(
                            RunnerGracefulShutdownOutcome.Failed,
                            $"Could not send Ctrl+C to listener console: {new Win32Exception(Marshal.GetLastWin32Error()).Message}");
                    }

                    var timeoutMs = timeout <= TimeSpan.Zero
                        ? 1
                        : (int)Math.Min(int.MaxValue, Math.Ceiling(timeout.TotalMilliseconds));
                    if (process.WaitForExit(timeoutMs))
                    {
                        return new RunnerGracefulShutdownResult(
                            RunnerGracefulShutdownOutcome.Exited,
                            "Runner accepted graceful Ctrl+C shutdown and listener exited.");
                    }

                    return new RunnerGracefulShutdownResult(
                        RunnerGracefulShutdownOutcome.TimedOut,
                        $"Runner did not exit within {timeout.TotalSeconds:0} seconds of graceful Ctrl+C shutdown.");
                }
                finally
                {
                    FreeConsole();
                    if (handlerDisabled)
                    {
                        SetConsoleCtrlHandler(null, false);
                    }
                }
            }
            catch (Exception ex)
            {
                return new RunnerGracefulShutdownResult(
                    RunnerGracefulShutdownOutcome.Failed,
                    $"Graceful runner shutdown failed: {ex.Message}");
            }
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GenerateConsoleCtrlEvent(uint ctrlEvent, uint processGroupId);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AttachConsole(uint processId);

    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FreeConsole();

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate? handlerRoutine, bool add);

    private delegate bool ConsoleCtrlDelegate(uint ctrlType);
}
