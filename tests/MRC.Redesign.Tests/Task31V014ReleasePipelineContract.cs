using System.Runtime.CompilerServices;

internal static class Task31V014ReleasePipelineContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifyReleaseWorkflow();
        Console.WriteLine("PASS  Task31 v0.0.14 release pipeline packages verifies and publishes only the exact precert candidate");
    }

    private static void VerifyReleaseWorkflow()
    {
        var root = Directory.GetCurrentDirectory();
        var path = Path.Combine(root, ".github", "workflows", "precert-v0.0.14-release.yml");
        Require(File.Exists(path), "v0.0.14 release workflow is missing.");

        var workflow = File.ReadAllText(path);
        foreach (var marker in new[]
        {
            "branches: [ precert-v0.0.14 ]",
            "- .release/v0.0.14.trigger",
            "contents: write",
            "./scripts/verify-redesign.ps1",
            "./scripts/verify-pass4.ps1",
            "./scripts/verify-pass3.ps1",
            "./scripts/verify-pass2.ps1",
            "./scripts/verify-pass1.ps1",
            "./scripts/render-v0.0.14-preview.ps1",
            "./artifacts/v0.0.14/package",
            "MRC-v0.0.14-win-x64.zip",
            "SHA256SUMS.txt",
            "manifest.version -ne '0.0.14'",
            "manifest.channel -ne 'precert'",
            "manifest.applicationChannel -ne 'precert'",
            "manifest.releaseStage -ne 'PreCertification'",
            "manifest.finalTarget -ne '0.1.0'",
            "manifest.targetMachine -ne 'DOONCHYSCOMPUTI'",
            "manifest.runnerRoot -ne 'D:\\Git_Runners_Main'",
            "Version:\\s+0\\.0\\.14",
            "Channel:\\s+precert",
            "Stage:\\s+PRE-CERTIFICATION",
            "Final target:\\s+0\\.1\\.0",
            "MRC-v0.0.14-2048x1222.png",
            "MRC-v0.0.14-1200x760.png",
            "MRC-v0.0.14-1180x760.png",
            "MRC-v0.0.14-900x560.png",
            "MRC-v0.0.14-900x560-busy.png",
            "gh release",
            "v0.0.14",
            "--prerelease",
            "gh release edit v0.0.14 --target \"$env:GITHUB_SHA\"",
            "git/ref/tags/v0.0.14",
            "$publishedTarget -ne $env:GITHUB_SHA"
        })
        {
            Require(workflow.Contains(marker, StringComparison.Ordinal),
                $"v0.0.14 release workflow is missing required marker '{marker}'.");
        }

        Require(!workflow.Contains("manifest.channel -ne 'stable'", StringComparison.Ordinal),
            "v0.0.14 release workflow incorrectly expects stable manifest transport channel.");
        Require(!workflow.Contains("applicationChannel -ne 'stable'", StringComparison.Ordinal),
            "v0.0.14 release workflow incorrectly expects stable application channel.");
        Require(workflow.Contains("$actual = (Get-FileHash", StringComparison.Ordinal)
                && workflow.Contains("$actual -ne $expected", StringComparison.Ordinal),
            "v0.0.14 release workflow does not verify the package SHA-256 against SHA256SUMS.txt.");
        Require(workflow.Contains("payload\\MRC.exe", StringComparison.Ordinal)
                && workflow.Contains("payload\\MRC.Gui.exe", StringComparison.Ordinal)
                && workflow.Contains("MRC.ico", StringComparison.Ordinal),
            "v0.0.14 release workflow does not verify executable/icon package authority.");
        Require(workflow.Contains("--target \"$env:GITHUB_SHA\"", StringComparison.Ordinal)
                && workflow.Contains("$publishedTarget", StringComparison.Ordinal),
            "Existing v0.0.14 prerelease can be refreshed without proving that its tag follows the exact candidate HEAD.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
