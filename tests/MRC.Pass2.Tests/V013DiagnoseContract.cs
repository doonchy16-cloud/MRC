using System.Runtime.CompilerServices;
using System.Text.Json;
using MRC.Core.Diagnostics;
using MRC.Core.Runners;
using MRC.Core.Runtime;

namespace MRC.Pass2.Tests;

internal static class V013DiagnoseContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        ExternalServiceEvidenceMustStayOutOfManagedRunnerError();
        Console.WriteLine("PASS  V013 P0-G Diagnose shares runtime ownership truth without duplicate access errors");
    }

    private static void ExternalServiceEvidenceMustStayOutOfManagedRunnerError()
    {
        using var temp = new TempDirectory();
        CreateRunner(temp.Path, "runner-off", "OffRunner", "https://github.com/example/OffRepo");

        var unresolvedSystemListener = new ProcessSnapshot(
            9001,
            7000,
            "Runner.Listener",
            null,
            "Access is denied.",
            0);
        var processProvider = new FixedProcessProvider(new ProcessInventory(
            new[] { unresolvedSystemListener },
            true,
            null));
        var serviceProvider = new FixedServiceEvidenceProvider(
            new RunnerServiceEvidence(
                "actions.runner.doonchy16-cloud-Lotto_engine.Lotto_MainPC_Runner",
                "\"C:\\actions-runner-lotto-mainpc\\bin\\RunnerService.exe\"",
                7000,
                "GitHub Actions Runner (Lotto)",
                "RUNNING",
                "Auto",
                "LocalSystem"));

        var diagnose = new DiagnoseService(temp.Path, processProvider, serviceProvider);
        var report = diagnose.Run();

        var managed = report.Runners.Single();
        Require(managed.State == RunnerState.OFF,
            $"External service evidence poisoned managed Diagnose state: {managed.State}: {managed.Error}");
        Require(string.IsNullOrWhiteSpace(managed.Error),
            $"OFF managed Diagnose row carried a global error: {managed.Error}");
        Require(string.IsNullOrWhiteSpace(managed.InspectionError),
            $"OFF managed Diagnose row duplicated global process inspection error: {managed.InspectionError}");

        var system = report.SystemProcesses.Single(process => process.ProcessId == 9001);
        Require(system.Kind == RunnerSystemFindingKind.External,
            $"Service-owned inaccessible listener should be EXTERNAL, got {system.Kind}.");
        Require(string.Equals(system.InspectionError, "Access is denied.", StringComparison.Ordinal),
            $"System finding should carry the access error once, got '{system.InspectionError ?? "<null>"}'.");
        Require(system.OwnershipProof.Contains("Lotto_MainPC_Runner", StringComparison.OrdinalIgnoreCase),
            $"External system finding did not preserve service ownership proof: {system.OwnershipProof}");
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

    private sealed class FixedProcessProvider : IProcessSnapshotProvider
    {
        private readonly ProcessInventory _inventory;
        public FixedProcessProvider(ProcessInventory inventory) => _inventory = inventory;
        public ProcessInventory Capture() => _inventory;
    }

    private sealed class FixedServiceEvidenceProvider : IRunnerServiceEvidenceProvider
    {
        private readonly IReadOnlyList<RunnerServiceEvidence> _services;
        public FixedServiceEvidenceProvider(params RunnerServiceEvidence[] services) => _services = services;
        public IReadOnlyList<RunnerServiceEvidence> Inspect() => _services;
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "mrc-v013-diagnose-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try { Directory.Delete(Path, true); } catch { }
        }
    }
}
