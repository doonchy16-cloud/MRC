using System.Runtime.CompilerServices;
using MRC.Core.Runtime;

internal static class Task22V014ControlReliabilityContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifyParentFallbackRecoversProtectedProcessAncestry();
        VerifyRecoveredAncestryProvesExternalServiceOwnership();
        VerifyUnresolvedEvidenceRemainsUnattributed();
        Console.WriteLine("PASS  Task22 v0.0.14 protected runner ownership evidence is safe and non-global-blocking");
    }

    private static void VerifyParentFallbackRecoversProtectedProcessAncestry()
    {
        var fallback = new Dictionary<int, int?> { [4076] = 3120 };
        var result = ParentProcessEvidence.Resolve(
            4076,
            handleReadSucceeded: false,
            handleParentProcessId: null,
            handleError: "Access is denied.",
            fallback);

        Require(result.ParentProcessId == 3120,
            $"Protected process parent fallback returned {result.ParentProcessId}; expected 3120.");
        Require(string.IsNullOrWhiteSpace(result.Error),
            $"Recovered parent evidence must clear the failed handle-read error, got '{result.Error}'.");
    }

    private static void VerifyRecoveredAncestryProvesExternalServiceOwnership()
    {
        var inventory = new ProcessInventory(new[]
        {
            new ProcessSnapshot(4076, 3120, "Runner.Listener", null, "Access is denied.", 0),
            new ProcessSnapshot(3120, 900, "RunnerService", @"C:\actions-runner-lotto-mainpc\bin\RunnerService.exe", null, 0)
        }, true, null);

        var services = new[]
        {
            new RunnerServiceEvidence(
                "actions.runner.doonchy16-cloud-Lotto_engine.Lotto_MainPC_Runner",
                @"\"C:\actions-runner-lotto-mainpc\bin\RunnerService.exe\"",
                3120,
                "GitHub Actions Runner",
                "RUNNING",
                "Auto",
                "LocalSystem")
        };

        var analysis = RunnerProcessInventoryAnalyzer.Analyze(
            @"D:\Git_Runners_Main",
            Array.Empty<MRC.Core.Runners.RunnerDescriptor>(),
            inventory,
            services);

        var listener = analysis.ObservedProcesses.Single(p => p.ProcessId == 4076);
        Require(listener.Ownership == RunnerProcessOwnershipKind.External,
            $"Protected service-owned listener classified as {listener.Ownership}; expected External.");
        Require(listener.ServiceName == services[0].Name,
            "Protected service-owned listener did not retain service ownership evidence.");
    }

    private static void VerifyUnresolvedEvidenceRemainsUnattributed()
    {
        var inventory = new ProcessInventory(new[]
        {
            new ProcessSnapshot(5000, null, "Runner.Listener", null, "Access is denied.", 0)
        }, true, null);

        var analysis = RunnerProcessInventoryAnalyzer.Analyze(
            @"D:\Git_Runners_Main",
            Array.Empty<MRC.Core.Runners.RunnerDescriptor>(),
            inventory,
            Array.Empty<RunnerServiceEvidence>());

        var listener = analysis.ObservedProcesses.Single();
        Require(listener.Ownership == RunnerProcessOwnershipKind.Unattributed,
            $"Genuinely unresolved listener classified as {listener.Ownership}; expected Unattributed.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
