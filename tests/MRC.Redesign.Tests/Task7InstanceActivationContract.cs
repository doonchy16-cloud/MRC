using System.IO.Pipes;
using System.Reflection;
using System.Runtime.CompilerServices;
using MRC.Cli;
using MRC.Core;

internal static class Task7InstanceActivationContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifyActivationProtocol();
        Console.WriteLine("PASS  Task7 existing-GUI activation protocol");
    }

    private static void VerifyActivationProtocol()
    {
        var coreAssembly = typeof(MrcConstants).Assembly;
        var protocolType = coreAssembly.GetType("MRC.Core.InstanceControl.InstanceActivationProtocol");
        Require(protocolType is not null, "InstanceActivationProtocol is missing.");

        var activationMessage = protocolType!.GetProperty("ActivationMessage", BindingFlags.Public | BindingFlags.Static)
            ?.GetValue(null)?.ToString();
        Require(string.Equals(activationMessage, "ACTIVATE", StringComparison.Ordinal),
            $"Activation message is '{activationMessage ?? "<null>"}', expected ACTIVATE.");

        var cliAssembly = typeof(CliDispatcher).Assembly;
        var activatorType = cliAssembly.GetType("MRC.Cli.GuiInstanceActivator");
        Require(activatorType is not null, "GuiInstanceActivator is missing.");
        var ctor = activatorType!.GetConstructor(new[] { typeof(string) });
        Require(ctor is not null, "GuiInstanceActivator(string pipeName) constructor is missing.");
        var activate = activatorType.GetMethod(
            "TryActivate",
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: new[] { typeof(TimeSpan) },
            modifiers: null);
        Require(activate is not null, "GuiInstanceActivator.TryActivate(TimeSpan) is missing.");

        var pipeName = $"MRC.Test.Activation.{Guid.NewGuid():N}";
        using var server = new NamedPipeServerStream(
            pipeName,
            PipeDirection.In,
            1,
            PipeTransmissionMode.Byte,
            PipeOptions.None);

        string? received = null;
        Exception? serverError = null;
        var serverThread = new Thread(() =>
        {
            try
            {
                server.WaitForConnection();
                using var reader = new StreamReader(server, leaveOpen: true);
                received = reader.ReadLine();
            }
            catch (Exception ex)
            {
                serverError = ex;
            }
        })
        {
            IsBackground = true,
            Name = "MRC Task7 activation test server"
        };
        serverThread.Start();

        var activator = ctor!.Invoke(new object[] { pipeName });
        var activationResult = activate!.Invoke(activator, new object[] { TimeSpan.FromSeconds(2) });
        Require(activationResult is not null, "GuiInstanceActivator returned null.");
        var successValue = activationResult!.GetType().GetProperty("Success")?.GetValue(activationResult);
        Require(successValue is bool success && success,
            "GuiInstanceActivator did not report successful activation with a listening pipe.");

        var serverCompleted = serverThread.Join(TimeSpan.FromSeconds(3));
        if (!serverCompleted)
        {
            server.Dispose();
            Require(false, "Activation server did not receive a message within 3 seconds.");
        }

        Require(serverError is null, $"Activation server failed: {serverError?.Message}");
        Require(string.Equals(received, "ACTIVATE", StringComparison.Ordinal),
            $"Activation server received '{received ?? "<null>"}', expected ACTIVATE.");

        var outcomeType = cliAssembly.GetType("MRC.Cli.GuiLaunchOutcome");
        Require(outcomeType is not null && outcomeType.IsEnum, "GuiLaunchOutcome enum is missing.");
        var names = Enum.GetNames(outcomeType!);
        Require(names.SequenceEqual(new[] { "Opened", "AlreadyRunningActivated", "Failed" }),
            $"GuiLaunchOutcome values are wrong: {string.Join(", ", names)}.");

        var resultType = cliAssembly.GetType("MRC.Cli.GuiLaunchResult");
        Require(resultType?.GetProperty("Outcome")?.PropertyType == outcomeType,
            "GuiLaunchResult.Outcome does not expose GuiLaunchOutcome.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
