using System.Runtime.CompilerServices;

internal static class Task38V0015StateOrbContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        var root = Directory.GetCurrentDirectory();
        var orbPath = Path.Combine(root, "src", "MRC.Gui", "Controls", "StateOrb.xaml");
        var orbCodePath = Path.Combine(root, "src", "MRC.Gui", "Controls", "StateOrb.xaml.cs");

        Require(File.Exists(orbPath), "StateOrb.xaml is missing.");
        Require(File.Exists(orbCodePath), "StateOrb.xaml.cs is missing.");

        var xaml = File.ReadAllText(orbPath);
        var code = File.ReadAllText(orbCodePath);
        var card = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "Controls", "RunnerControlCard.xaml"));
        var row = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "Presentation", "RunnerRowViewModel.cs"));

        Require(xaml.Contains("Width=\"32\"", StringComparison.Ordinal)
                && xaml.Contains("Height=\"32\"", StringComparison.Ordinal),
            "StateOrb does not preserve the locked 32x32 reference geometry.");
        Require(xaml.Contains("OpacityMask", StringComparison.Ordinal)
                && xaml.Contains("RadialGradientBrush", StringComparison.Ordinal),
            "Orb radial falloff is missing.");
        Require(xaml.Contains("Intensity", StringComparison.Ordinal),
            "Shared intensity binding is missing from StateOrb.");

        Require(code.Contains("DependencyProperty StateProperty", StringComparison.Ordinal)
                && code.Contains("nameof(State)", StringComparison.Ordinal)
                && code.Contains("typeof(RunnerState)", StringComparison.Ordinal),
            "StateOrb State dependency property is missing.");
        Require(code.Contains("DependencyProperty IntensityProperty", StringComparison.Ordinal)
                && code.Contains("nameof(Intensity)", StringComparison.Ordinal)
                && code.Contains("typeof(double)", StringComparison.Ordinal),
            "StateOrb Intensity dependency property is missing.");

        foreach (var color in new[] { "#2AD98B", "#FFD166", "#9AA4AA", "#55AFFF", "#BA92FF", "#FF7B45" })
            Require(xaml.Contains(color, StringComparison.OrdinalIgnoreCase), $"StateOrb color {color} is missing.");

        Require(card.Contains("<controls:StateOrb", StringComparison.Ordinal)
                && card.Contains("Row.State", StringComparison.Ordinal)
                && card.Contains("Row.AnimationIntensity", StringComparison.Ordinal),
            "RunnerControlCard does not bind the orb to truthful state plus shared animation intensity.");

        Require(!row.Contains("DispatcherTimer", StringComparison.Ordinal)
                && !code.Contains("DispatcherTimer", StringComparison.Ordinal)
                && !xaml.Contains("DispatcherTimer", StringComparison.Ordinal)
                && !card.Contains("DispatcherTimer", StringComparison.Ordinal),
            "Per-card or per-orb timer introduced.");

        Console.WriteLine("PASS  Task38 v0.0.15 luminous shared-clock StateOrb contract");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
