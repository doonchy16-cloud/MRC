namespace MRC.Core.Diagnostics;

public sealed class DoctorRepairService
{
    private readonly InstallHealthInspector _install;

    public DoctorRepairService(InstallHealthInspector install)
    {
        _install = install;
    }

    public static DoctorRepairRisk ClassifyRisk(string operation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        var value = operation.Trim().ToLowerInvariant();
        return value.Contains("process", StringComparison.Ordinal)
               || value.Contains("service", StringComparison.Ordinal)
               || value.Contains("runner", StringComparison.Ordinal)
               || value.Contains("delete", StringComparison.Ordinal)
               || value.Contains("termination", StringComparison.Ordinal)
               || value.Contains("configuration", StringComparison.Ordinal)
            ? DoctorRepairRisk.ApprovalRequired
            : DoctorRepairRisk.Automatic;
    }

    public DoctorRepairAction Execute(DoctorFinding finding, DoctorRunOptions options)
    {
        if (!finding.Repairable || finding.Risk is null)
        {
            return new DoctorRepairAction(
                finding.Id,
                finding.Name,
                finding.Risk ?? DoctorRepairRisk.ApprovalRequired,
                false,
                false,
                false,
                "Finding is not repairable by MRC.");
        }

        var authorized = finding.Risk == DoctorRepairRisk.Automatic
            ? options.ApplyAutomaticRepairs
            : options.IsApproved(finding.Id);
        if (!authorized)
        {
            return new DoctorRepairAction(
                finding.Id,
                finding.Name,
                finding.Risk.Value,
                false,
                false,
                false,
                finding.Risk == DoctorRepairRisk.ApprovalRequired
                    ? "Repair requires explicit approval."
                    : "Automatic repairs were not enabled.");
        }

        try
        {
            var executed = finding.Id switch
            {
                "install.path" => RepairPath(),
                "install.pointer" => _install.RepairActivePointer(),
                "install.launcher" => RepairLauncher(),
                _ => false
            };

            if (!executed)
            {
                return new DoctorRepairAction(
                    finding.Id,
                    finding.Name,
                    finding.Risk.Value,
                    true,
                    false,
                    false,
                    "No safe MRC-owned repair implementation exists for this finding.");
            }

            var verification = Verify(finding.Id);
            return new DoctorRepairAction(
                finding.Id,
                finding.Name,
                finding.Risk.Value,
                true,
                verification.Passed,
                verification.Passed,
                verification.Message);
        }
        catch (Exception ex)
        {
            return new DoctorRepairAction(
                finding.Id,
                finding.Name,
                finding.Risk.Value,
                true,
                false,
                false,
                $"Repair failed safely: {ex.Message}");
        }
    }

    public DoctorVerificationResult Verify(string findingId)
    {
        var snapshot = _install.Inspect();
        return findingId switch
        {
            "install.path" => new DoctorVerificationResult(
                findingId,
                snapshot.PathHealthy,
                snapshot.PathHealthy
                    ? "User PATH contains exactly one current MRC bin entry and no stale MRC bin entries."
                    : "User PATH remains missing, duplicated, or stale after repair."),
            "install.pointer" => new DoctorVerificationResult(
                findingId,
                snapshot.PointerHealthy,
                snapshot.PointerHealthy
                    ? $"Active version pointer resolves to a valid payload: {snapshot.ActiveVersion}."
                    : "Active version pointer still does not resolve to a valid payload."),
            "install.launcher" => new DoctorVerificationResult(
                findingId,
                snapshot.LauncherExists,
                snapshot.LauncherExists
                    ? "MRC launcher exists in the active bin directory."
                    : "MRC launcher is still missing."),
            _ => new DoctorVerificationResult(findingId, false, "No verification rule exists for this repair.")
        };
    }

    private bool RepairPath()
    {
        _install.RepairUserPath();
        return true;
    }

    private bool RepairLauncher()
    {
        _install.RepairLauncher();
        return true;
    }
}
