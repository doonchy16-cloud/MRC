using System.Diagnostics;

namespace MRC.Pass4.Tests;

internal static class DoctorAcceptance
{
    public static int Run()
    {
        var tests = new (string Name, Action Body)[]
        {
            ("doctor aliases execute the live diagnostic command surface", DoctorAliasesExecute),
            ("doctor covers every authority-required check and remains read-only", RequiredChecksArePresentAndReadOnly),
            ("help presents doctor as an active PASS 4 command", HelpPresentsLiveDoctor)
        };

        var failures = 0;
        Console.WriteLine($"MRC PASS 4 doctor harness — {tests.Length} tests");
        foreach (var test in tests)
        {
            try
            {
                test.Body();
                Console.WriteLine($"PASS  {test.Name}");
            }
            catch (Exception ex)
            {
                failures++;
                Console.WriteLine($"FAIL  {test.Name}");
                Console.WriteLine($"      {ex.Message}");
            }
        }

        Console.WriteLine(failures == 0
            ? $"PASS  all {tests.Length} PASS 4 doctor tests"
            : $"FAIL  {failures} of {tests.Length} PASS 4 doctor tests");
        return failures;
    }

    private static void DoctorAliasesExecute()
    {
        BuildCli();
        ProcessResult? baseline = null;
        foreach (var alias in new[] { "-doctor", "--doctor" })
        {
            var result = Run(CliExe(), alias);
            Require(result.ExitCode != 4, $"{alias} is still reserved instead of executing PASS 4 diagnostics.");
            Require(result.ExitCode is 0 or 1, $"{alias} returned unexpected diagnostic exit code {result.ExitCode}.\n{result.Output}");
            foreach (var marker in new[]
            {
                "MRC Doctor", "Machine identity:", "Runner root:", "PATH installation:",
                "Version authority:", "Discovered runners:", "Runner signatures:", "Release reachability:"
            })
            {
                Require(result.Output.Contains(marker, StringComparison.OrdinalIgnoreCase), $"{alias} omitted '{marker}'.\n{result.Output}");
            }

            if (baseline is null) baseline = result;
            else Require(result.ExitCode == baseline.ExitCode, "Doctor aliases must have equivalent exit semantics.");
        }
    }

    private static void RequiredChecksArePresentAndReadOnly()
    {
        var path = Path.Combine(RepoRoot(), "src", "MRC.Core", "Diagnostics", "DoctorService.cs");
        Require(File.Exists(path), "DoctorService.cs is missing.");
        var source = File.ReadAllText(path);
        foreach (var marker in new[]
        {
            "Environment.MachineName", "MrcConstants.RunnerRoot", "EnvironmentVariableTarget.User",
            "BuildInfo.Version", "RunnerPath.HasSignature", "HttpClient",
            "Machine identity", "Runner root", "PATH installation", "Version authority",
            "Discovered runners", "Runner signatures", "Release reachability"
        })
        {
            Require(source.Contains(marker, StringComparison.Ordinal), $"Doctor implementation omitted authority marker '{marker}'.");
        }

        foreach (var forbidden in new[] { "RunnerControlService", "RunnerOperationsService", ".Start(", ".StopIdle(", "ForceStop(" })
        {
            Require(!source.Contains(forbidden, StringComparison.Ordinal), $"Doctor must be read-only but contains lifecycle primitive '{forbidden}'.");
        }
    }

    private static void HelpPresentsLiveDoctor()
    {
        var dispatcher = File.ReadAllText(Path.Combine(RepoRoot(), "src", "MRC.Cli", "CliDispatcher.cs"));
        Require(!dispatcher.Contains("Diagnostics (implemented in PASS 4)", StringComparison.Ordinal),
            "Help still describes doctor as future work after the command is active.");
    }

    private static void BuildCli()
    {
        var result = Run("dotnet", "build", Path.Combine(RepoRoot(), "src", "MRC.Cli", "MRC.Cli.csproj"), "-c", "Release", "-v", "minimal");
        Require(result.ExitCode == 0, $"CLI build failed.\n{result.Output}");
    }

    private static string CliExe() => Path.Combine(RepoRoot(), "src", "MRC.Cli", "bin", "Release", "net10.0", "MRC.exe");

    private static ProcessResult Run(string fileName, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            WorkingDirectory = RepoRoot(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);
        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Could not start {fileName}.");
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return new ProcessResult(process.ExitCode, stdout + stderr);
    }

    private static string RepoRoot()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Auth", "0000_MasterAuth.md"))) return current.FullName;
            current = current.Parent;
        }
        throw new InvalidOperationException("Could not locate MRC repository root.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed record ProcessResult(int ExitCode, string Output);
}
