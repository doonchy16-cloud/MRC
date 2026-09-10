using System.Runtime.CompilerServices;

internal static class Task29V014LargeCardCompositionContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifyCurrentCardsUseTheirHeightIntentionally();
        Console.WriteLine("PASS  Task29 v0.0.14 card-height intent preserved through v0.0.15 compact component");
    }

    private static void VerifyCurrentCardsUseTheirHeightIntentionally()
    {
        var root = Directory.GetCurrentDirectory();
        var windowXaml = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "MainWindow.xaml"));
        var cardXaml = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "Controls", "RunnerControlCard.xaml"));

        Require(windowXaml.Contains("<controls:RunnerControlCard", StringComparison.Ordinal)
                && windowXaml.Contains("Row=\"{Binding}\"", StringComparison.Ordinal)
                && windowXaml.Contains("ActionRequested=\"RunnerControlCard_OnActionRequested\"", StringComparison.Ordinal),
            "Runner-card command center is not composed through the extracted current card component.");

        Require(cardXaml.Contains("Height=\"150\"", StringComparison.Ordinal),
            "Current runner card does not preserve the locked compact 150 px reference height.");
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
