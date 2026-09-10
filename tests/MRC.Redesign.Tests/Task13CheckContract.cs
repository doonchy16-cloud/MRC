using System.Runtime.CompilerServices;
using MRC.Cli;
using MRC.Core;
using MRC.Core.Diagnostics;
using MRC.Core.Updating;

internal static class Task13CheckContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifyUpdateAvailableAsync().GetAwaiter().GetResult();
        Console.WriteLine("PASS  Task13A --check reports update available without mutation");

        VerifyUpToDateAsync().GetAwaiter().GetResult();
        Console.WriteLine("PASS  Task13B --check reports up to date without mutation");

        VerifySemanticPresentation();
        Console.WriteLine("PASS  Task13C --check semantic colors and white labels");
    }

    private static async Task VerifyUpdateAvailableAsync()
    {
        var updateCalls = 0;
        var checkCalls = 0;
        var dispatcher = new CliDispatcher(
            new SuccessfulLauncher(),
            static (_, _) => Task.FromResult(HealthyDoctor()),
            (_, _) =>
            {
                updateCalls++;
                throw new InvalidOperationException("--check must never invoke the update mutation pipeline.");
            },
            static () => new EnvironmentFenceResult(true, "AUTHORIZED", "authorized"),
            _ =>
            {
                checkCalls++;
                return Task.FromResult(new UpdateCheckResult(
                    UpdateCheckOutcome.UpdateAvailable,
                    new Version(0, 0, 12),
                    new Version(0, 0, 13)));
            });

        var output = new StringWriter();
        var error = new StringWriter();
        var exitCode = await dispatcher.ExecuteAsync(new[] { "--check" }, output, error);
        var text = output.ToString();

        Require(exitCode == 0, $"--check update-available exit code was {exitCode}.");
        Require(checkCalls == 1, $"--check resolver called {checkCalls} times; expected exactly once.");
        Require(updateCalls == 0, $"--check invoked update mutation pipeline {updateCalls} time(s).");
        Require(string.IsNullOrWhiteSpace(error.ToString()), $"--check wrote unexpected stderr: {error}");
        Require(text.Contains("Current: 0.0.12", StringComparison.Ordinal), $"--check omitted current version: {text}");
        Require(text.Contains("Available: 0.0.13", StringComparison.Ordinal), $"--check omitted available version: {text}");
        Require(text.Contains("Status: UPDATE AVAILABLE", StringComparison.Ordinal), $"--check omitted UPDATE AVAILABLE status: {text}");
        Require(!text.Contains("\u001b[", StringComparison.Ordinal), "Redirected --check output contains ANSI escape sequences.");
    }

    private static async Task VerifyUpToDateAsync()
    {
        var updateCalls = 0;
        var dispatcher = new CliDispatcher(
            new SuccessfulLauncher(),
            static (_, _) => Task.FromResult(HealthyDoctor()),
            (_, _) =>
            {
                updateCalls++;
                throw new InvalidOperationException("--check must never invoke the update mutation pipeline.");
            },
            static () => new EnvironmentFenceResult(true, "AUTHORIZED", "authorized"),
            _ => Task.FromResult(new UpdateCheckResult(
                UpdateCheckOutcome.UpToDate,
                new Version(0, 0, 13),
                new Version(0, 0, 13))));

        var output = new StringWriter();
        var error = new StringWriter();
        var exitCode = await dispatcher.ExecuteAsync(new[] { "--check" }, output, error);
        var text = output.ToString();

        Require(exitCode == 0, $"--check up-to-date exit code was {exitCode}.");
        Require(updateCalls == 0, $"--check invoked update mutation pipeline {updateCalls} time(s).");
        Require(text.Contains("Status: UP TO DATE", StringComparison.Ordinal), $"--check omitted UP TO DATE status: {text}");
    }

    private static void VerifySemanticPresentation()
    {
        var available = CliPresentation.UpdateCheckLines(new UpdateCheckResult(
            UpdateCheckOutcome.UpdateAvailable,
            new Version(0, 0, 12),
            new Version(0, 0, 13)));
        var availableStatus = available.Single(line => line.Text.StartsWith("Status: ", StringComparison.Ordinal));
        Require(availableStatus.Segments[0].Text == "Status: " && availableStatus.Segments[0].Tone == CliTone.Normal,
            "--check Status label is not white/Normal.");
        Require(availableStatus.Segments[1].Text == "UPDATE AVAILABLE" && availableStatus.Segments[1].Tone == CliTone.Heading,
            "UPDATE AVAILABLE is not cyan/Heading.");

        var current = available.Single(line => line.Text.StartsWith("Current: ", StringComparison.Ordinal));
        Require(current.Segments[0].Tone == CliTone.Normal && current.Segments[1].Tone == CliTone.Metadata,
            "--check Current label/value semantics are incorrect.");

        var upToDate = CliPresentation.UpdateCheckLines(new UpdateCheckResult(
            UpdateCheckOutcome.UpToDate,
            new Version(0, 0, 13),
            new Version(0, 0, 13)));
        var upToDateStatus = upToDate.Single(line => line.Text.StartsWith("Status: ", StringComparison.Ordinal));
        Require(upToDateStatus.Segments[1].Text == "UP TO DATE" && upToDateStatus.Segments[1].Tone == CliTone.Success,
            "UP TO DATE is not green/Success.");
    }

    private static DoctorReport HealthyDoctor() => new(
        Array.Empty<DoctorFinding>(),
        Array.Empty<DoctorRepairAction>(),
        Array.Empty<DoctorVerificationResult>(),
        DoctorHealth.Healthy);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class SuccessfulLauncher : IGuiLauncher
    {
        public GuiLaunchResult Launch() => new(true, "MRC GUI launch requested.");
    }
}
