using System.Runtime.CompilerServices;

internal static class Task56FV0015ReferenceComparisonNormalizationContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        var root = RepoRoot();
        var scriptPath = Path.Combine(root, "scripts", "build-v0.0.15-reference-comparison.ps1");
        var script = File.ReadAllText(scriptPath);

        Require(script.Contains("$ReferenceViewportTop = 176", StringComparison.Ordinal),
            "F-V15-015: comparison must exclude the Owner screenshot browser/remote chrome before visual overlay.");
        Require(script.Contains("$ComparisonHeight = 884", StringComparison.Ordinal),
            "F-V15-015: comparison must stop before the screen-sharing banner/taskbar contamination band.");
        Require(script.Contains("CroppedBitmap", StringComparison.Ordinal),
            "F-V15-015: reference comparison must crop source pixels rather than scale browser chrome into the candidate geometry.");
        Require(script.Contains("[System.Windows.Int32Rect]::new(0, $ReferenceViewportTop, 2048, $ComparisonHeight)", StringComparison.Ordinal),
            "F-V15-015: Owner reference crop must use the locked clean viewport band.");
        Require(script.Contains("[System.Windows.Int32Rect]::new(0, 0, 2048, $ComparisonHeight)", StringComparison.Ordinal),
            "F-V15-015: candidate comparison must use the same native-pixel-height top band without scaling.");
        Require(script.Contains("Save-Visual $sideBySide 4096 1222", StringComparison.Ordinal)
                && script.Contains("Save-Visual $overlay 2048 1222", StringComparison.Ordinal),
            "F-V15-015: normalization must preserve the locked comparison artifact dimensions.");

        Console.WriteLine("PASS  F-V15-015 normalized Owner-viewport comparison evidence contract");
    }

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Auth", "0000_MasterAuth.md")))
                return directory.FullName;
            directory = directory.Parent;
        }
        throw new InvalidOperationException("Could not locate MRC repository root.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
