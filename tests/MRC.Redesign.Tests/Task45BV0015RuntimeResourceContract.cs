using System.Runtime.CompilerServices;

internal static class Task45BV0015RuntimeResourceContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        var root = RepoRoot();
        var hero = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "Controls", "HeroHeader.xaml"));
        var card = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "Controls", "RunnerControlCard.xaml"));

        Require(hero.Contains("Foreground=\"{DynamicResource ReplicaHeroWhiteBrush}\"", StringComparison.Ordinal),
            "HeroHeader root must late-bind ReplicaHeroWhiteBrush so its local merged dictionary exists before lookup.");
        Require(card.Contains("Foreground=\"{DynamicResource ReplicaHeroWhiteBrush}\"", StringComparison.Ordinal),
            "RunnerControlCard root must late-bind ReplicaHeroWhiteBrush so its local merged dictionary exists before lookup.");
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
