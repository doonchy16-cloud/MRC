using System.Runtime.CompilerServices;

internal static class Task45CV0015ReadOnlyBindingContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        var root = RepoRoot();
        var card = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "Controls", "RunnerControlCard.xaml"));

        Require(card.Contains("Text=\"{Binding Row.RepositoryName, ElementName=Root, Mode=OneWay}\"", StringComparison.Ordinal),
            "RunnerControlCard RepositoryName display binding must be explicitly OneWay because RepositoryName is read-only.");
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
