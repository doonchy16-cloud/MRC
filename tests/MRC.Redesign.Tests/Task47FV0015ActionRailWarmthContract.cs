using System.Runtime.CompilerServices;

internal static class Task47FV0015ActionRailWarmthContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        var root = Directory.GetCurrentDirectory();
        var theme = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "Themes", "ReferenceReplica.xaml"));
        var card = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "Controls", "RunnerControlCard.xaml"));

        var secondary = StyleBlock(theme, "ReplicaSecondaryActionButtonStyle");
        Require(secondary.Contains("BorderBrush\" Value=\"#6E5A2C\"", StringComparison.Ordinal),
            "F-V15-006: secondary lifecycle controls must use the restrained warm-gold reference outline instead of cool steel gray.");
        Require(secondary.Contains("Background\" Value=\"#0E1518\"", StringComparison.Ordinal),
            "F-V15-006: warm secondary outline must retain the dark secondary button body.");

        var baseStyle = StyleBlock(theme, "ReplicaActionButtonBaseStyle");
        Require(baseStyle.Contains("Property=\"IsEnabled\" Value=\"False\"", StringComparison.Ordinal)
                && baseStyle.Contains("Property=\"Opacity\" Value=\"0.30\"", StringComparison.Ordinal),
            "F-V15-006: disabled lifecycle controls must retain the existing 0.30 dimming treatment.");

        Require(card.Contains("Content=\"STOP\"", StringComparison.Ordinal)
                && card.Contains("Content=\"RESTART\"", StringComparison.Ordinal)
                && Count(card, "Style=\"{StaticResource ReplicaSecondaryActionButtonStyle}\"") >= 2,
            "F-V15-006: STOP and RESTART must continue to consume the shared secondary lifecycle style.");

        Console.WriteLine("PASS  F-V15-006 warm secondary action-rail outline contract");
    }

    private static string StyleBlock(string xaml, string key)
    {
        var marker = $"x:Key=\"{key}\"";
        var markerIndex = xaml.IndexOf(marker, StringComparison.Ordinal);
        Require(markerIndex >= 0, $"F-V15-006: style {key} is missing.");
        var start = xaml.LastIndexOf("<Style", markerIndex, StringComparison.Ordinal);
        var end = xaml.IndexOf("</Style>", markerIndex, StringComparison.Ordinal);
        Require(start >= 0 && end > markerIndex, $"F-V15-006: style {key} is malformed.");
        return xaml[start..(end + "</Style>".Length)];
    }

    private static int Count(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }
        return count;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
