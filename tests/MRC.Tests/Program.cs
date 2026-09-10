using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;

namespace MRC.Tests;

internal static class Program
{
    private static readonly string RepoRoot = FindRepoRoot();

    public static int Main()
    {
        var tests = new (string Name, Action Body)[]
        {
            ("CLI project builds", BuildCli),
            ("GUI project builds", BuildGui),
            ("version aliases report canonical pre-cert identity", VersionAliases),
            ("help aliases report canonical command surface", HelpAliases),
            ("doctor diagnose and update are live commands", OperationalCommandsLive),
            ("unknown CLI flags fail clearly", UnknownFlagFails),
            ("environment fence authorizes only exact machine and exact-root evidence", EnvironmentFenceIsFailClosed),
            ("runner control stays isolated from CLI GUI and install layers", RunnerControlLayering),
            ("installer follows user-scoped PATH architecture", InstallerArchitecture),
            ("packaging creates the v0.0.13 pre-cert zip checksum and icon authority", PackagingSkeleton)
        };

        var failures = 0;
        Console.WriteLine($"MRC PASS 1 regression harness — {tests.Length} tests");
        foreach (var test in tests)
        {
            try { test.Body(); Console.WriteLine("PASS  " + test.Name); }
            catch (Exception ex) { failures++; Console.WriteLine($"FAIL  {test.Name}\n      {ex.Message}"); }
        }
        Console.WriteLine();
        Console.WriteLine(failures == 0 ? $"PASS  all {tests.Length} PASS 1 regression tests" : $"FAIL  {failures} of {tests.Length} PASS 1 regression tests");
        return failures == 0 ? 0 : 1;
    }

    private static void BuildCli()
    {
        var result = Run("dotnet", "build", Path.Combine(RepoRoot, "src", "MRC.Cli", "MRC.Cli.csproj"), "-c", "Release", "-v", "minimal");
        Require(result.ExitCode == 0, $"CLI build failed.\n{result.Output}");
    }

    private static void BuildGui()
    {
        var result = Run("dotnet", "build", Path.Combine(RepoRoot, "src", "MRC.Gui", "MRC.Gui.csproj"), "-c", "Release", "-v", "minimal");
        Require(result.ExitCode == 0, $"GUI build failed.\n{result.Output}");
    }

    private static void VersionAliases()
    {
        EnsureCliBuilt();
        foreach (var alias in new[] { "-v", "-version", "--version" })
        {
            var result = Run(CliExe(), alias);
            Require(result.ExitCode == 0, $"{alias} exited {result.ExitCode}.\n{result.Output}");
            Require(result.Output.Contains("Main Runner Control", StringComparison.Ordinal), $"{alias} omitted product name.");
            Require(result.Output.Contains("Version: 0.0.13", StringComparison.Ordinal), $"{alias} omitted v0.0.13 identity.");
            Require(result.Output.Contains("Channel: precert", StringComparison.OrdinalIgnoreCase), $"{alias} omitted pre-cert channel.");
            Require(result.Output.Contains("Stage: PRE-CERTIFICATION", StringComparison.OrdinalIgnoreCase), $"{alias} omitted release stage.");
            Require(result.Output.Contains("Final target: 0.1.0", StringComparison.OrdinalIgnoreCase), $"{alias} omitted final target.");
            Require(result.Output.Contains(@"Runner root: D:\Git_Runners_Main", StringComparison.Ordinal), $"{alias} omitted runner root.");
            Require(result.Output.Contains("Install location:", StringComparison.Ordinal), $"{alias} omitted install location.");
        }
    }

    private static void HelpAliases()
    {
        EnsureCliBuilt();
        foreach (var alias in new[] { "-h", "-help", "--help" })
        {
            var result = Run(CliExe(), alias);
            Require(result.ExitCode == 0, $"{alias} exited {result.ExitCode}.\n{result.Output}");
            foreach (var command in new[] { "MRC -v", "MRC -doctor", "MRC -diagnose", "MRC -update" }) Require(result.Output.Contains(command, StringComparison.Ordinal), $"Help omitted {command}.");
            Require(!result.Output.Contains("MRC-version", StringComparison.Ordinal), "Help introduced a forbidden separate canonical executable.");
        }
    }

    private static void OperationalCommandsLive()
    {
        EnsureCliBuilt();
        foreach (var alias in new[] { "-doctor", "--doctor" })
        {
            var result = Run(CliExe(), alias);
            Require(result.ExitCode is 0 or 1, $"{alias} must execute Doctor with exit code 0 or 1, got {result.ExitCode}.\n{result.Output}");
            foreach (var marker in new[] { "MRC Doctor", "FIND", "REPAIR", "VERIFY", "RESULT", "Machine identity", "Release reachability" }) Require(result.Output.Contains(marker, StringComparison.OrdinalIgnoreCase), $"{alias} omitted '{marker}'.");
        }

        foreach (var alias in new[] { "-diagnose", "--diagnose" })
        {
            var result = Run(CliExe(), alias);
            Require(result.ExitCode is 0 or 1, $"{alias} must execute read-only Diagnose with exit code 0 or 1, got {result.ExitCode}.\n{result.Output}");
            Require(result.Output.Contains("MRC Diagnose", StringComparison.OrdinalIgnoreCase), $"{alias} omitted Diagnose heading.");
        }

        var dispatcher = File.ReadAllText(Path.Combine(RepoRoot, "src", "MRC.Cli", "CliDispatcher.cs"));
        Require(dispatcher.Contains("UpdateProgress", StringComparison.Ordinal), "Live update path does not stream staged progress.");
    }

    private static void UnknownFlagFails()
    {
        EnsureCliBuilt();
        var result = Run(CliExe(), "--definitely-not-an-mrc-flag");
        Require(result.ExitCode == 2, $"Unknown flag must return exit code 2, got {result.ExitCode}.");
        Require(result.Output.Contains("Unknown option", StringComparison.OrdinalIgnoreCase), "Unknown flag error was not explicit.");
    }

    private static void EnvironmentFenceIsFailClosed()
    {
        EnsureCliBuilt();
        var corePath = Path.Combine(RepoRoot, "src", "MRC.Cli", "bin", "Release", "net10.0", "MRC.Core.dll");
        Require(File.Exists(corePath), $"Core assembly missing: {corePath}");
        var assembly = Assembly.LoadFrom(corePath);
        var type = assembly.GetType("MRC.Core.EnvironmentFence", throwOnError: true)!;
        var evaluate = type.GetMethod("Evaluate", BindingFlags.Public | BindingFlags.Static) ?? throw new InvalidOperationException("EnvironmentFence.Evaluate was not found.");

        Require(IsAuthorized(evaluate, "DOONCHYSCOMPUTI", @"D:\Git_Runners_Main", true), "Correct machine + exact existing root was not authorized.");
        Require(!IsAuthorized(evaluate, "Main-PC", @"D:\Git_Runners_Main", true), "Friendly label was incorrectly accepted as OS machine authority.");
        Require(!IsAuthorized(evaluate, "Other-PC", @"D:\Git_Runners_Main", true), "Wrong machine was authorized.");
        Require(!IsAuthorized(evaluate, "DOONCHYSCOMPUTI", @"D:\Git_Runners_Other", true), "Wrong runner root was authorized.");
        Require(!IsAuthorized(evaluate, "DOONCHYSCOMPUTI", @"D:\Git_Runners_Main", false), "Missing runner root was authorized.");
    }

    private static bool IsAuthorized(MethodInfo evaluate, string machine, string root, bool rootExists)
    {
        var result = evaluate.Invoke(null, new object[] { machine, root, rootExists }) ?? throw new InvalidOperationException("EnvironmentFence.Evaluate returned null.");
        var property = result.GetType().GetProperty("IsAuthorized") ?? throw new InvalidOperationException("Fence result omitted IsAuthorized.");
        return (bool)(property.GetValue(result) ?? false);
    }

    private static void RunnerControlLayering()
    {
        var forbidden = new[] { "Runner.Listener.exe", "Runner.Worker.exe", "run.cmd", "Kill(", "entireProcessTree" };
        var roots = new[] { Path.Combine(RepoRoot, "src", "MRC.Cli"), Path.Combine(RepoRoot, "src", "MRC.Gui"), Path.Combine(RepoRoot, "scripts") };
        foreach (var root in roots.Where(Directory.Exists))
        {
            foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase)))
            {
                var text = File.ReadAllText(file);
                foreach (var marker in forbidden) Require(!text.Contains(marker, StringComparison.OrdinalIgnoreCase), $"Runner-control primitive '{marker}' leaked outside MRC.Core into {Path.GetRelativePath(RepoRoot, file)}.");
            }
        }
    }

    private static void InstallerArchitecture()
    {
        var path = Path.Combine(RepoRoot, "scripts", "install.ps1"); Require(File.Exists(path), "scripts/install.ps1 is missing.");
        var text = File.ReadAllText(path);
        foreach (var marker in new[] { "$env:LOCALAPPDATA", "DOONCHYSCOMPUTI", @"D:\Git_Runners_Main", "versions", "current.version", "bin", "MRC.cmd", "SetEnvironmentVariable", "MRC.ico", "Main Runner Control.lnk" }) Require(text.Contains(marker, StringComparison.OrdinalIgnoreCase), $"Installer is missing required architecture marker: {marker}");
    }

    private static void PackagingSkeleton()
    {
        var packageScript = Path.Combine(RepoRoot, "scripts", "package.ps1"); Require(File.Exists(packageScript), "scripts/package.ps1 is missing.");
        var result = Run("pwsh", "-NoProfile", "-File", packageScript, "-Configuration", "Release"); Require(result.ExitCode == 0, $"Packaging failed.\n{result.Output}");
        var zipPath = Path.Combine(RepoRoot, "artifacts", "MRC-v0.0.13-win-x64.zip");
        var sumsPath = Path.Combine(RepoRoot, "artifacts", "SHA256SUMS.txt");
        Require(File.Exists(zipPath), "v0.0.13 candidate ZIP was not produced."); Require(File.Exists(sumsPath), "SHA256SUMS.txt was not produced.");
        using var archive = ZipFile.OpenRead(zipPath);
        var entries = archive.Entries.Select(entry => entry.FullName.Replace('\\', '/')).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var required in new[] { "install.ps1", "manifest.json", "MRC.ico", "payload/MRC.exe", "payload/MRC.Gui.exe" }) Require(entries.Contains(required), $"Candidate ZIP is missing {required}.");
        var checksumText = File.ReadAllText(sumsPath);
        Require(checksumText.Contains("MRC-v0.0.13-win-x64.zip", StringComparison.Ordinal), "Checksum authority does not name the v0.0.13 candidate ZIP.");
    }

    private static void EnsureCliBuilt() { if (!File.Exists(CliExe())) BuildCli(); }
    private static string CliExe() => Path.Combine(RepoRoot, "src", "MRC.Cli", "bin", "Release", "net10.0", "MRC.exe");
    private static ProcessResult Run(string fileName, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo(fileName) { WorkingDirectory = RepoRoot, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);
        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Could not start {fileName}.");
        var stdout = process.StandardOutput.ReadToEnd(); var stderr = process.StandardError.ReadToEnd(); process.WaitForExit();
        return new ProcessResult(process.ExitCode, stdout + stderr);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static string FindRepoRoot() { var current = new DirectoryInfo(Directory.GetCurrentDirectory()); while (current is not null) { if (File.Exists(Path.Combine(current.FullName, "Auth", "0000_MasterAuth.md"))) return current.FullName; current = current.Parent; } throw new InvalidOperationException("Could not locate MRC repository root."); }
    private sealed record ProcessResult(int ExitCode, string Output);
}
