using System.Runtime.CompilerServices;

internal static class Task53FV0015SystemDrawerEvidenceContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        var root = Directory.GetCurrentDirectory();
        var preview = File.ReadAllText(Path.Combine(root, "tools", "MRC.Pass3.Preview", "Program.cs"));
        var render = File.ReadAllText(Path.Combine(root, "scripts", "render-v0.0.15-preview.ps1"));
        var drawer = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "Controls", "SystemDrawer.xaml"));

        Require(preview.Contains("\"system\"", StringComparison.OrdinalIgnoreCase)
                && preview.Contains("ShowSystemDrawer", StringComparison.Ordinal),
            "F-V15-012: deterministic preview must support opening the System Drawer.");

        Require(render.Contains("MRC-v0.0.15-1200x760-system-drawer.png", StringComparison.Ordinal)
                && render.Contains("-Drawer 'system'", StringComparison.Ordinal),
            "F-V15-012: render evidence matrix is missing the 1200x760 System Drawer frame.");

        Require(drawer.Contains("SystemFindingsSummary", StringComparison.Ordinal)
                && drawer.Contains("ItemsSource=\"{Binding SystemFindings}\"", StringComparison.Ordinal),
            "F-V15-012: System Drawer evidence must remain bound to read-only system findings authority.");
        Require(!drawer.Contains("START", StringComparison.Ordinal)
                && !drawer.Contains("FORCE STOP", StringComparison.Ordinal),
            "F-V15-012: System Drawer must remain free of runner lifecycle controls.");

        Console.WriteLine("PASS  F-V15-012 deterministic System Drawer render-evidence contract");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
