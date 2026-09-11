using System.Runtime.CompilerServices;

internal static class Task40V0015AmbientBackgroundContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        var root = Directory.GetCurrentDirectory();
        var backgroundPath = Path.Combine(root, "src", "MRC.Gui", "Controls", "AmbientBackground.xaml");
        var backgroundCodePath = Path.Combine(root, "src", "MRC.Gui", "Controls", "AmbientBackground.xaml.cs");

        Require(File.Exists(backgroundPath), "AmbientBackground.xaml is missing.");
        Require(File.Exists(backgroundCodePath),
            "AmbientBackground requires constructor-only code-behind so its XAML visual tree initializes at runtime.");

        var xaml = File.ReadAllText(backgroundPath);
        var codeBehind = File.ReadAllText(backgroundCodePath);
        var main = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "MainWindow.xaml"));

        Require(codeBehind.Contains("public partial class AmbientBackground", StringComparison.Ordinal)
                && codeBehind.Contains("public AmbientBackground()", StringComparison.Ordinal)
                && codeBehind.Contains("InitializeComponent();", StringComparison.Ordinal),
            "AmbientBackground code-behind must be limited to WPF visual-tree initialization.");
        Require(!codeBehind.Contains("DispatcherTimer", StringComparison.Ordinal)
                && !codeBehind.Contains("RunnerEngine", StringComparison.Ordinal)
                && !codeBehind.Contains("RunnerOperationsService", StringComparison.Ordinal)
                && !codeBehind.Contains("DataContext", StringComparison.Ordinal)
                && !codeBehind.Contains("Click", StringComparison.Ordinal),
            "AmbientBackground code-behind gained behavior, state, or lifecycle ownership.");

        foreach (var layer in new[]
                 {
                     "NearBlackBase",
                     "CoolHaze",
                     "LowerRightAmberBloom",
                     "UnderCardWarmth",
                     "OuterVignette"
                 })
        {
            Require(xaml.Contains($"x:Name=\"{layer}\"", StringComparison.Ordinal),
                $"Ambient background layer {layer} is missing.");
        }

        Require(xaml.Contains("IsHitTestVisible=\"False\"", StringComparison.Ordinal),
            "Ambient background must be non-interactive.");
        Require(xaml.Contains("RadialGradientBrush", StringComparison.Ordinal),
            "Ambient background is missing radial light falloff.");
        Require(xaml.Contains("GradientStop", StringComparison.Ordinal),
            "Ambient background gradient structure is missing.");
        Require(!xaml.Contains("{Binding", StringComparison.Ordinal)
                && !xaml.Contains("DataContext", StringComparison.Ordinal),
            "Ambient background must not depend on runtime DataContext or bindings.");
        Require(!xaml.Contains("Click=", StringComparison.Ordinal)
                && !xaml.Contains("DispatcherTimer", StringComparison.Ordinal),
            "Ambient background gained behavior or timer ownership.");

        Require(main.Contains("<controls:AmbientBackground Panel.ZIndex=\"0\"", StringComparison.Ordinal),
            "MainWindow does not compose AmbientBackground as the bottom-most visual layer.");
        Require(main.Contains("Panel.ZIndex=\"1\"", StringComparison.Ordinal),
            "Interactive MainWindow content is not explicitly layered above AmbientBackground.");
        Require(!main.Contains("x:Name=\"AmbientGlowLayer\"", StringComparison.Ordinal),
            "Legacy inline ambient-glow layer remains active after AmbientBackground extraction.");

        Console.WriteLine("PASS  Task40 v0.0.15 ambient five-layer background contract");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
