using System.Runtime.CompilerServices;

internal static class Task32V014ReferenceRecoveryContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifyResponsiveReferenceLayout();
        VerifyReferenceVisualHierarchy();
        VerifyRunnerControlModules();
        Console.WriteLine("PASS  Task32 v0.0.14 reference-first GUI recovery preserved through v0.0.15 component extraction");
    }

    private static void VerifyResponsiveReferenceLayout()
    {
        var root = Directory.GetCurrentDirectory();
        var code = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "MainWindow.xaml.cs"));
        var dashboard = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "Presentation", "RunnerDashboardViewModel.cs"));
        var responsive = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "Presentation", "RunnerResponsiveLayout.cs"));
        var xaml = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "MainWindow.xaml"));
        var card = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "Controls", "RunnerControlCard.xaml"));

        Require(dashboard.Contains("Math.Clamp(value, 1, 4)", StringComparison.Ordinal),
            "CardColumnCount is not bounded to the approved one-through-four column range.");
        Require(code.Contains("RunnerResponsiveLayout.ColumnCountForWidth(ActualWidth)", StringComparison.Ordinal),
            "MainWindow does not delegate responsive card allocation to the safe-width layout authority.");
        Require(responsive.Contains("MinimumCardSlotWidth", StringComparison.Ordinal)
                && responsive.Contains("ReferenceReplicaMetrics.ColumnCountForWidth(windowWidth)", StringComparison.Ordinal),
            "Safe-width responsive authority no longer delegates to the reference-replica metrics authority.");

        Require(xaml.Contains("<UniformGrid Columns=\"{Binding CardColumnCount}\"", StringComparison.Ordinal),
            "Reference recovery lost adaptive card layout authority.");
        Require(xaml.Contains("VerticalAlignment=\"Top\"", StringComparison.Ordinal),
            "Runner-card layout still stretches cards through the full available height.");
        Require(card.Contains("Height=\"150\"", StringComparison.Ordinal),
            "Current extracted runner cards do not preserve the locked compact 150 px reference height.");
    }

    private static void VerifyReferenceVisualHierarchy()
    {
        var root = Directory.GetCurrentDirectory();
        var xaml = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "MainWindow.xaml"));
        var header = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "Controls", "HeroHeader.xaml"));
        var ambient = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "Controls", "AmbientBackground.xaml"));
        var drawer = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "Controls", "ControlDrawer.xaml"));

        Require(xaml.Contains("x:Key=\"AmberActionBrush\"", StringComparison.Ordinal),
            "Reference-first GUI is missing the warm amber primary-action authority.");
        Require(xaml.Contains("<controls:AmbientBackground Panel.ZIndex=\"0\"", StringComparison.Ordinal)
                && ambient.Contains("x:Name=\"CoolHaze\"", StringComparison.Ordinal)
                && ambient.Contains("x:Name=\"LowerRightAmberBloom\"", StringComparison.Ordinal)
                && ambient.Contains("x:Name=\"OuterVignette\"", StringComparison.Ordinal),
            "Reference-first GUI is missing the dedicated dimensional ambient background hierarchy.");
        Require(xaml.Contains("<controls:HeroHeader", StringComparison.Ordinal)
                && header.Contains("Text=\"MAIN PC • LOCAL-FIRST CONTROL\"", StringComparison.Ordinal),
            "Header is missing the approved local-control context eyebrow.");
        Require(header.Contains("Text=\"MAIN RUNNER CONTROL\"", StringComparison.Ordinal)
                && header.Contains("FontSize=\"52\"", StringComparison.Ordinal),
            "Main application title is not visually dominant enough.");
        Require(header.Contains("x:Name=\"LiveInventoryPill\"", StringComparison.Ordinal)
                && header.Contains("Text=\"{Binding InventoryText, ElementName=Root}\"", StringComparison.Ordinal),
            "Authorization/inventory pill is not expressed as a first-class truthful live state.");
        Require(xaml.Contains("<controls:ControlDrawer x:Name=\"ControlDrawer\"", StringComparison.Ordinal)
                && drawer.Contains("x:Key=\"CounterTileStyle\"", StringComparison.Ordinal)
                && drawer.Contains("FontSize=\"20\"", StringComparison.Ordinal)
                && drawer.Contains("{Binding TotalCount}", StringComparison.Ordinal)
                && drawer.Contains("{Binding IdleCount}", StringComparison.Ordinal)
                && drawer.Contains("{Binding BusyCount}", StringComparison.Ordinal)
                && drawer.Contains("{Binding OffCount}", StringComparison.Ordinal)
                && drawer.Contains("{Binding ErrorCount}", StringComparison.Ordinal)
                && drawer.Contains("{Binding TransitionCount}", StringComparison.Ordinal),
            "Drawer counter modules do not preserve the complete reference hierarchy.");
    }

    private static void VerifyRunnerControlModules()
    {
        var root = Directory.GetCurrentDirectory();
        var windowXaml = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "MainWindow.xaml"));
        var cardXaml = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "Controls", "RunnerControlCard.xaml"));
        var themeXaml = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "Themes", "ReferenceReplica.xaml"));

        Require(windowXaml.Contains("<controls:RunnerControlCard", StringComparison.Ordinal),
            "Reference recovery no longer composes the active extracted runner control module.");
        Require(cardXaml.Contains("x:Name=\"ThreeSlotActionRail\"", StringComparison.Ordinal)
                && cardXaml.Contains("Height=\"46\"", StringComparison.Ordinal),
            "Runner lifecycle controls do not preserve the locked 46 px compact action rail.");
        Require(themeXaml.Contains("x:Key=\"ReplicaPrimaryActionButtonStyle\"", StringComparison.Ordinal)
                && themeXaml.Contains("Background\" Value=\"{StaticResource ReplicaAmberBrush}\"", StringComparison.Ordinal),
            "START is not rendered through the warm reference-replica primary action authority.");
        Require(cardXaml.Contains("Text=\"{Binding Row.RunnerName, ElementName=Root}\"", StringComparison.Ordinal)
                && cardXaml.Contains("FontSize=\"17\"", StringComparison.Ordinal),
            "Runner identity does not have the approved strong compact-card hierarchy.");
        Require(cardXaml.Contains("Height=\"150\"", StringComparison.Ordinal)
                && themeXaml.Contains("x:Key=\"ReplicaCardSurfaceStyle\"", StringComparison.Ordinal),
            "Runner cards do not use the approved compact reference control-module surface.");
        Require(themeXaml.Contains("RunnerState.IDLE", StringComparison.Ordinal)
                && themeXaml.Contains("DropShadowEffect", StringComparison.Ordinal),
            "Healthy runner cards have no restrained active-edge/glow treatment authority.");

        foreach (var required in new[]
        {
            "Value=\"START\"",
            "Value=\"FORCE STOP\"",
            "Content=\"STOP\"",
            "Content=\"RESTART\"",
            "ToolTip=\"DETAILS\"",
            "Row.CanStart",
            "Row.CanStop",
            "Row.CanRestart"
        })
        {
            Require(cardXaml.Contains(required, StringComparison.Ordinal),
                $"Reference recovery lost active runner-control contract marker: {required}");
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
