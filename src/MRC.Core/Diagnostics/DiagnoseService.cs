using System.Diagnostics;
using System.Security.Principal;
using MRC.Core.Runners;
using MRC.Core.Runtime;

namespace MRC.Core.Diagnostics;

public sealed class DiagnoseService
{
    private readonly string _root;
    private readonly IProcessSnapshotProvider _processProvider;
    private readonly IRunnerServiceEvidenceProvider _serviceEvidenceProvider;

    public DiagnoseService()
        : this(
            MrcConstants.RunnerRoot,
            new WindowsProcessSnapshotProvider(),
            new WindowsRunnerServiceEvidenceProvider())
    {
    }

    internal DiagnoseService(
        string root,
        IProcessSnapshotProvider processProvider,
        IRunnerServiceEvidenceProvider serviceEvidenceProvider)
    {
        _root = RunnerPath.Normalize(root);
        _processProvider = processProvider ?? throw new ArgumentNullException(nameof(processProvider));
        _serviceEvidenceProvider = serviceEvidenceProvider ?? throw new ArgumentNullException(nameof(serviceEvidenceProvider));
    }

    public DiagnoseReport Run()
    {
        var runners = RunnerDiscovery.Discover(_root);
        var inventory = CaptureInventory();
        var serviceEvidence = CaptureServiceEvidence();

        var runnerFindings = new List<DiagnoseRunnerFinding>(runners.Count);
        foreach (var runner in runners)
        {
            var association = RunnerProcessAssociator.AssociateExact(runner, inventory);
            var state = RunnerStateEvaluator.Evaluate(runner, association, null, DateTimeOffset.UtcNow);
            var expectedListenerPath = RunnerPath.ListenerExecutable(runner.DirectoryPath);
            var listener = association.Listener;
            var ownershipProof = listener is not null
                ? $"Managed ownership proven by exact executable path match: {listener.ExecutablePath} == {expectedListenerPath}."
                : !string.IsNullOrWhiteSpace(association.Error)
                    ? $"Managed ownership not proven: {association.Error}"
                    : $"No exact managed listener or worker process is present for {runner.DirectoryPath}; runner is OFF.";

            runnerFindings.Add(new DiagnoseRunnerFinding(
                runner.AgentName ?? Path.GetFileName(runner.DirectoryPath),
                runner.RepositoryName ?? "<unknown>",
                runner.DirectoryPath,
                state,
                association.Error ?? runner.IdentityError,
                expectedListenerPath,
                listener?.ProcessId,
                association.Workers.Select(worker => worker.ProcessId).ToArray(),
                listener?.ParentProcessId,
                listener?.SessionId,
                listener?.ProcessName,
                listener?.ExecutablePath,
                ownershipProof,
                listener?.InspectionError));
        }

        var processAnalysis = RunnerProcessInventoryAnalyzer.Analyze(
            _root,
            runners,
            inventory,
            serviceEvidence);
        var systemProcesses = processAnalysis.ObservedProcesses
            .Where(process => process.Ownership != RunnerProcessOwnershipKind.Managed)
            .Select(process => new DiagnoseProcessFinding(
                process.ProcessId,
                process.ParentProcessId,
                process.SessionId,
                process.ProcessName,
                process.ExecutablePath,
                process.Ownership == RunnerProcessOwnershipKind.External
                    ? RunnerSystemFindingKind.External
                    : RunnerSystemFindingKind.Unattributed,
                process.Reason,
                process.InspectionError,
                process.ServiceName))
            .ToArray();

        var services = serviceEvidence
            .Select(service => new WindowsServiceInfo(
                service.Name,
                service.DisplayName,
                service.State,
                service.StartMode,
                service.StartName,
                service.PathName,
                service.ProcessId))
            .ToArray();

        var folders = RunnerFolderClassifier.ClassifyImmediateChildren(_root);
        return new DiagnoseReport(
            Environment.MachineName,
            Environment.UserName,
            CurrentSessionId(),
            IsElevated(),
            runnerFindings,
            systemProcesses,
            folders,
            services);
    }

    private ProcessInventory CaptureInventory()
    {
        try
        {
            return _processProvider.Capture();
        }
        catch (Exception ex)
        {
            return new ProcessInventory(Array.Empty<ProcessSnapshot>(), false, ex.Message);
        }
    }

    private IReadOnlyList<RunnerServiceEvidence> CaptureServiceEvidence()
    {
        try
        {
            return _serviceEvidenceProvider.Inspect();
        }
        catch
        {
            return Array.Empty<RunnerServiceEvidence>();
        }
    }

    private static int? CurrentSessionId()
    {
        try
        {
            using var process = Process.GetCurrentProcess();
            return process.SessionId;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsElevated()
    {
        if (!OperatingSystem.IsWindows()) return false;
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }
}
