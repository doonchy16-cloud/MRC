using System.Runtime.CompilerServices;
using MRC.Cli;
using MRC.Core;

internal static class Task19V013IdentityContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifyBuildIdentity();
        VerifyReleaseAuthority();
        VerifyCliIdentity();
        VerifyPackageAuthority();
        Console.WriteLine("PASS  Task19 v0.0.13 pre-cert identity is coherent across build, release, CLI, and package authority");
    }

    private static void VerifyBuildIdentity()
    {
        Require(BuildInfo.Version == "0.0.13", $"BuildInfo.Version is '{BuildInfo.Version}', expected 0.0.13.");

        var props = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "Directory.Build.props"));
        foreach (var marker in new[]
        {
            "<Version>0.0.13</Version>",
            "<AssemblyVersion>0.0.13.0</AssemblyVersion>",
            "<FileVersion>0.0.13.0</FileVersion>",
            "<InformationalVersion>0.0.13</InformationalVersion>"
        })
        {
            Require(props.Contains(marker, StringComparison.Ordinal), $"Directory.Build.props is missing '{marker}'.");
        }
    }

    private static void VerifyReleaseAuthority()
    {
        var current = ReleaseAuthority.Current;
        Require(current.Version == "0.0.13", $"ReleaseAuthority.Current.Version is '{current.Version}', expected 0.0.13.");
        Require(current.Channel == "precert", $"Release channel is '{current.Channel}', expected precert.");
        Require(current.Stage == ReleaseStage.PreCertification, $"Release stage is {current.Stage}, expected PreCertification.");
        Require(current.FinalTarget == "0.1.0", $"Final target is '{current.FinalTarget}', expected 0.1.0.");
        Require(ReleaseAuthority.ManifestCompatibilityChannelFor(new Version(0, 0, 13)) == "precert",
            "v0.0.13 manifest transport channel must be precert; only v0.0.12 may use the legacy stable bootstrap bridge.");
    }

    private static void VerifyCliIdentity()
    {
        var lines = CliPresentation.VersionLines(@"C:\MRC\versions\0.0.13");
        Require(lines.Any(line => line.Text == "Version: 0.0.13"),
            "CLI version presentation does not identify 0.0.13.");
        Require(lines.Any(line => line.Text == "Channel: precert"),
            "CLI version presentation does not identify precert channel.");
        Require(lines.Any(line => line.Text == "Stage: PRE-CERTIFICATION"),
            "CLI version presentation does not identify PRE-CERTIFICATION stage.");
        Require(lines.Any(line => line.Text == "Final target: 0.1.0"),
            "CLI version presentation does not preserve final target 0.1.0.");
    }

    private static void VerifyPackageAuthority()
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
