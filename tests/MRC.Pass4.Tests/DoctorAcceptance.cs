using System.Diagnostics;

namespace MRC.Pass4.Tests;

internal static class DoctorAcceptance
{
    public static int Run()
    {
        var tests = new (string Name, Action Body)[]
        {
            ("doctor aliases execute the structured repair-verify command surface", DoctorAliasesExecute),
            ("doctor preserves automatic-versus-approval repair safety", RepairAuthorityIsPresent),
            ("help separates Doctor repair from deep read-only Diagnose", HelpPresentsDoctorAndDiagnose)
        };
        var failures = 0;
        Console.WriteLine($"MRC PASS 4 doctor harness — {tests.Length} tests");
        foreach (var test in tests)
        {
            try { test.Body(); Console.WriteLine($"PASS  {test.Name}"); }
            catch (Exception ex) { failures++; Console.WriteLine($"FAIL  {test.Name}\n      {ex.Message}"); }
        }
        Console.WriteLine(failures == 0 ? $"PASS  all {tests.Length} PASS 4 doctor tests" : $"FAIL  {failures} of {tests.Length} PASS 4 doctor tests");
        return failures;
    }

    private static void DoctorAliasesExecute()
    {
        BuildCli(); ProcessResult? baseline = null;
        foreach (var alias in new[] { "-doctor", "--doctor" })
        {
            var result = Run(CliExe(), alias);
            Require(result.ExitCode is 0 or 1, $"{alias} returned unexpected exit code {result.ExitCode}.\n{result.Output}");
            foreach (var marker in new[] { "MRC Doctor", "FIND", "REPAIR", "VERIFY", "RESULT", "Health:", "Machine identity", "Runner root", "PATH installation", "Version authority", "Release reachability" })
                Require(result.Output.Contains(marker, StringComparison.OrdinalIgnoreCase), $"{alias} omitted '{marker}'.\n{result.Output}");
            if (baseline is null) baseline = result; else Require(result.ExitCode == baseline.ExitCode, "Doctor aliases must have equivalent exit semantics.");
        }
    }

    private static void RepairAuthorityIsPresent()
    {
        var root = RepoRoot();
        var source = File.ReadAllText(Path.Combine(root, "src", "MRC.Core", "Diagnostics", "DoctorService.cs"));
        var models = File.ReadAllText(Path.Combine(root, "src", "MRC.Core", "Diagnostics", "DoctorModels.cs"));
        foreach (var marker in new[] { "ApplyAutomaticRepairs", "VerificationPassed", "DoctorRepairRisk" })
            Require(source.Contains(marker, StringComparison.Ordinal) || models.Contains(marker, StringComparison.Ordinal), $"Doctor repair/verification authority omitted '{marker}'.");
        Require((source + models).Contains("ApprovalRequired", StringComparison.Ordinal), "Doctor does not represent approval-required repair risk.");
        foreach (var forbidden in new[] { "RunnerControlService", "RunnerOperationsService", ".Start(", ".StopIdle(", "ForceStop(" })
            Require(!source.Contains(forbidden, StringComparison.Ordinal), $"Doctor core must not directly invoke runner lifecycle primitive '{forbidden}'.");
    }

    private static void HelpPresentsDoctorAndDiagnose()
    {
        BuildCli();
        var help = Run(CliExe(), "--help");
        Require(help.ExitCode == 0, $"--help failed.\n{help.Output}");
        Require(help.Output.Contains("-doctor", StringComparison.OrdinalIgnoreCase), "Help omits Doctor.");
        Require(help.Output.Contains("-diagnose", StringComparison.OrdinalIgnoreCase), "Help omits Diagnose.");
        Require(help.Output.Contains("repair", StringComparison.OrdinalIgnoreCase), "Help does not explain Doctor repair behavior.");
        Require(help.Output.Contains("read-only", StringComparison.OrdinalIgnoreCase), "Help does not identify Diagnose as read-only.");
    }

    private static void BuildCli() { var r = Run("dotnet", "build", Path.Combine(RepoRoot(), "src", "MRC.Cli", "MRC.Cli.csproj"), "-c", "Release", "-v", "minimal"); Require(r.ExitCode == 0, $"CLI build failed.\n{r.Output}"); }
    private static string CliExe() => Path.Combine(RepoRoot(), "src", "MRC.Cli", "bin", "Release", "net10.0", "MRC.exe");
    private static ProcessResult Run(string fileName, params string[] arguments)
    {
        var psi = new ProcessStartInfo(fileName) { WorkingDirectory = RepoRoot(), RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        foreach (var arg in arguments) psi.ArgumentList.Add(arg);
        using var p = Process.Start(psi) ?? throw new InvalidOperationException($"Could not start {fileName}.");
        var stdout = p.StandardOutput.ReadToEnd(); var stderr = p.StandardError.ReadToEnd(); p.WaitForExit();
        return new ProcessResult(p.ExitCode, stdout + stderr);
    }
    private static string RepoRoot() { var d = new DirectoryInfo(Directory.GetCurrentDirectory()); while (d is not null) { if (File.Exists(Path.Combine(d.FullName, "Auth", "0000_MasterAuth.md"))) return d.FullName; d = d.Parent; } throw new InvalidOperationException("Could not locate MRC repository root."); }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private sealed record ProcessResult(int ExitCode, string Output);
}
