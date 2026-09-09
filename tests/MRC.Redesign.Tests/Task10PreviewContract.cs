using System.Reflection;
using System.Runtime.CompilerServices;
using MRC.Core.Runtime;
using MRC.Gui;

internal static class Task10PreviewContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        var repoRoot = Directory.GetCurrentDirectory();

        var previewMethod = typeof(MainWindow).GetMethod(
            "ConfigurePreview",
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: new[] { typeof(RunnerRuntimeReport) },
            modifiers: null);
        Require(previewMethod is not null,
            "MainWindow.ConfigurePreview(RunnerRuntimeReport) is missing; preview cannot render system findings.");

        var previewSourcePath = Path.Combine(repoRoot, "tools", "MRC.Pass3.Preview", "Program.cs");
        var previewSource = File.ReadAllText(previewSourcePath);
        Require(previewSource.Contains("BuildPreviewReport", StringComparison.Ordinal),
            "Preview renderer does not construct a RunnerRuntimeReport.");
        Require(previewSource.Contains("RunnerSystemFindingKind.External", StringComparison.Ordinal)
                && previewSource.Contains("Lotto", StringComparison.OrdinalIgnoreCase)
                && previewSource.Contains("Session", StringComparison.OrdinalIgnoreCase)
                && previewSource.Contains("0", StringComparison.Ordinal),
            "Preview fixture does not contain an explicit external Lotto Session 0 finding.");
        Require(previewSource.Contains("window.ConfigurePreview(BuildPreviewReport())", StringComparison.Ordinal),
            "Preview renderer does not feed the full runtime report into MainWindow.");

        Console.WriteLine("PASS  Task10A deterministic preview includes external Lotto finding");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
