using System.Text.Json;
using MRC.Core.Control;
using MRC.Core.Runners;
using MRC.Core.Runtime;

namespace MRC.Pass4.Tests;

internal static class OperationsAcceptance
{
    private static readonly DateTimeOffset Now = new(2026, 9, 9, 1, 45, 0, TimeSpan.FromHours(-7));

    public static int Run()
    {
        var tests = new (string Name, Action Body)[]
        {
            ("individual controls preserve verified PASS 2 start/idle-stop safety", IndividualControlsPreservePass2Safety),
            ("BUSY force-stop requires explicit confirmation before any termination", ForceStopRequiresExplicitConfirmation),
            ("confirmed BUSY force-stop enters STOPPING through force terminator", ConfirmedForceStopTargetsBusy),
            ("force terminator re-verifies exact listener path and runner association before kill", ForceTerminatorRevalidatesBeforeKill),
            ("TURN ALL ON targets only OFF runners and staggers launch attempts by 150 ms", TurnAllOnTargetsOnlyOffAndStaggers),
            ("TURN ALL OFF targets only IDLE runners and reports BUSY skips exactly", TurnAllOffTargetsOnlyIdleAndSkipsBusy),
            ("controller close remains lifecycle-neutral", ControllerCloseHasNoRunnerLifecycleSideEffect)
        };

        var failures = 0;
        Console.WriteLine($"MRC PASS 4 operations harness — {tests.Length} tests");
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
            ? $"PASS  all {tests.Length} PASS 4 operations tests"
            : $"FAIL  {failures} of {tests.Length} PASS 4 operations tests");
        return failures == 0 ? 0 : 1;
    }

    private static void IndividualControlsPreservePass2Safety()
    {
        using var temp = new TempDirectory();
        CreateRunner(temp.Path, "off-runner", "OffRunner", "https://github.com/example/RepoA");
        CreateRunner(temp.Path, "idle-runner", "IdleRunner", "https://github.com/example/RepoA");
        var runners = RunnerDiscovery.Discover(temp.Path).OrderBy(r => r.AgentName).ToArray();
        var idle = runners.Single(r => r.AgentName == "IdleRunner");
        var off = runners.Single(r => r.AgentName == "OffRunner");
        var listener = new ProcessSnapshot(100, 1, "Runner.Listener", RunnerPath.ListenerExecutable(idle.DirectoryPath));
        var provider = new FakeProvider(Inventory(listener));
        var launcher = new FakeLauncher();
        var normal = new FakeTerminator();
        var service = new RunnerControlService(temp.Path, provider, launcher, normal, new RunnerTransitionTracker(), () => Now);

        var start = service.Start(off);
        var stop = service.StopIdle(idle);

        Require(start.Outcome == RunnerControlOutcome.Starting && start.State == RunnerState.STARTING, "OFF runner did not enter STARTING.");
        Require(stop.Outcome == RunnerControlOutcome.Stopping && stop.State == RunnerState.STOPPING, "IDLE runner did not enter STOPPING.");
        Require(launcher.CallCount == 1, "Start did not use the existing safe launcher exactly once.");
        Require(normal.CallCount == 1, "Idle stop did not use the existing exact-listener terminator exactly once.");
    }

    private static void ForceStopRequiresExplicitConfirmation()
    {
        using var temp = new TempDirectory();
        CreateRunner(temp.Path, "busy-runner", "BusyRunner", "https://github.com/example/RepoA");
        var runner = RunnerDiscovery.Discover(temp.Path).Single();
        var listener = new ProcessSnapshot(200, 1, "Runner.Listener", RunnerPath.ListenerExecutable(runner.DirectoryPath));
        var worker = new ProcessSnapshot(201, 200, "Runner.Worker", RunnerPath.WorkerExecutable(runner.DirectoryPath));
        var force = new FakeForceTerminator();
        var service = new RunnerControlService(
            temp.Path,
            new FakeProvider(Inventory(listener, worker)),
            new FakeLauncher(),
            new FakeTerminator(),
            force,
            new RunnerTransitionTracker(),
            () => Now);

        var result = service.ForceStopBusy(runner, confirmed: false);

        Require(result.Outcome == RunnerControlOutcome.ConfirmationRequired, $"Expected confirmation requirement, got {result.Outcome}.");
        Require(result.State == RunnerState.BUSY, "Unconfirmed force-stop must leave BUSY truth visible.");
        Require(force.CallCount == 0, "Unconfirmed force-stop reached a termination primitive.");
    }

    private static void ConfirmedForceStopTargetsBusy()
    {
        using var temp = new TempDirectory();
        CreateRunner(temp.Path, "busy-runner", "BusyRunner", "https://github.com/example/RepoA");
        var runner = RunnerDiscovery.Discover(temp.Path).Single();
        var listener = new ProcessSnapshot(210, 1, "Runner.Listener", RunnerPath.ListenerExecutable(runner.DirectoryPath));
        var worker = new ProcessSnapshot(211, 210, "Runner.Worker", RunnerPath.WorkerExecutable(runner.DirectoryPath));
        var force = new FakeForceTerminator(new RunnerTerminationResult(RunnerTerminationOutcome.Terminated, "force terminated"));
        var tracker = new RunnerTransitionTracker();
        var service = new RunnerControlService(
            temp.Path,
            new FakeProvider(Inventory(listener, worker)),
            new FakeLauncher(),
            new FakeTerminator(),
            force,
            tracker,
            () => Now);

        var result = service.ForceStopBusy(runner, confirmed: true);

        Require(result.Outcome == RunnerControlOutcome.ForceStopping && result.State == RunnerState.STOPPING, $"Confirmed BUSY force-stop did not enter STOPPING: {result}.");
        Require(force.CallCount == 1 && force.LastListener?.ProcessId == listener.ProcessId, "Confirmed force-stop did not target the exact observed listener once.");
        Require(tracker.Get(runner.DirectoryPath)?.Kind == RunnerTransitionKind.Stopping, "Confirmed force-stop did not register STOPPING transition.");
    }

    private static void ForceTerminatorRevalidatesBeforeKill()
    {
        using var temp = new TempDirectory();
        CreateRunner(temp.Path, "busy-runner", "BusyRunner", "https://github.com/example/RepoA");
        var runner = RunnerDiscovery.Discover(temp.Path).Single();
        var listener = new ProcessSnapshot(220, 1, "Runner.Listener", RunnerPath.ListenerExecutable(runner.DirectoryPath));
        var worker = new ProcessSnapshot(221, 220, "Runner.Worker", RunnerPath.WorkerExecutable(runner.DirectoryPath));
        var provider = new FakeProvider(Inventory(listener, worker));
        var wrongPath = System.IO.Path.Combine(temp.Path, "foreign", "Runner.Listener.exe");
        var killer = new FakeKiller();
        var terminator = new WindowsRunnerForceProcessTerminator(provider, new FakePathReader(listener.ProcessId, wrongPath), killer);

        var result = terminator.TerminateForce(runner, listener);

        Require(result.Outcome == RunnerTerminationOutcome.OwnershipMismatch, $"Expected ownership mismatch, got {result.Outcome}.");
        Require(killer.CallCount == 0, "Force path mismatch still reached process-tree kill.");
    }

    private static void TurnAllOnTargetsOnlyOffAndStaggers()
    {
        var runners = new[]
        {
            Descriptor(@"C:\Runners\off-a", "OffA"),
            Descriptor(@"C:\Runners\idle-a", "IdleA"),
            Descriptor(@"C:\Runners\off-b", "OffB"),
            Descriptor(@"C:\Runners\busy-a", "BusyA"),
            Descriptor(@"C:\Runners\off-c", "OffC")
        };
        var snapshots = new[]
        {
            Snapshot(runners[0], RunnerState.OFF),
            Snapshot(runners[1], RunnerState.IDLE),
            Snapshot(runners[2], RunnerState.OFF),
            Snapshot(runners[3], RunnerState.BUSY),
            Snapshot(runners[4], RunnerState.OFF)
        };
        var starts = new List<string>();
        var delays = new List<TimeSpan>();
        var operations = new RunnerOperationsService(
            () => snapshots,
            runner => { starts.Add(runner.AgentName ?? runner.DirectoryPath); return new RunnerControlResult(RunnerControlOutcome.Starting, RunnerState.STARTING, "starting"); },
            runner => new RunnerControlResult(RunnerControlOutcome.Stopping, RunnerState.STOPPING, "stopping"),
            (runner, confirmed) => new RunnerControlResult(RunnerControlOutcome.ForceStopping, RunnerState.STOPPING, "force stopping"),
            (delay, _) => { delays.Add(delay); return Task.CompletedTask; });

        var result = operations.TurnAllOnAsync().GetAwaiter().GetResult();

        Require(starts.SequenceEqual(new[] { "OffA", "OffB", "OffC" }), "TURN ALL ON called start for a non-OFF runner or changed snapshot order.");
        Require(delays.Count == 2 && delays.All(d => d == TimeSpan.FromMilliseconds(150)), "TURN ALL ON must delay exactly 150 ms between launch attempts, with no trailing delay.");
        Require(result.Attempted == 3 && result.Succeeded == 3 && result.Skipped == 2 && result.Errors == 0, $"Unexpected TURN ALL ON counts: {result}.");
        Require(result.BusySkipped == 0, "TURN ALL ON BUSY rows are ordinary non-target skips, not BUSY-stop skips.");
    }

    private static void TurnAllOffTargetsOnlyIdleAndSkipsBusy()
    {
        var runners = new[]
        {
            Descriptor(@"C:\Runners\idle-a", "IdleA"),
            Descriptor(@"C:\Runners\busy-a", "BusyA"),
            Descriptor(@"C:\Runners\off-a", "OffA"),
            Descriptor(@"C:\Runners\idle-b", "IdleB"),
            Descriptor(@"C:\Runners\busy-b", "BusyB")
        };
        var snapshots = new[]
        {
            Snapshot(runners[0], RunnerState.IDLE),
            Snapshot(runners[1], RunnerState.BUSY),
            Snapshot(runners[2], RunnerState.OFF),
            Snapshot(runners[3], RunnerState.IDLE),
            Snapshot(runners[4], RunnerState.BUSY)
        };
        var stops = new List<string>();
        var forceCalls = 0;
        var operations = new RunnerOperationsService(
            () => snapshots,
            runner => new RunnerControlResult(RunnerControlOutcome.Starting, RunnerState.STARTING, "starting"),
            runner => { stops.Add(runner.AgentName ?? runner.DirectoryPath); return new RunnerControlResult(RunnerControlOutcome.Stopping, RunnerState.STOPPING, "stopping"); },
            (runner, confirmed) => { forceCalls++; return new RunnerControlResult(RunnerControlOutcome.ForceStopping, RunnerState.STOPPING, "force stopping"); },
            (_, _) => Task.CompletedTask);

        var result = operations.TurnAllOff();

        Require(stops.SequenceEqual(new[] { "IdleA", "IdleB" }), "TURN ALL OFF targeted anything other than IDLE runners.");
        Require(forceCalls == 0, "TURN ALL OFF must never use force-stop.");
        Require(result.Attempted == 2 && result.Succeeded == 2 && result.Skipped == 3 && result.BusySkipped == 2 && result.Errors == 0, $"Unexpected TURN ALL OFF counts: {result}.");
    }

    private static void ControllerCloseHasNoRunnerLifecycleSideEffect()
    {
        var code = File.ReadAllText(System.IO.Path.Combine(RepoRoot(), "src", "MRC.Gui", "MainWindow.xaml.cs"));
        var start = code.IndexOf("private void OnClosed", StringComparison.Ordinal);
        Require(start >= 0, "MainWindow OnClosed handler is missing.");
        var tail = code[start..];
        var end = tail.IndexOf("    }", StringComparison.Ordinal);
        Require(end >= 0, "Could not isolate OnClosed handler.");
        var body = tail[..(end + 5)];
        Require(!body.Contains("Start(", StringComparison.Ordinal) &&
                !body.Contains("StopIdle(", StringComparison.Ordinal) &&
                !body.Contains("ForceStop", StringComparison.Ordinal) &&
                !body.Contains("TurnAll", StringComparison.Ordinal),
                "Closing MRC contains runner lifecycle control.");
    }

    private static RunnerSnapshot Snapshot(RunnerDescriptor runner, RunnerState state) => new(runner, state, null);

    private static RunnerDescriptor Descriptor(string path, string agentName) =>
        new(path, agentName, $"https://github.com/example/{agentName}", agentName, 1, "_work", null);

    private static ProcessInventory Inventory(params ProcessSnapshot[] processes) => new(processes, true, null);

    private static string CreateRunner(string root, string folder, string agentName, string gitHubUrl)
    {
        var runner = System.IO.Path.Combine(root, folder);
        Directory.CreateDirectory(System.IO.Path.Combine(runner, "bin"));
        var metadata = JsonSerializer.Serialize(new { agentName, gitHubUrl, agentId = 123, workFolder = "_work" });
        File.WriteAllText(System.IO.Path.Combine(runner, ".runner"), metadata);
        File.WriteAllText(System.IO.Path.Combine(runner, "run.cmd"), "@echo off\r\n");
        File.WriteAllText(System.IO.Path.Combine(runner, "run-helper.cmd.template"), "@echo off\r\n");
        File.WriteAllBytes(System.IO.Path.Combine(runner, "bin", "Runner.Listener.exe"), Array.Empty<byte>());
        return runner;
    }

    private static string RepoRoot()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            if (File.Exists(System.IO.Path.Combine(current.FullName, "Auth", "0000_MasterAuth.md"))) return current.FullName;
            current = current.Parent;
        }
        throw new InvalidOperationException("Could not locate MRC repository root.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "mrc-pass4-" + Guid.NewGuid().ToString("N"));
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
            if (_queued.Count > 0) _current = _queued.Dequeue();
            return _current;
        }
    }

    private sealed class FakeLauncher : IRunnerLauncher
    {
        public int CallCount { get; private set; }
        public void Launch(RunnerDescriptor runner) => CallCount++;
    }

    private sealed class FakeTerminator : IRunnerProcessTerminator
    {
        public int CallCount { get; private set; }
        public ProcessSnapshot? LastListener { get; private set; }
        public RunnerTerminationResult Terminate(RunnerDescriptor runner, ProcessSnapshot listener)
        {
            CallCount++;
            LastListener = listener;
            return new RunnerTerminationResult(RunnerTerminationOutcome.Terminated, "terminated");
        }
    }

    private sealed class FakeForceTerminator : IRunnerForceProcessTerminator
    {
        private readonly RunnerTerminationResult _result;
        public FakeForceTerminator(RunnerTerminationResult? result = null) =>
            _result = result ?? new RunnerTerminationResult(RunnerTerminationOutcome.Terminated, "force terminated");
        public int CallCount { get; private set; }
        public ProcessSnapshot? LastListener { get; private set; }
        public RunnerTerminationResult TerminateForce(RunnerDescriptor runner, ProcessSnapshot listener)
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
        public FakePathReader(int pid, string path) { _pid = pid; _path = path; }
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
