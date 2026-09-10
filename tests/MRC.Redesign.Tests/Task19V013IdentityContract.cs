using System.Runtime.CompilerServices;
using MRC.Core;

internal static class Task19V013IdentityContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifyHistoricalReleaseAuthority();
        VerifyHistoricalReleasePipeline();
        VerifyPackageCompatibilityAuthority();
        Console.WriteLine("PASS  Task19 v0.0.13 historical pre-cert identity remains immutable evidence");
    }

    private static void VerifyHistoricalReleaseAuthority()
    {
        var version = new Version(0, 0, 13);
        Require(ReleaseAuthority.ChannelFor(version) == "precert",
            "Historical v0.0.13 application channel must remain precert.");
        Require(ReleaseAuthority.StageFor(version) == ReleaseStage.PreCertification,
            "Historical v0.0.13 release stage must remain PreCertification.");
        Require(ReleaseAuthority.ManifestCompatibilityChannelFor(version) == "precert",
            "Historical v0.0.13 manifest transport channel must remain precert.");
        Require(ReleaseAuthority.FinalTargetVersion.ToString() == "0.1.0",
            "Historical v0.0.13 authority must preserve final target 0.1.0.");
    }

    private static void VerifyHistoricalReleasePipeline()
    {
        var root = Directory.GetCurrentDirectory();
        var workflowPath = Path.Combine(root, ".github", "workflows", "precert-v0.0.13-release.yml");
        Require(File.Exists(workflowPath), "Historical v0.0.13 release workflow is missing.");
        var workflow = File.ReadAllText(workflowPath);
        foreach (var marker in new[]
        {
            "branches: [ precert-v0.0.13 ]",
            "- .release/v0.0.13.trigger",
            "MRC-v0.0.13-win-x64.zip",
            "manifest.version -ne '0.0.13'",
            "manifest.channel -ne 'precert'",
            "--prerelease"
        })
        {
            Require(workflow.Contains(marker, StringComparison.Ordinal),
                $"Historical v0.0.13 release evidence is missing '{marker}'.");
        }
    }

    private static void VerifyPackageCompatibilityAuthority()
    {
        var packageScript = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "scripts", "package.ps1"));
        Require(packageScript.Contains("$manifestChannel = if ($version -eq '0.0.12') { 'stable' } else { $channel }", StringComparison.Ordinal),
            "Package authority no longer isolates the legacy stable transport bridge to v0.0.12 only.");
        Require(packageScript.Contains("$finalTarget = '0.1.0'", StringComparison.Ordinal),
            "Package authority does not preserve final target 0.1.0.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
