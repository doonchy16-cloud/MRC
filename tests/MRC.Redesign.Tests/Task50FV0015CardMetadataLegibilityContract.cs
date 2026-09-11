using System.Runtime.CompilerServices;

internal static class Task50FV0015CardMetadataLegibilityContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        var root = Directory.GetCurrentDirectory();
        var card = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "Controls", "RunnerControlCard.xaml"));

        var repoMarker = "<Run Text=\"REPO  \"";
        var repoPos = card.IndexOf(repoMarker, StringComparison.Ordinal);
        Require(repoPos >= 0, "F-V15-009: repository metadata line is missing.");
        var repoBlockStart = card.LastIndexOf("<TextBlock", repoPos, StringComparison.Ordinal);
        var repoBlockEnd = card.IndexOf('>', repoBlockStart);
        Require(repoBlockStart >= 0 && repoBlockEnd > repoBlockStart, "F-V15-009: repository metadata TextBlock is malformed.");
        var repoOpening = card[repoBlockStart..repoBlockEnd];
        Require(repoOpening.Contains("FontSize=\"11\"", StringComparison.Ordinal),
            "F-V15-009: repository metadata must use the readable 11 px reference hierarchy.");

        var pathMarker = "Text=\"{Binding Row.DirectoryPath";
        var pathPos = card.IndexOf(pathMarker, StringComparison.Ordinal);
        Require(pathPos >= 0, "F-V15-009: runner path line is missing.");
        var pathBlockStart = card.LastIndexOf("<TextBlock", pathPos, StringComparison.Ordinal);
        var pathBlockEnd = card.IndexOf(" />", pathPos, StringComparison.Ordinal);
        Require(pathBlockStart >= 0 && pathBlockEnd > pathBlockStart, "F-V15-009: runner path TextBlock is malformed.");
        var pathBlock = card[pathBlockStart..pathBlockEnd];
        Require(pathBlock.Contains("FontSize=\"10\"", StringComparison.Ordinal),
            "F-V15-009: runner path must not remain microtext below 10 px.");
        Require(pathBlock.Contains("Foreground=\"#78848A\"", StringComparison.Ordinal),
            "F-V15-009: runner path must use the higher-contrast cool-neutral foreground.");

        Require(card.Contains("FontSize=\"17\"", StringComparison.Ordinal)
                && card.Contains("Height=\"172\"", StringComparison.Ordinal)
                && card.Contains("x:Name=\"ThreeSlotActionRail\" Grid.Row=\"1\" Height=\"46\"", StringComparison.Ordinal),
            "F-V15-009: metadata legibility must not change locked runner-name, card-footprint, or action-rail geometry.");

        Console.WriteLine("PASS  F-V15-009 card metadata legibility contract");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
