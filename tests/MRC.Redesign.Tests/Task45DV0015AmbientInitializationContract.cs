using System.Runtime.CompilerServices;

internal static class Task45DV0015AmbientInitializationContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        var root = RepoRoot();
        var codeBehindPath = Path.Combine(root, "src", "MRC.Gui", "Controls", "AmbientBackground.xaml.cs");

        Require(File.Exists(codeBehindPath),
            "F-V15-001: AmbientBackground must have code-behind so its XAML visual tree is initialized at runtime.");

        var codeBehind = File.ReadAllText(codeBehindPath);
        Require(codeBehind.Contains("public partial class AmbientBackground", StringComparison.Ordinal),
            "F-V15-001: AmbientBackground runtime class is missing.");
        Require(codeBehind.Contains("public AmbientBackground()", StringComparison.Ordinal)
                && codeBehind.Contains("InitializeComponent();", StringComparison.Ordinal),
            "F-V15-001: AmbientBackground constructor must call InitializeComponent so the five-layer ambient XAML actually renders.");
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
