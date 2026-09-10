using System.Runtime.CompilerServices;

internal static class Task16MinWidthCommandBarContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifyResponsiveCommandBar();
        Console.WriteLine("PASS  Task16 minimum-width application command controls remain complete through overlay drawer");
    }

    private static void VerifyResponsiveCommandBar()
    {
        var root = Directory.GetCurrentDirectory();
        var xaml = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "MainWindow.xaml"));
        var drawer = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "Controls", "ControlDrawer.xaml"));
        var code = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "MainWindow.xaml.cs"));

        Require(!xaml.Contains("<ColumnDefinition Width=\"260\" />", StringComparison.Ordinal),
            "Primary screen still reserves a fixed 260 px search column that can squeeze controls.");
        Require(xaml.Contains("x:Name=\"ControlDrawerHost\"", StringComparison.Ordinal)
                && xaml.Contains("<controls:ControlDrawer x:Name=\"ControlDrawer\"", StringComparison.Ordinal),
            "Secondary command controls are not hosted by the overlay drawer.");
        Require(code.Contains("ControlDrawerHost.Width = Math.Min(400, Math.Max(0, ActualWidth - 32))", StringComparison.Ordinal),
            "Control drawer no longer preserves the safe min(400, window width - 32) width authority.");

        Require(drawer.Contains("<WrapPanel Grid.Row=\"3\"", StringComparison.Ordinal)
                && drawer.Contains("Content=\"ALL\"", StringComparison.Ordinal)
                && drawer.Contains("Content=\"IDLE\"", StringComparison.Ordinal)
                && drawer.Contains("Content=\"BUSY\"", StringComparison.Ordinal)
                && drawer.Contains("Content=\"OFF\"", StringComparison.Ordinal)
                && drawer.Contains("Content=\"ERROR\"", StringComparison.Ordinal),
            "Minimum-width drawer filters do not preserve complete wrapping controls.");
        Require(drawer.Contains("x:Name=\"SearchBox\"", StringComparison.Ordinal)
                && drawer.Contains("x:Name=\"ManualRefreshButton\"", StringComparison.Ordinal),
            "Search or manual refresh control is missing from the responsive drawer.");
        Require(drawer.Contains("<ColumnDefinition Width=\"*\" />", StringComparison.Ordinal)
                && drawer.Contains("x:Name=\"TurnAllOnButton\"", StringComparison.Ordinal)
                && drawer.Contains("x:Name=\"TurnAllOffButton\"", StringComparison.Ordinal),
            "Bulk controls are not retained as complete equal-width drawer actions.");

        Require(drawer.Contains("Content=\"TURN ALL ON\"", StringComparison.Ordinal)
                && drawer.Contains("Content=\"TURN ALL OFF\"", StringComparison.Ordinal),
            "Native bulk command labels must remain complete and explicit.");
        Require(!drawer.Contains("Content=\"[ ALL ON ]\"", StringComparison.Ordinal)
                && !drawer.Contains("Content=\"[ ALL OFF ]\"", StringComparison.Ordinal),
            "Superseded pseudo-terminal bulk button styling remains active.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
