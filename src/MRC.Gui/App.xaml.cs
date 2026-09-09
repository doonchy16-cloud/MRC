using System.Threading;
using System.Windows;
using MRC.Core.InstanceControl;

namespace MRC.Gui;

public partial class App : Application
{
    private Mutex? _instanceMutex;
    private bool _ownsMutex;
    private InstanceActivationServer? _activationServer;

    protected override void OnStartup(StartupEventArgs e)
    {
        _instanceMutex = new Mutex(initiallyOwned: true, InstanceActivationProtocol.MutexName, out var createdNew);
        _ownsMutex = createdNew;

        if (!createdNew)
        {
            Shutdown(0);
            return;
        }

        base.OnStartup(e);
        MainWindow = new MainWindow();
        MainWindow.Show();
        _activationServer = new InstanceActivationServer();
        _activationServer.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _activationServer?.Dispose();
        _activationServer = null;

        if (_ownsMutex)
        {
            _instanceMutex?.ReleaseMutex();
        }

        _instanceMutex?.Dispose();
        base.OnExit(e);
    }
}
