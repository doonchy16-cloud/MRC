using System.Runtime.CompilerServices;

internal static class Task48FV0015MenuChromeContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        var root = Directory.GetCurrentDirectory();
        var header = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "Controls", "HeroHeader.xaml"));

        var menu = ElementBlock(header, "MenuButton", "</Button>");
        Require(menu.Contains("Width=\"58\"", StringComparison.Ordinal)
                && menu.Contains("Height=\"58\"", StringComparison.Ordinal)
                && menu.Contains("CornerRadius=\"15\"", StringComparison.Ordinal),
            "F-V15-007: menu button must preserve the approved compact geometry and rounded-square silhouette.");
        Require(menu.Contains("Foreground=\"{StaticResource ReplicaAmberBrush}\"", StringComparison.Ordinal),
            "F-V15-007: hamburger glyph must use the reference amber foreground.");
        Require(menu.Contains("Background=\"#0E1518\"", StringComparison.Ordinal),
            "F-V15-007: menu surface must be dark instead of a solid amber tile.");
        Require(menu.Contains("BorderBrush=\"#6E5A2C\"", StringComparison.Ordinal),
            "F-V15-007: menu control must use a restrained amber rim.");
        Require(menu.Contains("DropShadowEffect Color=\"#FCC34C\"", StringComparison.Ordinal)
                && menu.Contains("BlurRadius=\"14\"", StringComparison.Ordinal)
                && menu.Contains("Opacity=\"0.16\"", StringComparison.Ordinal),
            "F-V15-007: menu rim needs a restrained native amber halo matching the reference control anchor.");
        Require(menu.Contains("AutomationProperties.Name=\"Open runner control drawer\"", StringComparison.Ordinal)
                && menu.Contains("Click=\"Menu_OnClick\"", StringComparison.Ordinal),
            "F-V15-007: menu visual convergence must not alter accessibility or drawer routing.");

        Console.WriteLine("PASS  F-V15-007 dark amber-rimmed menu chrome contract");
    }

    private static string ElementBlock(string xaml, string name, string terminator)
    {
        var marker = $"x:Name=\"{name}\"";
        var markerIndex = xaml.IndexOf(marker, StringComparison.Ordinal);
        Require(markerIndex >= 0, $"F-V15-007: element {name} is missing.");
        var start = xaml.LastIndexOf("<Button", markerIndex, StringComparison.Ordinal);
        var end = xaml.IndexOf(terminator, markerIndex, StringComparison.Ordinal);
        Require(start >= 0 && end > markerIndex, $"F-V15-007: element {name} is malformed.");
        return xaml[start..(end + terminator.Length)];
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
