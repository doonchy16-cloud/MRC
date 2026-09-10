using System.Runtime.CompilerServices;

internal static class Task32V014ReferenceRecoveryContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifyResponsiveReferenceLayout();
        VerifyReferenceVisualHierarchy();
        VerifyRunnerControlModules();
        Console.WriteLine("PASS  Task32 v0.0.14 reference-first GUI recovery contract");
    }

    private static void VerifyResponsiveReferenceLayout()
    {
        var root = Directory.GetCurrentDirectory();
        var code = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "MainWindow.xaml.cs"));
        var dashboard = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "Presentation", "RunnerDashboardViewModel.cs"));
        var xaml = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "MainWindow.xaml"));

        Require(dashboard.Contains("Math.Clamp(value, 1, 4)", StringComparison.Ordinal),
            "CardColumnCount is still capped at three columns instead of four.");
        Require(code.Contains("ActualWidth >= 1600", StringComparison.Ordinal)
                && code.Contains("? 4", StringComparison.Ordinal)
                && code.Contains("ActualWidth >= 1100", StringComparison.Ordinal)
                && code.Contains("? 3", StringComparison.Ordinal)
                && code.Contains("ActualWidth >= 800", StringComparison.Ordinal)
                && code.Contains("? 2", StringComparison.Ordinal),
            "Responsive layout does not implement the approved 4 / 3 / 2 / 1 column doctrine.");

        Require(xaml.Contains("<UniformGrid Columns=\"{Binding CardColumnCount}\"", StringComparison.Ordinal),
            "Reference recovery lost adaptive card layout authority.");
        Require(xaml.Contains("VerticalAlignment=\"Top\"", StringComparison.Ordinal),
            "Runner-card layout still stretches cards through the full available height.");
        Require(xaml.Contains("MaxHeight=\"210\"", StringComparison.Ordinal)
                || xaml.Contains("Property=\"MaxHeight\" Value=\"210\"", StringComparison.Ordinal),
            "Runner cards have no bounded large-screen height and can become giant empty panels.");
    }

    private static void VerifyReferenceVisualHierarchy()
    {
        var xaml = File.ReadAllText(Path.Combine(
            Directory.GetCurrentDirectory(), "src", "MRC.Gui", "MainWindow.xaml"));

        Require(xaml.Contains("x:Key=\"AmberActionBrush\"", StringComparison.Ordinal),
            "Reference-first GUI is missing the warm amber primary-action authority.");
        Require(xaml.Contains("x:Name=\"AmbientGlowLayer\"", StringComparison.Ordinal),
            "Reference-first GUI is missing the dimensional ambient glow layer.");
        Require(xaml.Contains("Text=\"LOCAL-FIRST RUNNER CONTROL\"", StringComparison.Ordinal),
            "Header is missing the approved local-control context eyebrow.");
        Require(xaml.Contains("Text=\"MAIN RUNNER CONTROL\" FontSize=\"34\"", StringComparison.Ordinal),
            "Main application title is not visually dominant enough.");
        Require(xaml.Contains("Text=\"CONTROL AUTHORIZED\"", StringComparison.Ordinal),
            "Authorization pill is not expressed as a first-class control-ready state.");
        Require(xaml.Contains("x:Key=\"CounterCardStyle\"", StringComparison.Ordinal)
                && xaml.Contains("FontSize=\"24\"", StringComparison.Ordinal),
            "Counter modules remain too visually restrained for the reference hierarchy.");
    }

    private static void VerifyRunnerControlModules()
    {
        var xaml = File.ReadAllText(Path.Combine(
            Directory.GetCurrentDirectory(), "src", "MRC.Gui", "MainWindow.xaml"));

        Require(xaml.Contains("Property=\"MinHeight\" Value=\"44\"", StringComparison.Ordinal)
                || xaml.Contains("MinHeight=\"44\"", StringComparison.Ordinal),
            "Runner lifecycle controls are still too small; 44 px minimum control height is required.");
        Require(xaml.Contains("x:Key=\"StartButtonStyle\"", StringComparison.Ordinal)
                && (xaml.Contains("Background=\"{StaticResource AmberActionBrush}\"", StringComparison.Ordinal)
                    || xaml.Contains("Property=\"Background\" Value=\"{StaticResource AmberActionBrush}\"", StringComparison.Ordinal)),
            "START is not rendered as the warm primary action.");
        Require(xaml.Contains("Text=\"{Binding RunnerName}\"", StringComparison.Ordinal)
                && xaml.Contains("FontSize=\"19\"", StringComparison.Ordinal),
            "Runner identity does not have the approved strong card hierarchy.");
        Require(xaml.Contains("x:Key=\"RunnerCardStyle\"", StringComparison.Ordinal)
                && xaml.Contains("Property=\"MinHeight\" Value=\"168\"", StringComparison.Ordinal),
            "Runner cards do not have the approved substantial control-module height.");
        Require(xaml.Contains("RunnerState.IDLE", StringComparison.Ordinal)
                && xaml.Contains("DropShadowEffect", StringComparison.Ordinal),
            "Healthy runner cards have no restrained active-edge/glow treatment.");

        foreach (var required in new[]
        {
            "Content=\"START\"",
            "Content=\"STOP\"",
            "Content=\"RESTART\"",
            "Content=\"DETAILS\"",
            "IsEnabled=\"{Binding CanStart}\"",
            "IsEnabled=\"{Binding CanStop}\"",
            "IsEnabled=\"{Binding CanRestart}\""
        })
        {
            Require(xaml.Contains(required, StringComparison.Ordinal),
                $"Reference recovery lost functional runner-control contract marker: {required}");
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
