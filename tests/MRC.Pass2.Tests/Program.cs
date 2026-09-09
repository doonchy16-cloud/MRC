using System.Diagnostics;
using System.Text.Json;
using MRC.Core.Control;
using MRC.Core.Runners;
using MRC.Core.Runtime;

namespace MRC.Pass2.Tests;

internal static class Program
{
    private static int Main()
    {
        var tests = new (string Name, Action Body)[]
        {
            ("discovery is immediate-child only and signature-gated", DiscoveryIsImmediateChildOnly),
            ("malformed runner metadata becomes identity error", MalformedMetadataBecomesError),
            ("repository display derives from final URL segment", RepositoryNameDerivation),
            ("listener association requires exact executable path", ListenerAssociationRequiresExactPath),
            ("worker association requires same-runner ancestry", WorkerAssociationRequiresAncestry),
            ("orphan same-runner worker is contradictory", OrphanWorkerIsError),
            ("incomplete runner-process inspection fails closed", IncompleteInspectionIsError),
            ("canonical runtime states derive truthfully", CanonicalStates),
            ("expired transient transition becomes ERROR", TransitionTimeoutIsError),
            ("launcher targets run.cmd hidden in runner working directory", LaunchSpecIsSafe),
            ("start is allowed only from OFF and marks STARTING", StartOnlyFromOff),
            ("start is blocked when listener already exists", StartBlockedWhenRunning),
            ("normal stop blocks BUSY without termination", BusyStopIsProtected),
            ("normal IDLE stop marks STOPPING and terminates exact listener tree", IdleStopIsAllowed),
            ("final stop recheck catches a worker that appeared after first scan", FinalBusyRecheckBlocksKill),
            ("terminator re-verifies listener path before kill", TerminatorRevalidatesPath),
            ("control rejects runner directories outside its authorized root", OutsideRootControlIsBlocked),
            ("engine refresh exposes IDLE snapshot with parsed identity", EngineRefreshIntegration),
            ("known runner whose signature degrades remains visible as ERROR", SignatureDegradationBecomesError)
        };

        var failures = 0;
        Console.WriteLine($"MRC PASS 2 test harness — {tests.Length} tests");
        foreach (var test in tests)
        {
            try
            {
                test.Body();
                Console.WriteLine($"PASS  {test.Name}");
            }
            catch (Exception ex)
            {
                failures++;
                Console.WriteLine($"FAIL  {test.Name}");
                Console.WriteLine($"      {ex.Message}");
            }
        }

        Console.WriteLine();
        Console.WriteLine(failures == 0
            ? $"PASS  all {tests.Length} PASS 2 tests"
            : $"FAIL  {failures} of {tests.Length} PASS 2 tests");
        return failures == 0 ? 0 : 1;
    }

    private static void DiscoveryIsImmediateChildOnly()
    {
        using var temp = new TempDirectory();
        var valid = CreateRunner(temp.Path, "runner-a", "DBridge_MAIN", "https://github.com/doonchy16-cloud/Doonchy_Bridge");
        var malformedParent = System.IO.Path.Combine(temp.Path, "not-a-runner");
        Directory.CreateDirectory(malformedParent);
        CreateRunner(malformedParent, "nested-runner", "Nested", "https://github.com/example/Nested");
        var missingSignature = System.IO.Path.Combine(temp.Path, "missing-signature");
        Directory.CreateDirectory(missingSignature);
        File.WriteAllText(System.IO.Path.Combine(missingSignature, ".runner"), "{}");

        var discovered = RunnerDiscovery.Discover(temp.Path);
        Require(discovered.Count == 1, $"Expected exactly one immediate valid runner, found {discovered.Count}.");
        Require(RunnerPath.EqualsWindows(discovered[0].DirectoryPath, valid), "Discovery returned the wrong directory.");
        Require(discovered[0].AgentName == "DBridge_MAIN", "Runner identity was not parsed.");
    }

    private static void MalformedMetadataBecomesError()
    {
        using var temp = new TempDirectory();
        var runner = CreateRunner(temp.Path, "runner-a", "RunnerA", "https://github.com/example/RepoA");
        File.WriteAllText(System.IO.Path.Combine(runner, ".runner"), "{ this is not json }");

        var discovered = RunnerDiscovery.Discover(temp.Path);
        Require(discovered.Count == 1, "Signature-valid runner with malformed metadata must remain observable.");
        Require(discovered[0].HasIdentityError, "Malformed .runner metadata did not produce IdentityError.");
    }

    private static void RepositoryNameDerivation()
    {
        using var temp = new TempDirectory();
        CreateRunner(temp.Path, "runner-a", "RunnerA", "https://github.com/doonchy16-cloud/Doonchy_Bridge/");
        var runner = RunnerDiscovery.Discover(temp.Path).Single();
        Require(runner.RepositoryName == "Doonchy_Bridge", $"Unexpected repository name: {runner.RepositoryName}");
    }

    private static void ListenerAssociationRequiresExactPath()
    {
        using var temp = new TempDirectory();
        var runnerPath = CreateRunner(temp.Path, "runner-a", "RunnerA", "https://github.com/example/RepoA");
        var runner = RunnerDiscovery.Discover(temp.Path).Single();
        var expected = RunnerPath.ListenerExecutable(runnerPath);
        var inventory = Inventory(
            new ProcessSnapshot(10, 1, "Runner.Listener", expected),
            new ProcessSnapshot(11, 1, "Runner.Listener", System.IO.Path.Combine(temp.Path, "foreign", "bin", "Runner.Listener.exe")));

        var association = RunnerProcessAssociator.Associate(runner, inventory);
        Require(association.Error is null, association.Error ?? "Unexpected association error.");
        Require(association.Listener?.ProcessId == 10, "Exact listener was not selected.");
    }

    private static void WorkerAssociationRequiresAncestry()
    {
        using var temp = new TempDirectory();
        CreateRunner(temp.Path, "runner-a", "RunnerA", "https://github.com/example/RepoA");
        var runner = RunnerDiscovery.Discover(temp.Path).Single();
        var inventory = Inventory(
            new ProcessSnapshot(20, 1, "Runner.Listener", RunnerPath.ListenerExecutable(runner.DirectoryPath)),
            new ProcessSnapshot(21, 20, "Runner.Worker", RunnerPath.WorkerExecutable(runner.DirectoryPath)));

        var association = RunnerProcessAssociator.Associate(runner, inventory);
        Require(association.Error is null, association.Error ?? "Unexpected association error.");
        Require(association.Workers.Count == 1 && association.Workers[0].ProcessId == 21, "Worker ancestry was not associated.");
    }

    private static void OrphanWorkerIsError()
    {
        using var temp = new TempDirectory();
        CreateRunner(temp.Path, "runner-a", "RunnerA", "https://github.com/example/RepoA");
        var runner = RunnerDiscovery.Discover(temp.Path).Single();
        var inventory = Inventory(
            new ProcessSnapshot(30, 1, "Runner.Listener", RunnerPath.ListenerExecutable(runner.DirectoryPath)),
            new ProcessSnapshot(31, 999, "Runner.Worker", RunnerPath.WorkerExecutable(runner.DirectoryPath)));

        var association = RunnerProcessAssociator.Associate(runner, inventory);
        Require(association.Error is not null, "Same-runner worker without listener ancestry must be contradictory evidence.");
    }

    private static void IncompleteInspectionIsError()
    {
        using var temp = new TempDirectory();
        CreateRunner(temp.Path, "runner-a", "RunnerA", "https://github.com/example/RepoA");
        var runner = RunnerDiscovery.Discover(temp.Path).Single();
        var inventory = new ProcessInventory(new[]
        {
            new ProcessSnapshot(40, null, "Runner.Listener", null, "access denied")
        }, true, null);

        var association = RunnerProcessAssociator.Associate(runner, inventory);
        Require(association.Error is not null, "Inaccessible runner-process evidence must fail closed.");
        var state = RunnerStateEvaluator.Evaluate(runner, association, null, Now);
        Require(state == RunnerState.ERROR, $"Incomplete evidence must be ERROR, got {state}.");
    }

    private static void CanonicalStates()
    {
        var runner = Descriptor(@"C:\Temp\runner");
        var listener = new ProcessSnapshot(50, 1, "Runner.Listener", RunnerPath.ListenerExecutable(runner.DirectoryPath));
        var worker = new ProcessSnapshot(51, 50, "Runner.Worker", RunnerPath.WorkerExecutable(runner.DirectoryPath));
        var idle = new RunnerProcessAssociation(listener, Array.Empty<ProcessSnapshot>(), null);
        var busy = new RunnerProcessAssociation(listener, new[] { worker }, null);
        var off = new RunnerProcessAssociation(null, Array.Empty<ProcessSnapshot>(), null);
        var starting = new RunnerTransition(RunnerTransitionKind.Starting, Now, Now.AddSeconds(30), null);
        var stopping = new RunnerTransition(RunnerTransitionKind.Stopping, Now, Now.AddSeconds(30), null);

        Require(RunnerStateEvaluator.Evaluate(runner, off, null, Now) == RunnerState.OFF, "OFF failed.");
        Require(RunnerStateEvaluator.Evaluate(runner, off, starting, Now) == RunnerState.STARTING, "STARTING failed.");
        Require(RunnerStateEvaluator.Evaluate(runner, idle, null, Now) == RunnerState.IDLE, "IDLE failed.");
        Require(RunnerStateEvaluator.Evaluate(runner, busy, null, Now) == RunnerState.BUSY, "BUSY failed.");
        Require(RunnerStateEvaluator.Evaluate(runner, idle, stopping, Now) == RunnerState.STOPPING, "STOPPING failed.");
        var bad = runner with { IdentityError = "malformed metadata" };
        Require(RunnerStateEvaluator.Evaluate(bad, off, null, Now) == RunnerState.ERROR, "ERROR failed.");
    }

    private static void TransitionTimeoutIsError()
    {
        var runner = Descriptor(@"C:\Temp\runner");
        var off = new RunnerProcessAssociation(null, Array.Empty<ProcessSnapshot>(), null);
        var expired = new RunnerTransition(RunnerTransitionKind.Starting, Now.AddSeconds(-31), Now.AddSeconds(-1), null);
        Require(RunnerStateEvaluator.Evaluate(runner, off, expired, Now) == RunnerState.ERROR, "Expired STARTING did not become ERROR.");
    }

    private static void LaunchSpecIsSafe()
    {
        using var temp = new TempDirectory();
        CreateRunner(temp.Path, "runner-a", "RunnerA", "https://github.com/example/RepoA");
        var runner = RunnerDiscovery.Discover(temp.Path).Single();
        var spec = WindowsRunnerLauncher.BuildStartInfo(runner);
        Require(spec.FileName.Equals("cmd.exe", StringComparison.OrdinalIgnoreCase), "Launcher must use cmd.exe for run.cmd.");
        Require(RunnerPath.EqualsWindows(spec.WorkingDirectory, runner.DirectoryPath), "Launcher working directory is not runner directory.");
        Require(spec.CreateNoWindow && !spec.UseShellExecute, "Launcher must run hidden/background without shell UI.");
        var args = string.Join(" ", spec.ArgumentList);
        Require(args.Contains("/d", StringComparison.OrdinalIgnoreCase) && args.Contains("/s", StringComparison.OrdinalIgnoreCase) && args.Contains("/c", StringComparison.OrdinalIgnoreCase), "cmd safety flags are missing.");
        Require(args.Contains(RunnerPath.RunCommand(runner.DirectoryPath), StringComparison.OrdinalIgnoreCase), "Launcher does not target runner run.cmd.");
        Require(!args.Contains("Runner.Listener.exe", StringComparison.OrdinalIgnoreCase), "Launcher bypasses run.cmd.");
    }

    private static void StartOnlyFromOff()
    {
        using var temp = new TempDirectory();
        CreateRunner(temp.Path, "runner-a", "RunnerA", "https://github.com/example/RepoA");
        var runner = RunnerDiscovery.Discover(temp.Path).Single();
        var provider = new FakeProvider(Inventory());
        var launcher = new FakeLauncher();
        var terminator = new FakeTerminator();
        var tracker = new RunnerTransitionTracker();
        var service = new RunnerControlService(temp.Path, provider, launcher, terminator, tracker, () => Now);

        var result = service.Start(runner);
        Require(result.Outcome == RunnerControlOutcome.Starting && result.State == RunnerState.STARTING, $"Unexpected start result: {result}");
        Require(launcher.CallCount == 1 && launcher.LastRunner == runner, "OFF start did not invoke launcher exactly once.");
        Require(tracker.Get(runner.DirectoryPath)?.Kind == RunnerTransitionKind.Starting, "STARTING transition was not tracked.");
    }

    private static void StartBlockedWhenRunning()
    {
        using var temp = new TempDirectory();
        CreateRunner(temp.Path, "runner-a", "RunnerA", "https://github.com/example/RepoA");
        var runner = RunnerDiscovery.Discover(temp.Path).Single();
        var provider = new FakeProvider(Inventory(new ProcessSnapshot(60, 1, "Runner.Listener", RunnerPath.ListenerExecutable(runner.DirectoryPath))));
        var launcher = new FakeLauncher();
        var service = new RunnerControlService(temp.Path, provider, launcher, new FakeTerminator(), new RunnerTransitionTracker(), () => Now);

        var result = service.Start(runner);
        Require(result.Outcome == RunnerControlOutcome.NotOff, $"Running runner should not start again: {result.Outcome}");
        Require(launcher.CallCount == 0, "Running runner was launched again.");
    }

    private static void BusyStopIsProtected()
    {
        using var temp = new TempDirectory();
        CreateRunner(temp.Path, "runner-a", "RunnerA", "https://github.com/example/RepoA");
        var runner = RunnerDiscovery.Discover(temp.Path).Single();
        var listener = new ProcessSnapshot(70, 1, "Runner.Listener", RunnerPath.ListenerExecutable(runner.DirectoryPath));
        var worker = new ProcessSnapshot(71, 70, "Runner.Worker", RunnerPath.WorkerExecutable(runner.DirectoryPath));
        var terminator = new FakeTerminator();
        var service = new RunnerControlService(temp.Path, new FakeProvider(Inventory(listener, worker)), new FakeLauncher(), terminator, new RunnerTransitionTracker(), () => Now);

        var result = service.StopIdle(runner);
        Require(result.Outcome == RunnerControlOutcome.BusyProtected && result.State == RunnerState.BUSY, $"BUSY protection failed: {result}");
        Require(terminator.CallCount == 0, "BUSY runner reached terminator.");
    }

    private static void IdleStopIsAllowed()
    {
        using var temp = new TempDirectory();
        CreateRunner(temp.Path, "runner-a", "RunnerA", "https://github.com/example/RepoA");
        var runner = RunnerDiscovery.Discover(temp.Path).Single();
        var listener = new ProcessSnapshot(80, 1, "Runner.Listener", RunnerPath.ListenerExecutable(runner.DirectoryPath));
        var terminator = new FakeTerminator(new RunnerTerminationResult(RunnerTerminationOutcome.Terminated, "terminated"));
        var tracker = new RunnerTransitionTracker();
        var service = new RunnerControlService(temp.Path, new FakeProvider(Inventory(listener)), new FakeLauncher(), terminator, tracker, () => Now);

        var result = service.StopIdle(runner);
        Require(result.Outcome == RunnerControlOutcome.Stopping && result.State == RunnerState.STOPPING, $"IDLE stop failed: {result}");
        Require(terminator.CallCount == 1 && terminator.LastListener?.ProcessId == 80, "IDLE stop did not target exact listener once.");
        Require(tracker.Get(runner.DirectoryPath)?.Kind == RunnerTransitionKind.Stopping, "STOPPING transition was not tracked.");
    }

    private static void FinalBusyRecheckBlocksKill()
    {
        using var temp = new TempDirectory();
        CreateRunner(temp.Path, "runner-a", "RunnerA", "https://github.com/example/RepoA");
        var runner = RunnerDiscovery.Discover(temp.Path).Single();
        var listener = new ProcessSnapshot(90, 1, "Runner.Listener", RunnerPath.ListenerExecutable(runner.DirectoryPath));
        var worker = new ProcessSnapshot(91, 90, "Runner.Worker", RunnerPath.WorkerExecutable(runner.DirectoryPath));
        var provider = new FakeProvider(Inventory(listener));
        provider.Enqueue(Inventory(listener, worker));
        var pathReader = new FakePathReader(listener.ProcessId, listener.ExecutablePath!);
        var killer = new FakeKiller();
        var terminator = new WindowsRunnerProcessTerminator(provider, pathReader, killer);
        var service = new RunnerControlService(temp.Path, provider, new FakeLauncher(), terminator, new RunnerTransitionTracker(), () => Now);

        var result = service.StopIdle(runner);
        Require(result.Outcome == RunnerControlOutcome.BusyProtected, $"Second-scan BUSY must be protected: {result.Outcome}");
        Require(killer.CallCount == 0, "A worker appeared during stop race but the listener tree was killed.");
    }

    private static void TerminatorRevalidatesPath()
    {
        using var temp = new TempDirectory();
        CreateRunner(temp.Path, "runner-a", "RunnerA", "https://github.com/example/RepoA");
        var runner = RunnerDiscovery.Discover(temp.Path).Single();
        var listener = new ProcessSnapshot(100, 1, "Runner.Listener", RunnerPath.ListenerExecutable(runner.DirectoryPath));
        var provider = new FakeProvider(Inventory(listener));
        var reader = new FakePathReader(listener.ProcessId, System.IO.Path.Combine(temp.Path, "foreign", "Runner.Listener.exe"));
        var killer = new FakeKiller();
        var terminator = new WindowsRunnerProcessTerminator(provider, reader, killer);

        var result = terminator.Terminate(runner, listener);
        Require(result.Outcome == RunnerTerminationOutcome.OwnershipMismatch, $"Expected ownership mismatch, got {result.Outcome}.");
        Require(killer.CallCount == 0, "Path mismatch still reached process-tree kill.");
    }

    private static void OutsideRootControlIsBlocked()
    {
        using var authorized = new TempDirectory();
        using var outside = new TempDirectory();
        CreateRunner(outside.Path, "runner-a", "RunnerA", "https://github.com/example/RepoA");
        var runner = RunnerDiscovery.Discover(outside.Path).Single();
        var launcher = new FakeLauncher();
        var terminator = new FakeTerminator();
        var service = new RunnerControlService(authorized.Path, new FakeProvider(Inventory()), launcher, terminator, new RunnerTransitionTracker(), () => Now);

        var start = service.Start(runner);
        var stop = service.StopIdle(runner);
        Require(start.Outcome == RunnerControlOutcome.Error && stop.Outcome == RunnerControlOutcome.Error, "Outside-root runner was not rejected.");
        Require(launcher.CallCount == 0 && terminator.CallCount == 0, "Outside-root runner reached a control primitive.");
    }

    private static void EngineRefreshIntegration()
    {
        using var temp = new TempDirectory();
        CreateRunner(temp.Path, "runner-a", "DBridge_MAIN", "https://github.com/doonchy16-cloud/Doonchy_Bridge");
        var discovered = RunnerDiscovery.Discover(temp.Path).Single();
        var listener = new ProcessSnapshot(110, 1, "Runner.Listener", RunnerPath.ListenerExecutable(discovered.DirectoryPath));
        var engine = new RunnerEngine(temp.Path, new FakeProvider(Inventory(listener)), new FakeLauncher(), new FakeTerminator(), () => Now);

        var snapshots = engine.Refresh();
        Require(snapshots.Count == 1, "Engine did not expose discovered runner.");
        Require(snapshots[0].State == RunnerState.IDLE, $"Expected IDLE, got {snapshots[0].State}.");
        Require(snapshots[0].Runner.AgentName == "DBridge_MAIN" && snapshots[0].Runner.RepositoryName == "Doonchy_Bridge", "Engine snapshot lost identity.");
    }

    private static void SignatureDegradationBecomesError()
    {
        using var temp = new TempDirectory();
        var runnerPath = CreateRunner(temp.Path, "runner-a", "RunnerA", "https://github.com/example/RepoA");
        var engine = new RunnerEngine(temp.Path, new FakeProvider(Inventory()), new FakeLauncher(), new FakeTerminator(), () => Now);
        Require(engine.Refresh().Single().State == RunnerState.OFF, "Initial valid runner should be OFF.");

        File.Delete(RunnerPath.ListenerExecutable(runnerPath));
        var degraded = engine.Refresh();
        Require(degraded.Count == 1, "Previously known degraded runner disappeared instead of surfacing ERROR.");
        Require(degraded[0].State == RunnerState.ERROR, $"Degraded signature should be ERROR, got {degraded[0].State}.");
    }

    private static RunnerDescriptor Descriptor(string path) =>
        new(path, "RunnerA", "https://github.com/example/RepoA", "RepoA", 1, "_work", null);

    private static ProcessInventory Inventory(params ProcessSnapshot[] processes) =>
        new(processes, true, null);

    private static string CreateRunner(string root, string folder, string agentName, string gitHubUrl)
    {
        var runner = System.IO.Path.Combine(root, folder);
        Directory.CreateDirectory(System.IO.Path.Combine(runner, "bin"));
        var metadata = JsonSerializer.Serialize(new
        {
            agentName,
            gitHubUrl,
            agentId = 123,
            workFolder = "_work"
        });
        File.WriteAllText(System.IO.Path.Combine(runner, ".runner"), metadata);
        File.WriteAllText(System.IO.Path.Combine(runner, "run.cmd"), "@echo off\r\n");
        File.WriteAllText(System.IO.Path.Combine(runner, "run-helper.cmd.template"), "@echo off\r\n");
        File.WriteAllBytes(System.IO.Path.Combine(runner, "bin", "Runner.Listener.exe"), Array.Empty<byte>());
        return runner;
    }

    private static readonly DateTimeOffset Now = new(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "mrc-pass2-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try { Directory.Delete(Path, true); } catch { }
        }
    }

    private sealed class FakeProvider : IProcessSnapshotProvider
    {
        private readonly Queue<ProcessInventory> _queued = new();
        private ProcessInventory _current;

        public FakeProvider(ProcessInventory current) => _current = current;

        public int CaptureCount { get; private set; }

        public void Enqueue(ProcessInventory inventory) => _queued.Enqueue(inventory);

        public ProcessInventory Capture()
        {
            CaptureCount++;
            if (_queued.Count > 0)
            {
                _current = _queued.Dequeue();
            }
            return _current;
        }
    }

    private sealed class FakeLauncher : IRunnerLauncher
    {
        public int CallCount { get; private set; }
        public RunnerDescriptor? LastRunner { get; private set; }

        public void Launch(RunnerDescriptor runner)
        {
            CallCount++;
            LastRunner = runner;
        }
    }

    private sealed class FakeTerminator : IRunnerProcessTerminator
    {
        private readonly RunnerTerminationResult _result;

        public FakeTerminator(RunnerTerminationResult? result = null) =>
            _result = result ?? new RunnerTerminationResult(RunnerTerminationOutcome.Terminated, "terminated");

        public int CallCount { get; private set; }
        public ProcessSnapshot? LastListener { get; private set; }

        public RunnerTerminationResult Terminate(RunnerDescriptor runner, ProcessSnapshot listener)
        {
            CallCount++;
            LastListener = listener;
            return _result;
        }
    }

    private sealed class FakePathReader : IProcessPathReader
    {
        private readonly int _pid;
        private readonly string _path;

        public FakePathReader(int pid, string path)
        {
            _pid = pid;
            _path = path;
        }

        public bool TryGetPath(int processId, out string? path, out string? error)
        {
            if (processId == _pid)
            {
                path = _path;
                error = null;
                return true;
            }
            path = null;
            error = "unknown pid";
            return false;
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
}
