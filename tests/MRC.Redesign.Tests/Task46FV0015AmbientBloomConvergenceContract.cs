using System.Runtime.CompilerServices;

internal static class Task46FV0015AmbientBloomConvergenceContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        var root = Directory.GetCurrentDirectory();
        var ambient = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "Controls", "AmbientBackground.xaml"));

        var marker = "x:Name=\"LowerRightAmberBloom\"";
        var start = ambient.IndexOf(marker, StringComparison.Ordinal);
        Require(start >= 0, "F-V15-005: lower-right amber bloom layer is missing.");

        var end = ambient.IndexOf('>', start);
        Require(end > start, "F-V15-005: lower-right amber bloom opening tag is malformed.");
        var openingTag = ambient[start..end];

        Require(openingTag.Contains("Width=\"1180\"", StringComparison.Ordinal),
            "F-V15-005: amber bloom must be wide enough to carry reference warmth beneath the runner field.");
        Require(openingTag.Contains("Height=\"760\"", StringComparison.Ordinal),
            "F-V15-005: amber bloom must be tall enough to rise into the canonical runner field.");
        Require(openingTag.Contains("Margin=\"0,0,-210,-80\"", StringComparison.Ordinal),
            "F-V15-005: amber bloom must not remain buried below the bottom edge.");

        Require(ambient.Contains("x:Name=\"CoolHaze\"", StringComparison.Ordinal)
                && ambient.Contains("x:Name=\"UnderCardWarmth\"", StringComparison.Ordinal)
                && ambient.Contains("x:Name=\"OuterVignette\"", StringComparison.Ordinal),
            "F-V15-005: ambient convergence must preserve the approved five-layer composition.");
        Require(!ambient.Contains("DispatcherTimer", StringComparison.Ordinal)
                && !ambient.Contains("{Binding", StringComparison.Ordinal),
            "F-V15-005: ambient background must remain static, native, and behavior-free.");

        Console.WriteLine("PASS  F-V15-005 lower-right amber bloom convergence contract");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
