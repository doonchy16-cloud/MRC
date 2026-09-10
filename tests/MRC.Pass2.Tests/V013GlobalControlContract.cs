using System.Runtime.CompilerServices;
using System.Text.Json;
using MRC.Core;
using MRC.Core.Control;
using MRC.Core.Runners;
using MRC.Core.Runtime;

namespace MRC.Pass2.Tests;

internal static class V013GlobalControlContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        UnrelatedUnresolvedRunnerProcessMustNotPoisonOffManagedRunner();
        Console.WriteLine("PASS  V013 P0-A unrelated unresolved process isolation");
    }

    private static void UnrelatedUnresolvedRunnerProcessMustNotPoisonOffManagedRunner()
    {
        using var temp = new TempDirectory();
        CreateRunner(temp.Path, "runner-idle", "IdleRunner", "https://github.com/example/IdleRepo");
        CreateRunner(temp.Path, "runner-off", "OffRunner", "https://github.com/example/OffRepo");

        var runners = RunnerDiscovery.Discover(temp.Path);
        var idleRunner = runners.Single(runner => runner.AgentName == "IdleRunner");
        var offRunner = runners.Single(runner => runner.AgentName == "OffRunner");
        var idleListener = new ProcessSnapshot(
            501,
            1,
            "Runner.Listener",
            RunnerPath.ListenerExecutable(idleRunner.DirectoryPath));

        // Represents a machine-level runner process whose executable path cannot be read,
        // e.g. a service-hosted external runner. It is NOT positively associated with OffRunner.
        var unresolvedSystemListener = new ProcessSnapshot(
            9001,
            7000,
            "Runner.Listener",
            null,
            "Access is denied.",
            0);

        var provider = new FixedProvider(new ProcessInventory(
            new[] { idleListener, unresolvedSystemListener },
            true,
            null));
        var engine = new RunnerEngine(
            temp.Path,
            provider,
            new NoopLauncher(),
            new NoopTerminator(),
            () => new DateTimeOffset(2026, 9, 9, 18, 0, 0, TimeSpan.Zero));

        var report = engine.RefreshReport();
        var idle = report.ManagedRunners.Single(snapshot => snapshot.Runner.AgentName == "IdleRunner");
        var off = report.ManagedRunners.Single(snapshot => snapshot.Runner.AgentName == "OffRunner");

        Require(idle.State == RunnerState.IDLE,
            $"Exact managed listener should remain IDLE, got {idle.State}.");
        Require(off.State == RunnerState.OFF,
            $"Unrelated unresolved system runner contaminated managed OFF state: got {off.State} with '{off.Error ?? "<no error>"}'.");
        Require(report.SystemFindings.Count == 1,
            $"Expected one separate system finding, got {report.SystemFindings.Count}.");
        Require(report.SystemFindings[0].Kind == RunnerSystemFindingKind.Unattributed,
            $"Unresolved process should remain separate UNATTRIBUTED evidence, got {report.SystemFindings[0].Kind}.");
    }

    private static void CreateRunner(string root, string folder, string agentName, string gitHubUrl)
    {
        var runner = Path.Combine(root, folder);
        Directory.CreateDirectory(Path.Combine(runner, "bin"));
        File.WriteAllText(Path.Combine(runner, ".runner"), JsonSerializer.Serialize(new
        {
            agentName,
            gitHubUrl,
            agentId = 123,
            workFolder = "_work"
        }));
        File.WriteAllText(Path.Combine(runner, "run.cmd"), "@echo off\r\n");
        File.WriteAllText(Path.Combine(runner, "run-helper.cmd.template"), "@echo off\r\n");
        File.WriteAllBytes(Path.Combine(runner, "bin", "Runner.Listener.exe"), Array.Empty<byte>());
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class FixedProvider : IProcessSnapshotProvider
    {
        private readonly ProcessInventory _inventory;
        public FixedProvider(ProcessInventory inventory) => _inventory = inventory;
        public ProcessInventory Capture() => _inventory;
    }

    private sealed class NoopLauncher : IRunnerLauncher
    {
        public void Launch(RunnerDescriptor runner) =>
            throw new InvalidOperationException("Launcher must not be reached by refresh-only contract.");
    }

    private sealed class NoopTerminator : IRunnerProcessTerminator
    {
        public RunnerTerminationResult Terminate(RunnerDescriptor runner, ProcessSnapshot listener) =>
            throw new InvalidOperationException("Terminator must not be reached by refresh-only contract.");
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "mrc-v013-global-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try { Directory.Delete(Path, true); } catch { }
        }
    }
}
