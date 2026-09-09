using System.Diagnostics;
using System.Security.Principal;
using MRC.Core.Runners;
using MRC.Core.Runtime;

namespace MRC.Core.Diagnostics;

public sealed class DiagnoseService
{
    public DiagnoseReport Run()
    {
        var root = MrcConstants.RunnerRoot;
        var runners = RunnerDiscovery.Discover(root);
        var provider = new WindowsProcessSnapshotProvider();
        ProcessInventory inventory;
        try
        {
            inventory = provider.Capture();
        }
        catch (Exception ex)
        {
            inventory = new ProcessInventory(Array.Empty<ProcessSnapshot>(), false, ex.Message);
        }

        var runnerFindings = new List<DiagnoseRunnerFinding>(runners.Count);
        foreach (var runner in runners)
        {
            var association = RunnerProcessAssociator.Associate(runner, inventory);
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
                listener?.InspectionError ?? InspectionFailureForUnresolvedRunnerProcesses(inventory)));
        }

        var services = new WindowsServiceInspector().Inspect();
        var servicesByPid = services
            .Where(service => service.ProcessId is > 0)
            .GroupBy(service => service.ProcessId!.Value)
            .ToDictionary(group => group.Key, group => group.First());

        var processAnalysis = RunnerProcessInventoryAnalyzer.Analyze(root, runners, inventory);
        var systemProcesses = processAnalysis.ObservedProcesses
            .Where(process => process.Ownership != RunnerProcessOwnershipKind.Managed)
            .Select(process => BuildSystemFinding(process, root, servicesByPid))
            .ToArray();

        var folders = RunnerFolderClassifier.ClassifyImmediateChildren(root);
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

    private static string? InspectionFailureForUnresolvedRunnerProcesses(ProcessInventory inventory)
    {
        var errors = inventory.Processes
            .Where(process => string.IsNullOrWhiteSpace(process.ExecutablePath)
                              && IsRunnerProcess(process.ProcessName)
                              && !string.IsNullOrWhiteSpace(process.InspectionError))
            .Select(process => $"PID {process.ProcessId} {process.ProcessName}: {process.InspectionError}")
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        return errors.Length == 0 ? null : string.Join("; ", errors);
    }

    private static bool IsRunnerProcess(string processName) =>
        processName.Equals("Runner.Listener", StringComparison.OrdinalIgnoreCase)
        || processName.Equals("Runner.Listener.exe", StringComparison.OrdinalIgnoreCase)
        || processName.Equals("Runner.Worker", StringComparison.OrdinalIgnoreCase)
        || processName.Equals("Runner.Worker.exe", StringComparison.OrdinalIgnoreCase);

    private static DiagnoseProcessFinding BuildSystemFinding(
        RunnerObservedProcess process,
        string root,
        IReadOnlyDictionary<int, WindowsServiceInfo> servicesByPid)
    {
        WindowsServiceInfo? relatedService = null;
        if (process.ParentProcessId is int parentPid && servicesByPid.TryGetValue(parentPid, out var parentService))
        {
            relatedService = parentService;
        }
        else if (servicesByPid.TryGetValue(process.ProcessId, out var directService))
        {
            relatedService = directService;
        }

        string? serviceExecutable = null;
        var provenExternalService = relatedService is not null
            && TryExecutablePath(relatedService.PathName, out serviceExecutable)
            && serviceExecutable is not null
            && !IsUnderRoot(serviceExecutable, root);

        var kind = process.Ownership == RunnerProcessOwnershipKind.External || provenExternalService
            ? RunnerSystemFindingKind.External
            : RunnerSystemFindingKind.Unattributed;

        var proof = provenExternalService
            ? $"Parent/service process is Windows service '{relatedService!.Name}' with executable outside managed root: {serviceExecutable}."
            : process.Reason;

        return new DiagnoseProcessFinding(
            process.ProcessId,
            process.ParentProcessId,
            process.SessionId,
            process.ProcessName,
            process.ExecutablePath,
            kind,
            proof,
            process.InspectionError,
            relatedService?.Name);
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

    private static bool TryExecutablePath(string? commandLine, out string? executablePath)
    {
        executablePath = null;
        if (string.IsNullOrWhiteSpace(commandLine)) return false;
        var value = Environment.ExpandEnvironmentVariables(commandLine.Trim());
        if (value.StartsWith('"'))
        {
            var end = value.IndexOf('"', 1);
            if (end <= 1) return false;
            executablePath = value[1..end];
            return true;
        }

        var exe = value.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
        if (exe < 0) return false;
        executablePath = value[..(exe + 4)].Trim();
        return true;
    }

    private static bool IsUnderRoot(string candidate, string root)
    {
        try
        {
            var normalizedRoot = RunnerPath.Normalize(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                 + Path.DirectorySeparatorChar;
            var normalizedCandidate = RunnerPath.Normalize(candidate);
            return normalizedCandidate.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
