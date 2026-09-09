namespace MRC.Core.InstanceControl;

public static class InstanceActivationProtocol
{
    public static string MutexName => @"Local\MRC.MainRunnerControl.Gui.v0.1";
    public static string PipeName => "MRC.MainRunnerControl.Gui.Activation.v0.1";
    public static string ActivationMessage => "ACTIVATE";
}
