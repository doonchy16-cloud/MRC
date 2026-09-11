using System.Runtime.CompilerServices;

internal static class Task45EV0015ResponsiveHeroContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        var root = RepoRoot();
        var heroXaml = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "Controls", "HeroHeader.xaml"));
        var heroCode = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "Controls", "HeroHeader.xaml.cs"));
        var mainCode = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "MainWindow.xaml.cs"));

        Require(heroCode.Contains("CompactProperty", StringComparison.Ordinal)
                && heroCode.Contains("public bool Compact", StringComparison.Ordinal),
            "F-V15-003: HeroHeader must expose an explicit compact responsive mode.");

        Require(heroCode.Contains("Compact = ActualWidth <= 960", StringComparison.Ordinal),
            "F-V15-003: HeroHeader must own its minimum-width compact threshold at the 900px evidence viewport.");

        Require(heroXaml.Contains("x:Name=\"HeroIdentityCluster\"", StringComparison.Ordinal)
                && heroXaml.Contains("x:Name=\"HeroTruthCluster\"", StringComparison.Ordinal)
                && heroXaml.Contains("Grid.ColumnSpan", StringComparison.Ordinal)
                && heroXaml.Contains("Grid.Row", StringComparison.Ordinal)
                && heroXaml.Contains("Compact, ElementName=Root", StringComparison.Ordinal),
            "F-V15-003: compact mode must reflow hero identity and truth metadata instead of clipping or globally scaling the window.");

        Require(!mainCode.Contains("RootSurface.LayoutTransform", StringComparison.Ordinal),
            "F-V15-003: responsive hero must not reintroduce whole-window scaling.");
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
