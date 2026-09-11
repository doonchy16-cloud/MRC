using System.Runtime.CompilerServices;

internal static class Task54FV0015SystemDrawerScrollbarChromeContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        var root = Directory.GetCurrentDirectory();
        var app = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "App.xaml"));
        var drawer = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "Controls", "SystemDrawer.xaml"));

        Require(app.Contains("x:Key=\"DarkScrollBarStyle\"", StringComparison.Ordinal)
                && app.Contains("x:Key=\"DarkScrollThumbTemplate\"", StringComparison.Ordinal),
            "F-V15-013: shared dark scrollbar authority is missing.");

        var scrollStart = drawer.IndexOf("<ScrollViewer Grid.Row=\"2\"", StringComparison.Ordinal);
        Require(scrollStart >= 0, "F-V15-013: System Drawer findings ScrollViewer is missing.");
        var scrollEnd = drawer.IndexOf("</ScrollViewer>", scrollStart, StringComparison.Ordinal);
        Require(scrollEnd > scrollStart, "F-V15-013: System Drawer findings ScrollViewer block is malformed.");
        var scrollBlock = drawer[scrollStart..scrollEnd];

        Require(scrollBlock.Contains("<ScrollViewer.Resources>", StringComparison.Ordinal)
                && scrollBlock.Contains("TargetType=\"{x:Type ScrollBar}\"", StringComparison.Ordinal)
                && scrollBlock.Contains("BasedOn=\"{StaticResource DarkScrollBarStyle}\"", StringComparison.Ordinal),
            "F-V15-013: System Drawer findings list must consume DarkScrollBarStyle at its visual resource boundary.");

        Require(scrollBlock.Contains("VerticalScrollBarVisibility=\"Auto\"", StringComparison.Ordinal)
                && scrollBlock.Contains("HorizontalScrollBarVisibility=\"Disabled\"", StringComparison.Ordinal),
            "F-V15-013: System Drawer scrollbar convergence must preserve vertical-auto/horizontal-disabled semantics.");

        Require(!drawer.Contains("START", StringComparison.Ordinal)
                && !drawer.Contains("FORCE STOP", StringComparison.Ordinal),
            "F-V15-013: scrollbar convergence must not introduce runner lifecycle controls into System Drawer.");

        Console.WriteLine("PASS  F-V15-013 System Drawer dark-scrollbar consumption contract");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
