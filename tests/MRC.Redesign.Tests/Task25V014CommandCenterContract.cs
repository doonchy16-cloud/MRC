using System.Runtime.CompilerServices;

internal static class Task25V014CommandCenterContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifyCommandCenterSurface();
        VerifyResponsiveCardLayout();
        VerifyExplicitCardControls();
        Console.WriteLine("PASS  Task25 v0.0.14 responsive native command center contract");
    }

    private static void VerifyCommandCenterSurface()
    {
        var root = Directory.GetCurrentDirectory();
        var xaml = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "MainWindow.xaml"));

        Require(xaml.Contains("x:Name=\"RunnerCardSurface\"", StringComparison.Ordinal),
            "V0.0.14 command center has no dedicated runner-card surface.");
        Require(!xaml.Contains("x:Name=\"RunnerTableSurface\"", StringComparison.Ordinal),
            "V0.0.14 still exposes the superseded table-era runner surface.");
        Require(xaml.Contains("x:Key=\"RunnerCardStyle\"", StringComparison.Ordinal),
            "Runner cards do not have a dedicated application-card visual language.");
        Require(xaml.Contains("CornerRadius=\"14\"", StringComparison.Ordinal),
            "Runner cards are missing the approved polished rounded application surface.");
        Require(xaml.Contains("Text=\"{Binding RunnerName}\"", StringComparison.Ordinal)
                && xaml.Contains("FontSize=\"18\"", StringComparison.Ordinal),
            "Runner identity is not visually dominant at a readable card scale.");
        Require(xaml.Contains("Text=\"{Binding StateText}\"", StringComparison.Ordinal)
                && xaml.Contains("x:Key=\"StateBadgeStyle\"", StringComparison.Ordinal),
            "Runner cards do not expose a dedicated state badge.");
    }

    private static void VerifyResponsiveCardLayout()
    {
        var root = Directory.GetCurrentDirectory();
        var xaml = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "MainWindow.xaml"));
        var code = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "MainWindow.xaml.cs"));
        var dashboard = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "Presentation", "RunnerDashboardViewModel.cs"));

        Require(xaml.Contains("MinWidth=\"900\"", StringComparison.Ordinal)
                && xaml.Contains("MinHeight=\"560\"", StringComparison.Ordinal)
                && xaml.Contains("Width=\"1200\"", StringComparison.Ordinal)
                && xaml.Contains("Height=\"760\"", StringComparison.Ordinal),
            "Command Center no longer preserves the approved default/minimum viewports.");
        Require(xaml.Contains("<UniformGrid Columns=\"{Binding CardColumnCount}\"", StringComparison.Ordinal),
            "Runner cards do not use an adaptive multi-column layout.");
        Require(dashboard.Contains("CardColumnCount", StringComparison.Ordinal),
            "Dashboard exposes no card-column layout authority.");
        Require(code.Contains("ApplyResponsiveLayout", StringComparison.Ordinal)
                && code.Contains("_dashboard.CardColumnCount", StringComparison.Ordinal),
            "Window width is not driving the adaptive card-column count.");
        Require(!code.Contains("RootSurface.LayoutTransform", StringComparison.Ordinal),
            "V0.0.14 still globally scales the full rigid surface instead of adapting layout.");
    }

    private static void VerifyExplicitCardControls()
    {
        var xaml = File.ReadAllText(Path.Combine(
            Directory.GetCurrentDirectory(), "src", "MRC.Gui", "MainWindow.xaml"));

        Require(xaml.Contains("Content=\"START\"", StringComparison.Ordinal)
                && xaml.Contains("IsEnabled=\"{Binding CanStart}\"", StringComparison.Ordinal)
                && xaml.Contains("Click=\"RunnerStart_OnClick\"", StringComparison.Ordinal),
            "Runner card START is not an explicit state-authorized core control.");
        Require(xaml.Contains("Content=\"STOP\"", StringComparison.Ordinal)
                && xaml.Contains("IsEnabled=\"{Binding CanStop}\"", StringComparison.Ordinal)
                && xaml.Contains("Click=\"RunnerStop_OnClick\"", StringComparison.Ordinal),
            "Runner card STOP is not an explicit state-authorized core control.");
        Require(xaml.Contains("Content=\"RESTART\"", StringComparison.Ordinal)
                && xaml.Contains("IsEnabled=\"{Binding CanRestart}\"", StringComparison.Ordinal)
                && xaml.Contains("Click=\"RunnerRestart_OnClick\"", StringComparison.Ordinal),
            "Runner card RESTART is not an explicit state-authorized core control.");
        Require(xaml.Contains("Content=\"DETAILS\"", StringComparison.Ordinal)
                && xaml.Contains("IsEnabled=\"{Binding CanShowDetails}\"", StringComparison.Ordinal)
                && xaml.Contains("Click=\"RunnerDetails_OnClick\"", StringComparison.Ordinal),
            "Runner card DETAILS path is missing or not bound to diagnostic authority.");
        Require(xaml.Contains("Property=\"MinHeight\" Value=\"38\"", StringComparison.Ordinal)
                || xaml.Contains("MinHeight=\"38\"", StringComparison.Ordinal),
            "Runner card controls are missing a readable minimum hit-target height.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
