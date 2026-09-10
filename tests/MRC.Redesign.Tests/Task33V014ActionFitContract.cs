using System.Runtime.CompilerServices;
using MRC.Gui.Presentation;

internal static class Task33V014ActionFitContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifyCanonicalViewportColumns();
        VerifyActionGeometryIsNotShrunkOrHidden();
        Console.WriteLine("PASS  Task33 v0.0.14 action-fit safety preserved through v0.0.15 compact card");
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
        var cardXaml = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "Controls", "RunnerControlCard.xaml"));
        var cardCode = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "Controls", "RunnerControlCard.xaml.cs"));
        var windowCode = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "MainWindow.xaml.cs"));

        Require(cardXaml.Contains("x:Name=\"ThreeSlotActionRail\"", StringComparison.Ordinal)
                && cardXaml.Contains("Height=\"46\"", StringComparison.Ordinal),
            "Action-fit correction must preserve the locked 46 px three-slot lifecycle rail.");
        Require(cardXaml.Contains("Value=\"FORCE STOP\"", StringComparison.Ordinal)
                && cardXaml.Contains("RunnerState.BUSY", StringComparison.Ordinal)
                && cardCode.Contains("RunnerCardAction.ForceStop", StringComparison.Ordinal),
            "Action-fit correction must preserve explicit BUSY FORCE STOP in the primary slot.");
        Require(windowCode.Contains("case RunnerCardAction.ForceStop", StringComparison.Ordinal)
                && windowCode.Contains("ExecuteForceStopAsync", StringComparison.Ordinal),
            "BUSY FORCE STOP no longer routes through MainWindow's confirmed lifecycle path.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
