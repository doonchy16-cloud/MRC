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
        var card = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "Controls", "RunnerControlCard.xaml"));
        var theme = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "Themes", "ReferenceReplica.xaml"));

        Require(xaml.Contains("x:Name=\"RunnerCardSurface\"", StringComparison.Ordinal),
            "Command center has no dedicated runner-card surface.");
        Require(!xaml.Contains("x:Name=\"RunnerTableSurface\"", StringComparison.Ordinal),
            "Command center still exposes the superseded table-era runner surface.");
        Require(xaml.Contains("<controls:RunnerControlCard Row=\"{Binding}\"", StringComparison.Ordinal),
            "Command center does not compose the extracted RunnerControlCard.");
        Require(theme.Contains("x:Key=\"ReplicaCardSurfaceStyle\"", StringComparison.Ordinal)
                && theme.Contains("CornerRadius\" Value=\"16\"", StringComparison.Ordinal),
            "Runner cards do not have the reference-replica rounded application surface.");
        Require(card.Contains("Text=\"{Binding Row.RunnerName, ElementName=Root}\"", StringComparison.Ordinal)
                && card.Contains("FontSize=\"17\"", StringComparison.Ordinal),
            "Runner identity is not visually dominant at the compact reference-card scale.");
        Require(card.Contains("Text=\"{Binding Row.StateText, ElementName=Root}\"", StringComparison.Ordinal)
                && card.Contains("ReplicaStateBadgeStyle", StringComparison.Ordinal),
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
            "Command center globally scales the full rigid surface instead of adapting layout.");
    }

    private static void VerifyExplicitCardControls()
    {
        var root = Directory.GetCurrentDirectory();
        var xaml = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "MainWindow.xaml"));
        var card = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "Controls", "RunnerControlCard.xaml"));
        var cardCode = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "Controls", "RunnerControlCard.xaml.cs"));
        var mainCode = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "MainWindow.xaml.cs"));

        Require(card.Contains("x:Name=\"ThreeSlotActionRail\"", StringComparison.Ordinal),
            "Runner card does not expose the locked three-slot lifecycle rail.");
        Require(card.Contains("Value=\"START\"", StringComparison.Ordinal)
                && card.Contains("Row.CanStart", StringComparison.Ordinal)
                && cardCode.Contains("RunnerCardAction.Start", StringComparison.Ordinal),
            "Runner card START is not an explicit state-authorized core control.");
        Require(card.Contains("Content=\"STOP\"", StringComparison.Ordinal)
                && card.Contains("Row.CanStop", StringComparison.Ordinal)
                && cardCode.Contains("RunnerCardAction.Stop", StringComparison.Ordinal),
            "Runner card STOP is not an explicit state-authorized core control.");
        Require(card.Contains("Content=\"RESTART\"", StringComparison.Ordinal)
                && card.Contains("Row.CanRestart", StringComparison.Ordinal)
                && cardCode.Contains("RunnerCardAction.Restart", StringComparison.Ordinal),
            "Runner card RESTART is not an explicit state-authorized core control.");
        Require(card.Contains("x:Name=\"CardDetailsAffordance\"", StringComparison.Ordinal)
                && card.Contains("Row.CanShowDetails", StringComparison.Ordinal)
                && cardCode.Contains("RunnerCardAction.Details", StringComparison.Ordinal),
            "Runner card DETAILS path is missing or not bound to diagnostic authority.");
        Require(card.Contains("Value=\"FORCE STOP\"", StringComparison.Ordinal)
                && cardCode.Contains("RunnerCardAction.ForceStop", StringComparison.Ordinal),
            "BUSY primary slot does not become the explicit FORCE STOP request.");
        Require(card.Contains("Height=\"46\"", StringComparison.Ordinal),
            "Runner card lifecycle controls are missing the locked 46 px reference action height.");
        Require(xaml.Contains("ActionRequested=\"RunnerControlCard_OnActionRequested\"", StringComparison.Ordinal)
                && mainCode.Contains("_operations.Start(e.Row.Runner)", StringComparison.Ordinal)
                && mainCode.Contains("_operations.StopIdle(e.Row.Runner)", StringComparison.Ordinal)
                && mainCode.Contains("_operations.Restart(e.Row.Runner)", StringComparison.Ordinal)
                && mainCode.Contains("ExecuteForceStopAsync(e.Row)", StringComparison.Ordinal),
            "Extracted card actions are not routed through MainWindow operations authority.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
