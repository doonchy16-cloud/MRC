using System.Runtime.CompilerServices;

internal static class Task15V013PreviewContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifyCurrentPreviewPipeline();
        Console.WriteLine("PASS  Task15 v0.0.13 current GUI preview evidence pipeline");
    }

    private static void VerifyCurrentPreviewPipeline()
    {
        var root = Directory.GetCurrentDirectory();
        var scriptPath = Path.Combine(root, "scripts", "render-v0.0.13-preview.ps1");
        Require(File.Exists(scriptPath), "Current v0.0.13 preview script is missing.");
        var script = File.ReadAllText(scriptPath);

        foreach (var file in new[]
        {
            "MRC-v0.0.13-1200x760.png",
            "MRC-v0.0.13-1180x760.png",
            "MRC-v0.0.13-900x560.png"
        })
        {
            Require(script.Contains(file, StringComparison.Ordinal), $"Current preview script does not produce {file}.");
        }

        Require(script.Contains("PngBitmapDecoder", StringComparison.Ordinal),
            "Current preview script does not verify rendered PNG geometry.");
        Require(script.Contains("-Width 1200 -Height 760", StringComparison.Ordinal)
                && script.Contains("-Width 1180 -Height 760", StringComparison.Ordinal)
                && script.Contains("-Width 900 -Height 560", StringComparison.Ordinal),
            "Current preview script does not cover default, responsive-large, and minimum viewports.");
        Require(script.Contains("artifacts\\v0.0.13", StringComparison.Ordinal),
            "Current preview evidence is not isolated under artifacts\\v0.0.13.");

        var workflowPath = Path.Combine(root, ".github", "workflows", "precert-v0.0.13-cli.yml");
        var workflow = File.ReadAllText(workflowPath);
        Require(workflow.Contains("./scripts/render-v0.0.13-preview.ps1", StringComparison.Ordinal),
            "v0.0.13 pre-cert workflow does not render current GUI previews.");
        Require(workflow.Contains("actions/upload-artifact@v6", StringComparison.Ordinal),
            "v0.0.13 pre-cert workflow does not upload current GUI preview evidence.");
        Require(workflow.Contains("artifacts/v0.0.13", StringComparison.Ordinal),
            "v0.0.13 pre-cert workflow does not publish the current preview evidence directory.");

        var historicalScript = Path.Combine(root, "scripts", "render-v0.0.12-preview.ps1");
        Require(File.Exists(historicalScript), "Historical v0.0.12 preview evidence script must remain preserved.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
