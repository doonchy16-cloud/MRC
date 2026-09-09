namespace MRC.Core.Diagnostics;

public enum DoctorCheckStatus
{
    Pass = 0,
    Warning = 1,
    Fail = 2
}

public enum DoctorHealth
{
    Healthy = 0,
    Repaired = 1,
    Attention = 2,
    Blocked = 3
}

public enum DoctorRepairRisk
{
    Automatic = 0,
    ApprovalRequired = 1
}

public sealed record DoctorCheck(string Name, DoctorCheckStatus Status, string Message);

public sealed record DoctorFinding(
    string Id,
    string Name,
    DoctorCheckStatus Status,
    string Message,
    bool Repairable = false,
    DoctorRepairRisk? Risk = null);

public sealed record DoctorRepairAction(
    string Id,
    string Description,
    DoctorRepairRisk Risk,
    bool Attempted,
    bool Succeeded,
    bool VerificationPassed,
    string Message);

public sealed record DoctorVerificationResult(
    string FindingId,
    bool Passed,
    string Message);

public sealed record DoctorRunOptions(
    bool ApplyAutomaticRepairs = false,
    IReadOnlySet<string>? ApprovedRepairIds = null)
{
    public bool IsApproved(string repairId) => ApprovedRepairIds?.Contains(repairId) == true;
}

public sealed record DoctorReport(
    IReadOnlyList<DoctorFinding> Findings,
    IReadOnlyList<DoctorRepairAction> Repairs,
    IReadOnlyList<DoctorVerificationResult> VerificationResults,
    DoctorHealth Health)
{
    public IReadOnlyList<DoctorCheck> Checks => Findings
        .Select(finding => new DoctorCheck(finding.Name, finding.Status, finding.Message))
        .ToArray();

    public int ExitCode => Health == DoctorHealth.Blocked ? 1 : 0;
}
