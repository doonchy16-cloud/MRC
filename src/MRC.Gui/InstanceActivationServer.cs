using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using MRC.Core.InstanceControl;

namespace MRC.Gui;

internal sealed class InstanceActivationServer : IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private Task? _loop;

    public void Start()
    {
        _loop ??= Task.Run(ListenLoopAsync);
    }

    private async Task ListenLoopAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                using var server = new NamedPipeServerStream(
                    InstanceActivationProtocol.PipeName,
                    PipeDirection.In,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);
                await server.WaitForConnectionAsync(_cts.Token);
                using var reader = new StreamReader(server);
                var message = await reader.ReadLineAsync(_cts.Token);
                if (string.Equals(message, InstanceActivationProtocol.ActivationMessage, StringComparison.Ordinal))
                {
                    await Application.Current.Dispatcher.InvokeAsync(ActivateMainWindow);
                }
            }
            catch (OperationCanceledException) when (_cts.IsCancellationRequested)
            {
                break;
            }
            catch
            {
                if (_cts.IsCancellationRequested) break;
                await Task.Delay(100, _cts.Token).ConfigureAwait(false);
            }
        }
    }

    private static void ActivateMainWindow()
    {
        var window = Application.Current.MainWindow;
        if (window is null) return;
        if (window.WindowState == WindowState.Minimized) window.WindowState = WindowState.Normal;
        if (!window.IsVisible) window.Show();
        window.Activate();
        window.Focus();
        var handle = new WindowInteropHelper(window).Handle;
        if (handle != IntPtr.Zero) SetForegroundWindow(handle);
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
}
