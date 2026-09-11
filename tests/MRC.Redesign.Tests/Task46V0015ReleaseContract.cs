using System.Runtime.CompilerServices;

internal static class Task46V0015ReleaseContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        var root = Directory.GetCurrentDirectory();
        var path = Path.Combine(root, ".github", "workflows", "precert-v0.0.15-release.yml");
        Require(File.Exists(path), "Task46 v0.0.15 release workflow is missing.");

        var workflow = File.ReadAllText(path);
        foreach (var marker in new[]
        {
            "branches: [ precert-v0.0.15 ]",
            "- .release/v0.0.15.trigger",
            "contents: write",
            "./scripts/verify-redesign.ps1",
            "./scripts/verify-pass4.ps1",
            "./scripts/verify-pass3.ps1",
            "./scripts/verify-pass2.ps1",
            "./scripts/verify-pass1.ps1",
            "./scripts/render-v0.0.15-preview.ps1",
            "./artifacts/v0.0.15/package",
            "MRC-v0.0.15-win-x64.zip",
            "SHA256SUMS.txt",
            "manifest.version -ne '0.0.15'",
            "manifest.channel -ne 'precert'",
            "manifest.applicationChannel -ne 'precert'",
            "manifest.releaseStage -ne 'PreCertification'",
            "manifest.finalTarget -ne '0.1.0'",
            "manifest.targetMachine -ne 'DOONCHYSCOMPUTI'",
            "manifest.runnerRoot -ne 'D:\\Git_Runners_Main'",
            "Version:\\s+0\\.0\\.15",
            "Channel:\\s+precert",
            "Stage:\\s+PRE-CERTIFICATION",
            "Final target:\\s+0\\.1\\.0",
            "MRC-v0.0.15-2048x1222.png",
            "MRC-v0.0.15-1200x760.png",
            "MRC-v0.0.15-1180x760.png",
            "MRC-v0.0.15-900x560.png",
            "MRC-v0.0.15-900x560-busy.png",
            "MRC-v0.0.15-1200x760-drawer.png",
            "MRC-v0.0.15-1200x760-system-drawer.png",
            "MRC-v0.0.15-2048x1222-mixed.png",
            "gh release",
            "v0.0.15",
            "--prerelease",
            "gh release edit v0.0.15 --target \"$env:GITHUB_SHA\"",
            "gh api --method PATCH \"repos/$env:GITHUB_REPOSITORY/git/refs/tags/v0.0.15\"",
            "-f \"sha=$env:GITHUB_SHA\"",
            "-F \"force=false\"",
            "git/ref/tags/v0.0.15",
            "$publishedTarget -ne $env:GITHUB_SHA"
        })
        {
            Require(workflow.Contains(marker, StringComparison.Ordinal),
                $"Task46 v0.0.15 release workflow is missing required marker '{marker}'.");
        }

        Require(workflow.Contains("$actual = (Get-FileHash", StringComparison.Ordinal)
                && workflow.Contains("$actual -ne $expected", StringComparison.Ordinal),
            "Task46 release workflow does not verify package SHA-256 against SHA256SUMS.txt.");
        Require(workflow.Contains("payload\\MRC.exe", StringComparison.Ordinal)
                && workflow.Contains("payload\\MRC.Gui.exe", StringComparison.Ordinal)
                && workflow.Contains("MRC.ico", StringComparison.Ordinal),
            "Task46 release workflow does not verify executable/icon package authority.");
        Require(workflow.Contains("--target \"$env:GITHUB_SHA\"", StringComparison.Ordinal)
                && workflow.Contains("$publishedTarget", StringComparison.Ordinal),
            "Task46 release workflow can publish without proving exact-head provenance.");
        Require(workflow.Contains("-F \"force=false\"", StringComparison.Ordinal)
                && !workflow.Contains("-F \"force=true\"", StringComparison.Ordinal),
            "Task46 v0.0.15 tag movement must fail closed and never force rewrite history.");
        Require(!workflow.Contains("gh release edit v0.0.14", StringComparison.Ordinal)
                && !workflow.Contains("git/refs/tags/v0.0.14", StringComparison.Ordinal),
            "Task46 v0.0.15 workflow must never mutate frozen v0.0.14 release/tag state.");

        Console.WriteLine("PASS  Task46 v0.0.15 exact-head prerelease pipeline contract");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
