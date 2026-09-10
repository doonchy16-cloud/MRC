using System.Runtime.CompilerServices;

internal static class Task29V014LargeCardCompositionContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifyLargeCardsUseTheirHeightIntentionally();
        Console.WriteLine("PASS  Task29 v0.0.14 large-screen card composition");
    }

    private static void VerifyLargeCardsUseTheirHeightIntentionally()
    {
        var xaml = File.ReadAllText(Path.Combine(
            Directory.GetCurrentDirectory(), "src", "MRC.Gui", "MainWindow.xaml"));

        var cardTemplateStart = xaml.IndexOf("<Border Style=\"{StaticResource RunnerCardStyle}\"", StringComparison.Ordinal);
        Require(cardTemplateStart >= 0, "Runner card template is missing.");
        var cardTemplateEnd = xaml.IndexOf("</DataTemplate>", cardTemplateStart, StringComparison.Ordinal);
        Require(cardTemplateEnd > cardTemplateStart, "Runner card template could not be isolated.");
        var cardTemplate = xaml[cardTemplateStart..cardTemplateEnd];

        Require(cardTemplate.Contains("<RowDefinition Height=\"Auto\" />", StringComparison.Ordinal)
                && cardTemplate.Contains("<RowDefinition Height=\"*\" />", StringComparison.Ordinal),
            "Large runner cards still use only Auto rows, leaving stretched card height as dead space.");

        var actionsStart = cardTemplate.IndexOf("<Grid Grid.Row=\"1\"", StringComparison.Ordinal);
        Require(actionsStart >= 0, "Runner-card action rail is missing.");
        var actionsEnd = cardTemplate.IndexOf(">", actionsStart, StringComparison.Ordinal);
        Require(actionsEnd > actionsStart, "Runner-card action rail opening tag could not be isolated.");
        var actionOpeningTag = cardTemplate[actionsStart..(actionsEnd + 1)];
        Require(actionOpeningTag.Contains("VerticalAlignment=\"Bottom\"", StringComparison.Ordinal),
            "Runner-card action rail is not anchored to the bottom of flexible card height.");

        Require(cardTemplate.Contains("Margin=\"0,14,0,0\"", StringComparison.Ordinal),
            "Runner-card action rail lacks deliberate separation from identity content.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
