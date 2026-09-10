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

        NormalStopMustPreferGracefulShutdownAndVerifyOff();
        Console.WriteLine("PASS  V013 P0-B graceful normal-stop lifecycle");

        HardKillMustBeFallbackAndStillVerifyOff();
        Console.WriteLine("PASS  V013 P0-C verified hard-kill fallback lifecycle");

        VerifiedStopMustReturnOffAndClearTransition();
        Console.WriteLine("PASS  V013 P0-D control reports OFF only after verified stop");

        ProvenExternalServiceMustNotBlockManagedStartAndMustClassifyExternal();
        Console.WriteLine("PASS  V013 P0-E proven external service ownership is shared with control/runtime");

        GenuinelyUnattributedRunnerMustBlockManagedStart();
        Console.WriteLine("PASS  V013 P0-F unattributed runner evidence remains fail-closed for start");
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
            () => Now);

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

    private static void NormalStopMustPreferGracefulShutdownAndVerifyOff()
    {
        using var temp = new TempDirectory();
        CreateRunner(temp.Path, "runner-a", "RunnerA", "https://github.com/example/RepoA");
        var runner = RunnerDiscovery.Discover(temp.Path).Single();
        var listener = new ProcessSnapshot(601, 1, "Runner.Listener", RunnerPath.ListenerExecutable(runner.DirectoryPath));
        var provider = new SequenceProvider(Inventory(listener));
        var graceful = new FakeGracefulShutdown(RunnerGracefulShutdownOutcome.Exited);
        var exitVerifier = new FakeExitVerifier(true);
        var killer = new FakeKiller();
        var terminator = new WindowsRunnerProcessTerminator(
            provider,
            new FixedPathReader(listener.ProcessId, listener.ExecutablePath!),
            killer,
            graceful,
            exitVerifier);

        var result = terminator.Terminate(runner, listener);

        Require(result.Outcome == RunnerTerminationOutcome.Terminated,
            $"Graceful verified stop should terminate successfully, got {result.Outcome}: {result.Message}");
        Require(graceful.CallCount == 1 && graceful.LastProcessId == listener.ProcessId,
            "Normal stop did not signal the exact listener through the graceful shutdown path.");
        Require(exitVerifier.CallCount == 1,
            "Normal stop did not verify exact runner process exit after graceful shutdown.");
        Require(killer.CallCount == 0,
            "Normal stop hard-killed even though graceful shutdown completed and OFF was verified.");
    }

    private static void HardKillMustBeFallbackAndStillVerifyOff()
    {
        using var temp = new TempDirectory();
        CreateRunner(temp.Path, "runner-a", "RunnerA", "https://github.com/example/RepoA");
        var runner = RunnerDiscovery.Discover(temp.Path).Single();
        var listener = new ProcessSnapshot(701, 1, "Runner.Listener", RunnerPath.ListenerExecutable(runner.DirectoryPath));
        var provider = new SequenceProvider(Inventory(listener), Inventory(listener));
        var graceful = new FakeGracefulShutdown(RunnerGracefulShutdownOutcome.TimedOut);
        var exitVerifier = new FakeExitVerifier(true);
        var killer = new FakeKiller();
        var terminator = new WindowsRunnerProcessTerminator(
            provider,
            new FixedPathReader(listener.ProcessId, listener.ExecutablePath!),
            killer,
            graceful,
            exitVerifier);

        var result = terminator.Terminate(runner, listener);

        Require(graceful.CallCount == 1,
            "Hard-kill path did not attempt graceful shutdown first.");
        Require(killer.CallCount == 1,
            $"Graceful timeout should use exactly one verified hard-kill fallback, got {killer.CallCount}.");
        Require(exitVerifier.CallCount == 1,
            "Hard-kill fallback did not verify exact runner processes exited.");
        Require(result.Outcome == RunnerTerminationOutcome.Terminated,
            $"Verified fallback stop should succeed, got {result.Outcome}: {result.Message}");
    }

    private static void VerifiedStopMustReturnOffAndClearTransition()
    {
        using var temp = new TempDirectory();
        CreateRunner(temp.Path, "runner-a", "RunnerA", "https://github.com/example/RepoA");
        var runner = RunnerDiscovery.Discover(temp.Path).Single();
        var listener = new ProcessSnapshot(801, 1, "Runner.Listener", RunnerPath.ListenerExecutable(runner.DirectoryPath));
        var tracker = new RunnerTransitionTracker();
        var service = new RunnerControlService(
            temp.Path,
            new FixedProvider(Inventory(listener)),
            new NoopLauncher(),
            new FixedTerminator(new RunnerTerminationResult(RunnerTerminationOutcome.Terminated, "Verified runner shutdown completed.")),
            tracker,
            () => Now);

        var result = service.StopIdle(runner);

        Require(result.Outcome == RunnerControlOutcome.Stopped,
            $"Verified completed stop should return Stopped, got {result.Outcome}.");
        Require(result.State == RunnerState.OFF,
            $"Verified completed stop should report OFF, got {result.State}.");
        Require(tracker.Get(runner.DirectoryPath) is null,
            "Verified completed stop left a stale STOPPING transition behind.");
    }

    private static void ProvenExternalServiceMustNotBlockManagedStartAndMustClassifyExternal()
    {
        using var temp = new TempDirectory();
        CreateRunner(temp.Path, "runner-a", "RunnerA", "https://github.com/example/RepoA");
        var runner = RunnerDiscovery.Discover(temp.Path).Single();
        var unresolvedSystemListener = new ProcessSnapshot(
            9001,
            7000,
            "Runner.Listener",
            null,
            "Access is denied.",
            0);
        var inventory = Inventory(unresolvedSystemListener);
        var services = new FixedServiceEvidenceProvider(
            new RunnerServiceEvidence(
                "actions.runner.doonchy16-cloud-Lotto_engine.Lotto_MainPC_Runner",
                "\"C:\\actions-runner-lotto-mainpc\\bin\\RunnerService.exe\"",
                7000));

        var analysis = RunnerProcessInventoryAnalyzer.Analyze(temp.Path, new[] { runner }, inventory, services.Inspect());
        var external = analysis.ObservedProcesses.Single(process => process.ProcessId == 9001);
        Require(external.Ownership == RunnerProcessOwnershipKind.External,
            $"Service-correlated inaccessible listener should be EXTERNAL, got {external.Ownership}: {external.Reason}");

        var launcher = new RecordingLauncher();
        var control = new RunnerControlService(
            temp.Path,
            new FixedProvider(inventory),
            launcher,
            new NoopTerminator(),
            services,
            new RunnerTransitionTracker(),
            () => Now);
        var start = control.Start(runner);

        Require(start.Outcome == RunnerControlOutcome.Starting,
            $"Proven external service evidence incorrectly blocked unrelated managed start: {start.Outcome}: {start.Message}");
        Require(launcher.CallCount == 1,
            "Proven external service evidence prevented the managed runner's run.cmd launch.");
    }

    private static void GenuinelyUnattributedRunnerMustBlockManagedStart()
    {
        using var temp = new TempDirectory();
        CreateRunner(temp.Path, "runner-a", "RunnerA", "https://github.com/example/RepoA");
        var runner = RunnerDiscovery.Discover(temp.Path).Single();
        var unresolved = new ProcessSnapshot(
            9101,
            7100,
            "Runner.Listener",
            null,
            "Access is denied.",
            0);
        var launcher = new RecordingLauncher();
        var control = new RunnerControlService(
            temp.Path,
            new FixedProvider(Inventory(unresolved)),
            launcher,
            new NoopTerminator(),
            new FixedServiceEvidenceProvider(),
            new RunnerTransitionTracker(),
            () => Now);

        var start = control.Start(runner);

        Require(start.Outcome == RunnerControlOutcome.Error,
            $"Genuinely unattributed runner evidence must fail closed before start, got {start.Outcome}.");
        Require(launcher.CallCount == 0,
            "Managed runner was launched while unresolved runner ownership remained genuinely unattributed.");
        Require(start.Message.Contains("unattributed", StringComparison.OrdinalIgnoreCase)
                || start.Message.Contains("ownership", StringComparison.OrdinalIgnoreCase),
            $"Blocked start did not explain ownership uncertainty: {start.Message}");
    }

    private static ProcessInventory Inventory(params ProcessSnapshot[] processes) =>
        new(processes, true, null);

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

    private static readonly DateTimeOffset Now = new(2026, 9, 9, 18, 0, 0, TimeSpan.Zero);

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

    private sealed class SequenceProvider : IProcessSnapshotProvider
    {
        private readonly Queue<ProcessInventory> _sequence;
        private ProcessInventory _last;

        public SequenceProvider(params ProcessInventory[] sequence)
        {
            _sequence = new Queue<ProcessInventory>(sequence);
            _last = sequence[^1];
        }

        public ProcessInventory Capture()
        {
            if (_sequence.Count > 0) _last = _sequence.Dequeue();
            return _last;
        }
    }

    private sealed class NoopLauncher : IRunnerLauncher
    {
        public void Launch(RunnerDescriptor runner) =>
            throw new InvalidOperationException("Launcher must not be reached by this contract.");
    }

    private sealed class RecordingLauncher : IRunnerLauncher
    {
        public int CallCount { get; private set; }
        public void Launch(RunnerDescriptor runner) => CallCount++;
    }

    private sealed class NoopTerminator : IRunnerProcessTerminator
    {
        public RunnerTerminationResult Terminate(RunnerDescriptor runner, ProcessSnapshot listener) =>
            throw new InvalidOperationException("Terminator must not be reached by this contract.");
    }

    private sealed class FixedTerminator : IRunnerProcessTerminator
    {
        private readonly RunnerTerminationResult _result;
        public FixedTerminator(RunnerTerminationResult result) => _result = result;
        public RunnerTerminationResult Terminate(RunnerDescriptor runner, ProcessSnapshot listener) => _result;
    }

    private sealed class FixedServiceEvidenceProvider : IRunnerServiceEvidenceProvider
    {
        private readonly IReadOnlyList<RunnerServiceEvidence> _services;
        public FixedServiceEvidenceProvider(params RunnerServiceEvidence[] services) => _services = services;
        public IReadOnlyList<RunnerServiceEvidence> Inspect() => _services;
    }

    private sealed class FakeGracefulShutdown : IRunnerGracefulShutdown
    {
        private readonly RunnerGracefulShutdownOutcome _outcome;
        public FakeGracefulShutdown(RunnerGracefulShutdownOutcome outcome) => _outcome = outcome;
        public int CallCount { get; private set; }
        public int? LastProcessId { get; private set; }

        public RunnerGracefulShutdownResult TryShutdown(ProcessSnapshot listener, TimeSpan timeout)
        {
            CallCount++;
            LastProcessId = listener.ProcessId;
            return new RunnerGracefulShutdownResult(_outcome, _outcome.ToString());
        }
    }

    private sealed class FakeExitVerifier : IRunnerExitVerifier
    {
        private readonly bool _verified;
        public FakeExitVerifier(bool verified) => _verified = verified;
        public int CallCount { get; private set; }

        public bool WaitUntilOff(RunnerDescriptor runner, TimeSpan timeout, out string? error)
        {
            CallCount++;
            error = _verified ? null : "runner processes remained present";
            return _verified;
        }
    }

    private sealed class FixedPathReader : IProcessPathReader
    {
        private readonly int _pid;
        private readonly string _path;
        public FixedPathReader(int pid, string path) { _pid = pid; _path = path; }

        public bool TryGetPath(int processId, out string? path, out string? error)
        {
            path = processId == _pid ? _path : null;
            error = path is null ? "unexpected pid" : null;
            return path is not null;
        }
    }

    private sealed class FakeKiller : IProcessTreeKiller
    {
        public int CallCount { get; private set; }
        public bool TryKillTree(int processId, out string? error)
        {
            CallCount++;
            error = null;
            return true;
        }
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
