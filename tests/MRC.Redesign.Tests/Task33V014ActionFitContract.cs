using System.Runtime.CompilerServices;
using MRC.Gui.Presentation;

internal static class Task33V014ActionFitContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifyCanonicalViewportColumns();
        VerifyActionGeometryIsNotShrunkOrHidden();
        Console.WriteLine("PASS  Task33 v0.0.14 canonical action-rail fit contract");
    }

    private static void VerifyCanonicalViewportColumns()
    {
        Require(RunnerResponsiveLayout.MinimumCardSlotWidth >= 470,
            "Responsive card allocation does not reserve enough width for the complete action rail.");

        Require(RunnerResponsiveLayout.ColumnCountForWidth(2048) == 4,
            "2048 px acceptance width must retain the four-column large-screen layout.");
        Require(RunnerResponsiveLayout.ColumnCountForWidth(1200) == 2,
            "1200 px acceptance width must use two columns to keep all actions visible.");
        Require(RunnerResponsiveLayout.ColumnCountForWidth(1180) == 2,
            "1180 px acceptance width must use two columns to keep all actions visible.");
        Require(RunnerResponsiveLayout.ColumnCountForWidth(900) == 1,
            "900 px minimum acceptance width must use one column to keep all actions visible.");

        Require(RunnerResponsiveLayout.ColumnCountForWidth(1600) == 3,
            "Approximately 1600 px should resolve to three safe-width runner cards.");
    }

    private static void VerifyActionGeometryIsNotShrunkOrHidden()
    {
        var root = Directory.GetCurrentDirectory();
        var xaml = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "MainWindow.xaml"));

        Require(xaml.Contains("Property=\"MinHeight\" Value=\"44\"", StringComparison.Ordinal)
                || xaml.Contains("MinHeight=\"44\"", StringComparison.Ordinal),
            "Action-fit correction must preserve the 44 px lifecycle-control height.");
        Require(xaml.Contains("Content=\"FORCE STOP\"", StringComparison.Ordinal)
                && xaml.Contains("RunnerMoreControl_OnClick", StringComparison.Ordinal),
            "Action-fit correction must preserve explicit BUSY FORCE STOP control.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
