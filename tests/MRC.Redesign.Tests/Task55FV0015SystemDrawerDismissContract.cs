using System.Runtime.CompilerServices;

internal static class Task55FV0015SystemDrawerDismissContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        var root = Directory.GetCurrentDirectory();
        var drawerXaml = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "Controls", "SystemDrawer.xaml"));
        var drawerCode = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "Controls", "SystemDrawer.xaml.cs"));
        var windowXaml = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "MainWindow.xaml"));
        var windowCode = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "MainWindow.xaml.cs"));

        Require(drawerXaml.Contains("Background=\"#FF0B1013\"", StringComparison.Ordinal),
            "F-V15-014: expanded System Drawer must be visually opaque so the collapsed summary cannot ghost through it.");
        Require(drawerXaml.Contains("x:Name=\"CloseButton\"", StringComparison.Ordinal)
                && drawerXaml.Contains("Click=\"CloseButton_OnClick\"", StringComparison.Ordinal),
            "F-V15-014: expanded System Drawer is missing an explicit close affordance.");

        Require(drawerCode.Contains("event EventHandler? CloseRequested", StringComparison.Ordinal)
                && drawerCode.Contains("CloseRequested?.Invoke(this, EventArgs.Empty)", StringComparison.Ordinal),
            "F-V15-014: System Drawer close affordance does not raise a presentation-only close request.");

        Require(windowXaml.Contains("CloseRequested=\"SystemDrawer_OnCloseRequested\"", StringComparison.Ordinal),
            "F-V15-014: MainWindow does not consume the System Drawer close request.");
        Require(windowCode.Contains("private void CloseSystemDrawer()", StringComparison.Ordinal)
                && windowCode.Contains("SystemFindingsPanel.IsChecked = false;", StringComparison.Ordinal)
                && windowCode.Contains("SystemDrawer_OnCloseRequested", StringComparison.Ordinal),
            "F-V15-014: MainWindow does not deterministically close the System Drawer through the summary toggle authority.");
        Require(windowCode.Contains("SystemFindingsPanel.IsChecked == true", StringComparison.Ordinal)
                && windowCode.Contains("CloseSystemDrawer();", StringComparison.Ordinal),
            "F-V15-014: Escape does not provide a keyboard dismiss path for the System Drawer.");

        Require(!drawerXaml.Contains("START", StringComparison.Ordinal)
                && !drawerXaml.Contains("FORCE STOP", StringComparison.Ordinal)
                && !drawerXaml.Contains("RESTART", StringComparison.Ordinal)
                && !drawerCode.Contains("RunnerOperationsService", StringComparison.Ordinal)
                && !drawerCode.Contains("RunnerEngine", StringComparison.Ordinal),
            "F-V15-014: dismiss convergence must preserve the System Drawer's read-only presentation boundary.");

        Console.WriteLine("PASS  F-V15-014 System Drawer dismiss + opaque-overlay contract");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
