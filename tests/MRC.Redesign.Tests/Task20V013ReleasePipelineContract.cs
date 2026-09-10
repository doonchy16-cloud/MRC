using System.Runtime.CompilerServices;

internal static class Task20V013ReleasePipelineContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        var root = Directory.GetCurrentDirectory();
        var workflowPath = Path.Combine(root, ".github", "workflows", "precert-v0.0.13-release.yml");
        Require(File.Exists(workflowPath), "v0.0.13 release workflow is missing.");

        var workflow = File.ReadAllText(workflowPath);
        foreach (var marker in new[]
        {
            "branches: [ precert-v0.0.13 ]",
            "- .release/v0.0.13.trigger",
            "contents: write",
            "./scripts/verify-redesign.ps1",
            "./scripts/verify-pass4.ps1",
            "./scripts/verify-pass3.ps1",
            "./scripts/verify-pass2.ps1",
            "./scripts/verify-pass1.ps1",
            "./scripts/render-v0.0.13-preview.ps1",
            "./scripts/package.ps1 -Configuration Release -Runtime win-x64 -ArtifactsRoot ./artifacts/v0.0.13/package",
            "MRC-v0.0.13-win-x64.zip",
            "SHA256SUMS.txt",
            "manifest.version -ne '0.0.13'",
            "manifest.channel -ne 'precert'",
            "manifest.applicationChannel -ne 'precert'",
            "manifest.releaseStage -ne 'PreCertification'",
            "manifest.finalTarget -ne '0.1.0'",
            "manifest.targetMachine -ne 'DOONCHYSCOMPUTI'",
            "manifest.runnerRoot -ne 'D:\\Git_Runners_Main'",
            "MRC-v0.0.13-1200x760.png",
            "MRC-v0.0.13-1180x760.png",
            "MRC-v0.0.13-900x560.png",
            "MRC-v0.0.13-2048x1222.png",
            "gh release",
            "v0.0.13",
            "--prerelease"
        })
        {
            Require(workflow.Contains(marker, StringComparison.Ordinal),
                $"v0.0.13 release workflow is missing required marker: {marker}");
        }

        Require(!workflow.Contains("manifest.channel -ne 'stable'", StringComparison.Ordinal),
            "v0.0.13 release workflow incorrectly expects the v0.0.12 legacy stable transport bridge.");

        Console.WriteLine("PASS  Task20 v0.0.13 release pipeline packages, verifies, and publishes only the precert candidate");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
