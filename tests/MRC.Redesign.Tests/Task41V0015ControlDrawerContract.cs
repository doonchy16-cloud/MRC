using System.Runtime.CompilerServices;

internal static class Task41V0015ControlDrawerContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        var root = Directory.GetCurrentDirectory();
        var drawerPath = Path.Combine(root, "src", "MRC.Gui", "Controls", "ControlDrawer.xaml");
        var drawerCodePath = Path.Combine(root, "src", "MRC.Gui", "Controls", "ControlDrawer.xaml.cs");

        Require(File.Exists(drawerPath), "ControlDrawer.xaml is missing.");
        Require(File.Exists(drawerCodePath), "ControlDrawer.xaml.cs is missing.");

        var drawer = File.ReadAllText(drawerPath);
        var drawerCode = File.ReadAllText(drawerCodePath);
        var main = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "MainWindow.xaml"));
        var mainCode = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "MainWindow.xaml.cs"));

        foreach (var marker in new[]
                 {
                     "x:Name=\"SearchBox\"",
                     "Content=\"ALL\"",
                     "Content=\"IDLE\"",
                     "Content=\"BUSY\"",
                     "Content=\"OFF\"",
                     "Content=\"ERROR\"",
                     "x:Name=\"TurnAllOnButton\"",
                     "x:Name=\"TurnAllOffButton\"",
                     "x:Name=\"ManualRefreshButton\"",
                     "TotalCount",
                     "IdleCount",
                     "BusyCount",
                     "OffCount",
                     "ErrorCount",
                     "TransitionCount"
                 })
        {
            Require(drawer.Contains(marker, StringComparison.Ordinal), $"Control drawer marker is missing: {marker}");
        }

        Require(drawerCode.Contains("dashboard.SearchText = SearchBox.Text", StringComparison.Ordinal),
            "Drawer search does not write through the dashboard authority.");
        Require(drawerCode.Contains("dashboard.SelectedFilter = filter", StringComparison.Ordinal),
            "Drawer filters do not write through the dashboard authority.");
        Require(drawerCode.Contains("public event RoutedEventHandler? TurnAllOnRequested", StringComparison.Ordinal)
                && drawerCode.Contains("public event RoutedEventHandler? TurnAllOffRequested", StringComparison.Ordinal)
                && drawerCode.Contains("public event RoutedEventHandler? RefreshRequested", StringComparison.Ordinal),
            "Drawer does not expose the required request-only event boundary.");
        Require(drawerCode.Contains("public void FocusSearch()", StringComparison.Ordinal)
                && drawerCode.Contains("SearchBox.Focus()", StringComparison.Ordinal)
                && drawerCode.Contains("SearchBox.SelectAll()", StringComparison.Ordinal),
            "Drawer search-focus API is missing.");
        Require(!drawerCode.Contains("RunnerOperationsService", StringComparison.Ordinal)
                && !drawerCode.Contains("RunnerEngine", StringComparison.Ordinal)
                && !drawerCode.Contains("ForceStopBusy", StringComparison.Ordinal),
            "ControlDrawer crossed the presentation boundary into lifecycle/runtime control.");

        Require(main.Contains("x:Name=\"ControlDrawerHost\"", StringComparison.Ordinal)
                && main.Contains("x:Name=\"DrawerScrim\"", StringComparison.Ordinal)
                && main.Contains("<controls:ControlDrawer x:Name=\"ControlDrawer\"", StringComparison.Ordinal),
            "MainWindow does not compose the overlay control drawer.");
        foreach (var retiredPrimaryMarker in new[]
                 {
                     "x:Name=\"SearchBox\"",
                     "x:Name=\"FilterPanel\"",
                     "x:Name=\"TurnAllOnButton\"",
                     "x:Name=\"TurnAllOffButton\"",
                     "x:Name=\"ManualRefreshButton\"",
                     "<UniformGrid Grid.Row=\"1\" Columns=\"6\""
                 })
        {
            Require(!main.Contains(retiredPrimaryMarker, StringComparison.Ordinal),
                $"Secondary instrumentation remains permanently on the primary screen: {retiredPrimaryMarker}");
        }

        Require(mainCode.Contains("ControlDrawerHost.Width = Math.Min(400, Math.Max(0, ActualWidth - 32))", StringComparison.Ordinal),
            "Control drawer width does not preserve min(400, window width - 32).");
        Require(mainCode.Contains("OpenControlDrawer(focusSearch: true)", StringComparison.Ordinal),
            "Slash shortcut does not open and focus drawer search.");
        Require(mainCode.Contains("CloseControlDrawer()", StringComparison.Ordinal)
                && mainCode.Contains("RunnerList.Focus()", StringComparison.Ordinal),
            "Escape close/focus-return behavior is missing.");
        Require(mainCode.Contains("HeroHeader_OnMenuRequested", StringComparison.Ordinal)
                && mainCode.Contains("OpenControlDrawer();", StringComparison.Ordinal),
            "Hero menu does not route exclusively into the control drawer.");

        Console.WriteLine("PASS  Task41 v0.0.15 overlay control drawer contract");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
