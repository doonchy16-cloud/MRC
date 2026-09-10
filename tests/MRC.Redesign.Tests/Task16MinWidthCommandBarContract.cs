using System.Runtime.CompilerServices;

internal static class Task16MinWidthCommandBarContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifyResponsiveCommandBar();
        Console.WriteLine("PASS  Task16 minimum-width application command bar preserves complete controls");
    }

    private static void VerifyResponsiveCommandBar()
    {
        var xaml = File.ReadAllText(Path.Combine(
            Directory.GetCurrentDirectory(), "src", "MRC.Gui", "MainWindow.xaml"));

        Require(!xaml.Contains("<ColumnDefinition Width=\"260\" />", StringComparison.Ordinal),
            "Command bar still reserves a fixed 260 px search column that can squeeze bulk controls.");
        Require(xaml.Contains(
                "<Grid.ColumnDefinitions><ColumnDefinition Width=\"Auto\" /><ColumnDefinition Width=\"Auto\" /><ColumnDefinition Width=\"*\" MinWidth=\"160\" /><ColumnDefinition Width=\"Auto\" /></Grid.ColumnDefinitions>",
                StringComparison.Ordinal),
            "Command bar does not use Auto/Auto/responsive-search/Auto column allocation.");
        Require(xaml.Contains("Grid.Column=\"1\" Orientation=\"Horizontal\"", StringComparison.Ordinal),
            "Bulk control group is not isolated in its own Auto-sized column.");
        Require(xaml.Contains("Grid.Column=\"2\"", StringComparison.Ordinal)
                && xaml.Contains("x:Name=\"SearchBox\"", StringComparison.Ordinal),
            "Search field is not placed in the responsive command-bar region.");
        Require(xaml.Contains("Grid.Column=\"3\" x:Name=\"ManualRefreshButton\"", StringComparison.Ordinal),
            "Refresh control is not isolated in the final Auto-sized column.");

        Require(xaml.Contains("Content=\"TURN ALL ON\"", StringComparison.Ordinal)
                && xaml.Contains("Content=\"TURN ALL OFF\"", StringComparison.Ordinal),
            "Native bulk command labels must remain complete and explicit.");
        Require(!xaml.Contains("Content=\"[ ALL ON ]\"", StringComparison.Ordinal)
                && !xaml.Contains("Content=\"[ ALL OFF ]\"", StringComparison.Ordinal),
            "Superseded pseudo-terminal bulk button styling remains active.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
