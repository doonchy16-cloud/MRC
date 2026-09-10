using System.Runtime.CompilerServices;

internal static class Task37V0015RunnerControlCardContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        var root = Directory.GetCurrentDirectory();
        var xaml = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "Controls", "RunnerControlCard.xaml"));
        Require(xaml.Contains("x:Name=\"ThreeSlotActionRail\"", StringComparison.Ordinal), "Three-slot rail missing.");
        Require(xaml.Contains("Height=\"46\"", StringComparison.Ordinal), "46 px action height missing.");
        Require(xaml.Contains("x:Name=\"CardDetailsAffordance\"", StringComparison.Ordinal), "Secondary DETAILS affordance missing.");
        Require(xaml.Contains("Value=\"FORCE STOP\"", StringComparison.Ordinal), "BUSY FORCE STOP primary slot missing.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
