using System.Runtime.CompilerServices;

internal static class Task46FV0015AmbientBloomConvergenceContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        var root = Directory.GetCurrentDirectory();
        var ambient = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "Controls", "AmbientBackground.xaml"));

        var lowerRight = OpeningTag(ambient, "LowerRightAmberBloom");
        Require(lowerRight.Contains("Width=\"980\"", StringComparison.Ordinal)
                && lowerRight.Contains("Height=\"650\"", StringComparison.Ordinal)
                && lowerRight.Contains("Margin=\"0,0,-260,-245\"", StringComparison.Ordinal),
            "F-V15-005: preserve the proven lower-right amber anchor geometry required by F-V15-002 pixel evidence.");

        var underCards = OpeningTag(ambient, "UnderCardWarmth");
        Require(underCards.Contains("Width=\"1200\"", StringComparison.Ordinal),
            "F-V15-005: under-card warmth must span enough of the canonical runner field.");
        Require(underCards.Contains("Height=\"520\"", StringComparison.Ordinal),
            "F-V15-005: under-card warmth must have enough vertical falloff to carry depth beneath the grid.");
        Require(underCards.Contains("HorizontalAlignment=\"Right\"", StringComparison.Ordinal)
                && underCards.Contains("VerticalAlignment=\"Top\"", StringComparison.Ordinal)
                && underCards.Contains("Margin=\"0,520,-100,0\"", StringComparison.Ordinal),
            "F-V15-005: under-card warmth must rise beneath the canonical grid without moving the lower-right anchor.");

        Require(ambient.Contains("x:Name=\"CoolHaze\"", StringComparison.Ordinal)
                && ambient.Contains("x:Name=\"OuterVignette\"", StringComparison.Ordinal),
            "F-V15-005: ambient convergence must preserve the approved five-layer composition.");
        Require(!ambient.Contains("DispatcherTimer", StringComparison.Ordinal)
                && !ambient.Contains("{Binding", StringComparison.Ordinal),
            "F-V15-005: ambient background must remain static, native, and behavior-free.");

        Console.WriteLine("PASS  F-V15-005 preserved amber anchor + lifted under-card warmth contract");
    }

    private static string OpeningTag(string xaml, string name)
    {
        var marker = $"x:Name=\"{name}\"";
        var start = xaml.IndexOf(marker, StringComparison.Ordinal);
        Require(start >= 0, $"F-V15-005: ambient layer {name} is missing.");
        var end = xaml.IndexOf('>', start);
        Require(end > start, $"F-V15-005: ambient layer {name} opening tag is malformed.");
        return xaml[start..end];
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
