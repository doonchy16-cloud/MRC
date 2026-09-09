using System.Runtime.CompilerServices;
using System.Text.Json;
using MRC.Core.Control;
using MRC.Core.Runners;
using MRC.Core.Runtime;

namespace MRC.Pass2.Tests;

internal static class EliteSafetyChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        var failures = new List<string>();
        Check("runtime state enum is exactly canonical six", ExactStateEnum, failures);
        Check("existing transitions report truthful state", TransitionResponsesAreTruthful, failures);
        Check("stop race performs a second fresh BUSY check", StopRaceReallyUsesSecondCapture, failures);

        foreach (var failure in failures)
        {
            Console.WriteLine($"FAIL  ELITE {failure}");
        }

        if (failures.Count > 0)
        {
            throw new InvalidOperationException($"PASS 2 elite safety prechecks failed: {failures.Count} finding(s).");
        }

        Console.WriteLine("PASS  all 3 PASS 2 elite safety prechecks");
    }

    private static void ExactStateEnum()
    {
        var actual = Enum.GetNames<RunnerState>();
        var expected = new[] { "OFF", "STARTING", "IDLE", "BUSY", "STOPPING", "ERROR" };
        Require(actual.SequenceEqual(expected, StringComparer.Ordinal),
            $"State enum mismatch. Actual: {string.Join(", ", actual)}");
    }

    private static void TransitionResponsesAreTruthful()
    {
        using var temp = new TempDirectory();
        CreateRunner(temp.Path, "runner-a");
        var runner = RunnerDiscovery.Discover(temp.Path).Single();

        var startingTracker = new RunnerTransitionTracker();
        startingTracker.MarkStarting(runner.DirectoryPath, Now);
        var startingService = new RunnerControlService(
            temp.Path,
            new SequenceProvider(Inventory()),
            new FakeLauncher(),
            new FakeTerminator(),
            startingTracker,
            () => Now);
        var stopWhileStarting = startingService.StopIdle(runner);
        Require(stopWhileStarting.State == RunnerState.STARTING,
            $"Stop while STARTING reported {stopWhileStarting.State} instead of STARTING.");

        var stoppingTracker = new RunnerTransitionTracker();
        stoppingTracker.MarkStopping(runner.DirectoryPath, Now);
        var stoppingService = new RunnerControlService(
            temp.Path,
            new SequenceProvider(Inventory()),
            new FakeLauncher(),
            new FakeTerminator(),
            stoppingTracker,
            () => Now);
        var startWhileStopping = stoppingService.Start(runner);
        Require(startWhileStopping.State == RunnerState.STOPPING,
            $"Start while STOPPING reported {startWhileStopping.State} instead of STOPPING.");
    }

    private static void StopRaceReallyUsesSecondCapture()
    {
        using var temp = new TempDirectory();
        CreateRunner(temp.Path, "runner-a");
        var runner = RunnerDiscovery.Discover(temp.Path).Single();
        var listener = new ProcessSnapshot(501, 1, "Runner.Listener", RunnerPath.ListenerExecutable(runner.DirectoryPath));
        var worker = new ProcessSnapshot(502, 501, "Runner.Worker", RunnerPath.WorkerExecutable(runner.DirectoryPath));

        var provider = new SequenceProvider(Inventory(listener), Inventory(listener, worker));
        var killer = new FakeKiller();
        var terminator = new WindowsRunnerProcessTerminator(
            provider,
            new FixedPathReader(listener.ProcessId, listener.ExecutablePath!),
            killer);
        var service = new RunnerControlService(
            temp.Path,
            provider,
            new FakeLauncher(),
            terminator,
            new RunnerTransitionTracker(),
            () => Now);

        var result = service.StopIdle(runner);
        Require(provider.CaptureCount == 2,
            $"Expected exactly 2 process captures (initial IDLE + final BUSY), got {provider.CaptureCount}.");
        Require(result.Outcome == RunnerControlOutcome.BusyProtected && result.State == RunnerState.BUSY,
            $"Second capture BUSY evidence was not protected: {result.Outcome}/{result.State}.");
        Require(killer.CallCount == 0, "Process-tree killer was invoked after BUSY appeared on final recheck.");
    }

    private static void Check(string name, Action action, ICollection<string> failures)
    {
        try
        {
            action();
            Console.WriteLine($"PASS  ELITE {name}");
        }
        catch (Exception ex)
        {
            failures.Add($"{name}: {ex.Message}");
        }
    }

    private static ProcessInventory Inventory(params ProcessSnapshot[] processes) =>
        new(processes, true, null);

    private static void CreateRunner(string root, string folder)
    {
        var runner = Path.Combine(root, folder);
        Directory.CreateDirectory(Path.Combine(runner, "bin"));
        File.WriteAllText(Path.Combine(runner, ".runner"), JsonSerializer.Serialize(new
        {
            agentName = "RunnerA",
            gitHubUrl = "https://github.com/example/RepoA",
            agentId = 1,
            workFolder = "_work"
        }));
        File.WriteAllText(Path.Combine(runner, "run.cmd"), "@echo off\r\n");
        File.WriteAllText(Path.Combine(runner, "run-helper.cmd.template"), "@echo off\r\n");
        File.WriteAllBytes(Path.Combine(runner, "bin", "Runner.Listener.exe"), Array.Empty<byte>());
    }

    private static readonly DateTimeOffset Now = new(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class SequenceProvider : IProcessSnapshotProvider
    {
        private readonly Queue<ProcessInventory> _sequence;
        private ProcessInventory _last;

        public SequenceProvider(params ProcessInventory[] sequence)
        {
            if (sequence.Length == 0) throw new ArgumentException("At least one inventory is required.", nameof(sequence));
            _sequence = new Queue<ProcessInventory>(sequence);
            _last = sequence[^1];
        }

        public int CaptureCount { get; private set; }

        public ProcessInventory Capture()
        {
            CaptureCount++;
            if (_sequence.Count > 0)
            {
                _last = _sequence.Dequeue();
            }
            return _last;
        }
    }

    private sealed class FakeLauncher : IRunnerLauncher
    {
        public void Launch(RunnerDescriptor runner) => throw new InvalidOperationException("Launcher must not be reached by these checks.");
    }

    private sealed class FakeTerminator : IRunnerProcessTerminator
    {
        public RunnerTerminationResult Terminate(RunnerDescriptor runner, ProcessSnapshot listener) =>
            throw new InvalidOperationException("Terminator must not be reached by transition-truth checks.");
    }

    private sealed class FixedPathReader : IProcessPathReader
    {
        private readonly int _processId;
        private readonly string _path;

        public FixedPathReader(int processId, string path)
        {
            _processId = processId;
            _path = path;
        }

        public bool TryGetPath(int processId, out string? path, out string? error)
        {
            if (processId == _processId)
            {
                path = _path;
                error = null;
                return true;
            }

            path = null;
            error = "unexpected pid";
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

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "mrc-pass2-elite-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try { Directory.Delete(Path, true); } catch { }
        }
    }
}
