using System.Runtime.CompilerServices;

internal static class Task29V014LargeCardCompositionContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifyCurrentCardsUseTheirHeightIntentionally();
        Console.WriteLine("PASS  Task29 v0.0.14 visible card-height intent preserved through v0.0.15 compact component");
    }

    private static void VerifyCurrentCardsUseTheirHeightIntentionally()
    {
        var root = Directory.GetCurrentDirectory();
        var windowXaml = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "MainWindow.xaml"));
        var cardXaml = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "Controls", "RunnerControlCard.xaml"));
        var themeXaml = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "Themes", "ReferenceReplica.xaml"));
        var metrics = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "Presentation", "ReferenceReplicaMetrics.cs"));

        Require(windowXaml.Contains("<controls:RunnerControlCard", StringComparison.Ordinal)
                && windowXaml.Contains("Row=\"{Binding}\"", StringComparison.Ordinal)
                && windowXaml.Contains("ActionRequested=\"RunnerControlCard_OnActionRequested\"", StringComparison.Ordinal),
            "Runner-card command center is not composed through the extracted current card component.");

        Require(metrics.Contains("CardTargetHeight = 150", StringComparison.Ordinal)
                && metrics.Contains("CardGap = 22", StringComparison.Ordinal),
            "Current reference metrics no longer preserve the locked 150 px visible card body and 22 px inter-card gap.");
        Require(themeXaml.Contains("<Setter Property=\"Margin\" Value=\"11\" />", StringComparison.Ordinal),
            "Current card surface no longer preserves the 11 px per-side spacing that produces the 22 px gap.");
        Require(cardXaml.Contains("Height=\"172\"", StringComparison.Ordinal),
            "Current runner-card footprint must be 172 px so the locked 150 px visible card body survives its 11 px top/bottom spacing.");

        Require(cardXaml.Contains("<RowDefinition Height=\"*\" />", StringComparison.Ordinal)
                && cardXaml.Contains("<RowDefinition Height=\"46\" />", StringComparison.Ordinal),
            "Compact runner card does not intentionally allocate flexible identity space above the fixed action rail.");

        Require(cardXaml.Contains("x:Name=\"ThreeSlotActionRail\"", StringComparison.Ordinal)
                && cardXaml.Contains("Grid.Row=\"1\" Height=\"46\"", StringComparison.Ordinal),
            "Compact runner-card action rail is not fixed to the locked 46 px lower row.");
        Require(cardXaml.Contains("<Grid Margin=\"0,0,0,11\">", StringComparison.Ordinal),
            "Compact runner-card identity region lacks deliberate separation from the action rail.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
