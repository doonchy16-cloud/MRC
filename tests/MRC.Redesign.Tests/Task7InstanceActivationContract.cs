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
        VerifyActivationProtocol().GetAwaiter().GetResult();
        Console.WriteLine("PASS  Task7 existing-GUI activation protocol");
    }

    private static async Task VerifyActivationProtocol()
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
            PipeOptions.Asynchronous);

        var receiveTask = Task.Run(async () =>
        {
            await server.WaitForConnectionAsync();
            using var reader = new StreamReader(server);
            return await reader.ReadLineAsync();
        });

        var activator = ctor!.Invoke(new object[] { pipeName });
        var activationResult = activate!.Invoke(activator, new object[] { TimeSpan.FromSeconds(2) });
        Require(activationResult is not null, "GuiInstanceActivator returned null.");
        var success = activationResult!.GetType().GetProperty("Success")?.GetValue(activationResult) as bool?;
        Require(success == true, "GuiInstanceActivator did not report successful activation with a listening pipe.");

        var received = await receiveTask.WaitAsync(TimeSpan.FromSeconds(3));
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
