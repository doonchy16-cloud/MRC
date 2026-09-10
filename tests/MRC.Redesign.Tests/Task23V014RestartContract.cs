using System.Runtime.CompilerServices;
using System.Text.Json;
using MRC.Core.Control;
using MRC.Core.Runners;
using MRC.Core.Runtime;

internal static class Task23V014RestartContract
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-10T04:00:00Z");

    [ModuleInitializer]
    internal static void Run()
    {
        VerifyIdleRestartStopsThenStarts();
        VerifyFailedStopNeverStarts();
        VerifyBusyRestartRemainsProtected();
        Console.WriteLine("PASS  Task23 v0.0.14 restart is verified STOP -> OFF -> START");
    }

    private static void VerifyIdleRestartStopsThenStarts()
    {
        using var temp = new TempDirectory();
        var runner = CreateRunner(temp.Path, "runner", "RestartRunner");
        var listener = new ProcessSnapshot(4100, 1, "Runner.Listener", RunnerPath.ListenerExecutable(runner.DirectoryPath));
        var provider = new SequenceProvider(
            Inventory(listener),
            Inventory());
        var launcher = new CountingLauncher();
        var terminator = new FixedTerminator(new RunnerTerminationResult(RunnerTerminationOutcome.Terminated, "verified off"));
        var service = Service(temp.Path, provider, launcher, terminator);

        var result = service.Restart(runner);

        Require(terminator.CallCount == 1, $"Restart terminated {terminator.CallCount} time(s); expected once.");
        Require(launcher.CallCount == 1, $"Restart launched {launcher.CallCount} time(s); expected once after verified OFF.");
        Require(provider.CaptureCount >= 2, "Restart did not re-observe state before START.");
        Require(result.Outcome == RunnerControlOutcome.Starting && result.State == RunnerState.STARTING,
            $"Successful restart ended as {result.Outcome}/{result.State}; expected Starting/STARTING.");
        Require(result.Message.Contains("restart", StringComparison.OrdinalIgnoreCase),
            "Successful restart result does not identify the restart lifecycle.");
    }

    private static void VerifyFailedStopNeverStarts()
    {
        using var temp = new TempDirectory();
        var runner = CreateRunner(temp.Path, "runner", "RestartRunner");
        var listener = new ProcessSnapshot(4200, 1, "Runner.Listener", RunnerPath.ListenerExecutable(runner.DirectoryPath));
        var provider = new SequenceProvider(Inventory(listener));
        var launcher = new CountingLauncher();
        var terminator = new FixedTerminator(new RunnerTerminationResult(RunnerTerminationOutcome.InspectionError, "shutdown could not be verified"));
        var service = Service(temp.Path, provider, launcher, terminator);

        var result = service.Restart(runner);

        Require(launcher.CallCount == 0, "Restart launched after STOP failed to prove OFF.");
        Require(result.Outcome == RunnerControlOutcome.Error,
            $"Failed restart returned {result.Outcome}; expected Error.");
    }

    private static void VerifyBusyRestartRemainsProtected()
    {
        using var temp = new TempDirectory();
        var runner = CreateRunner(temp.Path, "runner", "RestartRunner");
        var listener = new ProcessSnapshot(4300, 1, "Runner.Listener", RunnerPath.ListenerExecutable(runner.DirectoryPath));
        var worker = new ProcessSnapshot(4301, 4300, "Runner.Worker", RunnerPath.WorkerExecutable(runner.DirectoryPath));
        var provider = new SequenceProvider(Inventory(listener, worker));
        var launcher = new CountingLauncher();
        var terminator = new FixedTerminator(new RunnerTerminationResult(RunnerTerminationOutcome.Terminated, "should not run"));
        var service = Service(temp.Path, provider, launcher, terminator);

        var result = service.Restart(runner);

        Require(result.Outcome == RunnerControlOutcome.BusyProtected && result.State == RunnerState.BUSY,
            $"BUSY restart returned {result.Outcome}/{result.State}; BUSY must remain protected.");
        Require(terminator.CallCount == 0, "BUSY restart reached normal termination.");
        Require(launcher.CallCount == 0, "BUSY restart reached launch.");
    }

    private static RunnerControlService Service(
        string root,
        IProcessSnapshotProvider provider,
        IRunnerLauncher launcher,
        IRunnerProcessTerminator terminator) =>
        new(root, provider, launcher, terminator, new RunnerTransitionTracker(), () => Now);

    private static RunnerDescriptor CreateRunner(string root, string folder, string agentName)
    {
        var path = Path.Combine(root, folder);
        Directory.CreateDirectory(Path.Combine(path, "bin"));
        File.WriteAllText(Path.Combine(path, ".runner"), JsonSerializer.Serialize(new
        {
            agentName,
            gitHubUrl = $"https://github.com/example/{agentName}",
            agentId = 1,
            workFolder = "_work"
        }));
        File.WriteAllText(Path.Combine(path, "run.cmd"), "@echo off\r\n");
        File.WriteAllText(Path.Combine(path, "run-helper.cmd.template"), "@echo off\r\n");
        File.WriteAllBytes(Path.Combine(path, "bin", "Runner.Listener.exe"), Array.Empty<byte>());
        return RunnerDiscovery.Discover(root).Single();
    }

    private static ProcessInventory Inventory(params ProcessSnapshot[] processes) =>
        new(processes, true, null);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class SequenceProvider : IProcessSnapshotProvider
    {
        private readonly Queue<ProcessInventory> _values;
        private ProcessInventory _last;

        public SequenceProvider(params ProcessInventory[] values)
        {
            _values = new Queue<ProcessInventory>(values);
            _last = values.Last();
        }

        public int CaptureCount { get; private set; }

        public ProcessInventory Capture()
        {
            CaptureCount++;
            if (_values.Count > 0) _last = _values.Dequeue();
            return _last;
        }
    }

    private sealed class CountingLauncher : IRunnerLauncher
    {
        public int CallCount { get; private set; }
        public void Launch(RunnerDescriptor runner) => CallCount++;
    }

    private sealed class FixedTerminator : IRunnerProcessTerminator
    {
        private readonly RunnerTerminationResult _result;
        public FixedTerminator(RunnerTerminationResult result) => _result = result;
        public int CallCount { get; private set; }
        public RunnerTerminationResult Terminate(RunnerDescriptor runner, ProcessSnapshot listener)
        {
            CallCount++;
            return _result;
        }
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "mrc-v014-restart-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }
        public void Dispose() { try { Directory.Delete(Path, true); } catch { } }
    }
}
