using System.Runtime.CompilerServices;

internal static class Task45FV0015CardVisibleHeightContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        var root = Directory.GetCurrentDirectory();
        var card = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "Controls", "RunnerControlCard.xaml"));
        var theme = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "Themes", "ReferenceReplica.xaml"));
        var metrics = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "Presentation", "ReferenceReplicaMetrics.cs"));

        Require(metrics.Contains("CardTargetHeight = 150", StringComparison.Ordinal),
            "F-V15-004: locked visible card-height authority must remain 150 px.");
        Require(metrics.Contains("CardGap = 22", StringComparison.Ordinal),
            "F-V15-004: locked inter-card gap authority must remain 22 px.");
        Require(theme.Contains("<Setter Property=\"Margin\" Value=\"11\" />", StringComparison.Ordinal),
            "F-V15-004: card surface must retain 11 px per-side spacing that produces the 22 px gap.");
        Require(card.Contains("Height=\"172\"", StringComparison.Ordinal),
            "F-V15-004: runner-card layout footprint must be 172 px so the 11 px top/bottom spacing does not steal from the locked 150 px visible card body.");
        Require(card.Contains("Padding=\"19\"", StringComparison.Ordinal)
                && card.Contains("Grid.Row=\"1\" Height=\"46\"", StringComparison.Ordinal),
            "F-V15-004: restoring visible card height must preserve 19 px inner padding and the 46 px lifecycle rail.");

        Console.WriteLine("PASS  F-V15-004 visible 150 px card body + 22 px inter-card gap contract");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
