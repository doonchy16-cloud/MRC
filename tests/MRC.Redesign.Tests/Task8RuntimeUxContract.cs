using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using MRC.Core.Runners;
using MRC.Core.Runtime;
using MRC.Gui.Presentation;

internal static class Task8RuntimeUxContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifySystemFindingsStaySeparateFromManagedRows();
        VerifyCustomDiagnosticDialogContract();
        Console.WriteLine("PASS  Task8B runtime findings + custom diagnostics UX");
    }

    private static void VerifySystemFindingsStaySeparateFromManagedRows()
    {
        var runner = new RunnerDescriptor(
            @"D:\Git_Runners_Main\BBAP_runner",
            "BBAPRunner",
            "https://github.com/doonchy16-cloud/BBAP",
            "BBAP",
            1,
            "_work",
            null);
        var report = new RunnerRuntimeReport(
            new[] { new RunnerSnapshot(runner, RunnerState.IDLE, null) },
            new[]
            {
                new RunnerSystemFinding(
                    RunnerSystemFindingKind.External,
                    31716,
                    50276,
                    0,
                    "Runner.Listener",
                    null,
                    "Service-hosted runner is outside the authorized MRC root and is not controllable by MRC.")
            });

        var dashboard = new RunnerDashboardViewModel();
        var applyReport = typeof(RunnerDashboardViewModel).GetMethod(
            "ApplyRuntimeReport",
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: new[] { typeof(RunnerRuntimeReport) },
            modifiers: null);
        Require(applyReport is not null,
            "RunnerDashboardViewModel.ApplyRuntimeReport(RunnerRuntimeReport) is missing.");

        applyReport!.Invoke(dashboard, new object[] { report });

        Require(dashboard.Rows.Count == 1, $"Expected exactly one managed row, got {dashboard.Rows.Count}.");
        Require(dashboard.Rows[0].State == RunnerState.IDLE,
            $"External finding contaminated managed row state: expected IDLE, got {dashboard.Rows[0].State}.");
        Require(dashboard.ErrorCount == 0,
            $"External finding contaminated managed ERROR count: expected 0, got {dashboard.ErrorCount}.");

        var findingsProperty = typeof(RunnerDashboardViewModel).GetProperty("SystemFindings");
        Require(findingsProperty?.GetValue(dashboard) is ICollection findings && findings.Count == 1,
            "Dedicated SystemFindings model does not contain the external process finding.");

        var hasFindings = typeof(RunnerDashboardViewModel).GetProperty("HasSystemFindings")?.GetValue(dashboard) as bool?;
        Require(hasFindings == true, "HasSystemFindings is not true for an external process finding.");

        var summary = typeof(RunnerDashboardViewModel).GetProperty("SystemFindingsSummary")?.GetValue(dashboard)?.ToString();
        Require(!string.IsNullOrWhiteSpace(summary)
                && summary.Contains("EXTERNAL", StringComparison.OrdinalIgnoreCase)
                && summary.Contains("31716", StringComparison.Ordinal)
                && summary.Contains("SESSION 0", StringComparison.OrdinalIgnoreCase)
                && summary.Contains("not controllable", StringComparison.OrdinalIgnoreCase),
            $"System findings summary is incomplete or misleading: '{summary ?? "<null>"}'.");
    }

    private static void VerifyCustomDiagnosticDialogContract()
    {
        var repoRoot = Directory.GetCurrentDirectory();
        var xamlPath = Path.Combine(repoRoot, "src", "MRC.Gui", "DiagnosticDialog.xaml");
        Require(File.Exists(xamlPath), "DiagnosticDialog.xaml is missing.");
        var xaml = File.ReadAllText(xamlPath);

        Require(xaml.Contains("Cascadia Mono, Consolas", StringComparison.OrdinalIgnoreCase),
            "Diagnostic dialog does not use the MRC terminal font stack.");
        Require(xaml.Contains("#0B0F14", StringComparison.OrdinalIgnoreCase),
            "Diagnostic dialog does not use the near-black MRC surface.");
        Require(xaml.Contains("RUNNER DIAGNOSTIC", StringComparison.OrdinalIgnoreCase),
            "Diagnostic dialog does not identify itself as an MRC runner diagnostic surface.");
        Require(xaml.Contains("RunnerName", StringComparison.Ordinal)
                && xaml.Contains("RepositoryName", StringComparison.Ordinal)
                && xaml.Contains("DirectoryPath", StringComparison.Ordinal)
                && xaml.Contains("StateText", StringComparison.Ordinal)
                && xaml.Contains("DiagnosticText", StringComparison.Ordinal),
            "Diagnostic dialog is missing required runner forensic fields.");

        var mainWindowPath = Path.Combine(repoRoot, "src", "MRC.Gui", "MainWindow.xaml.cs");
        var source = File.ReadAllText(mainWindowPath);
        var errorBranchStart = source.IndexOf("if (row.State == RunnerState.ERROR)", StringComparison.Ordinal);
        var nextBranch = source.IndexOf("if (row.State is not RunnerState.OFF", errorBranchStart + 1, StringComparison.Ordinal);
        Require(errorBranchStart >= 0 && nextBranch > errorBranchStart,
            "Could not locate the ERROR-row control branch in MainWindow.");
        var errorBranch = source[errorBranchStart..nextBranch];
        Require(errorBranch.Contains("DiagnosticDialog", StringComparison.Ordinal),
            "ERROR-row details do not open DiagnosticDialog.");
        Require(!errorBranch.Contains("MessageBox.Show", StringComparison.Ordinal),
            "ERROR-row details still use a generic Windows MessageBox.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
