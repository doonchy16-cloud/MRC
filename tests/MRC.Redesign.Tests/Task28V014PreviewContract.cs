using System.Runtime.CompilerServices;

internal static class Task28V014PreviewContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifyV014PreviewPipeline();
        Console.WriteLine("PASS  Task28 v0.0.14 visual evidence pipeline");
    }

    private static void VerifyV014PreviewPipeline()
    {
        var root = Directory.GetCurrentDirectory();
        var scriptPath = Path.Combine(root, "scripts", "render-v0.0.14-preview.ps1");
        Require(File.Exists(scriptPath), "v0.0.14 preview script is missing.");
        var script = File.ReadAllText(scriptPath);

        foreach (var file in new[]
        {
            "MRC-v0.0.14-2048x1222.png",
            "MRC-v0.0.14-1200x760.png",
            "MRC-v0.0.14-1180x760.png",
            "MRC-v0.0.14-900x560.png"
        })
        {
            Require(script.Contains(file, StringComparison.Ordinal), $"v0.0.14 preview script does not produce {file}.");
        }

        Require(script.Contains("PngBitmapDecoder", StringComparison.Ordinal),
            "v0.0.14 preview script does not verify actual PNG dimensions.");
        Require(script.Contains("-Width 2048 -Height 1222", StringComparison.Ordinal)
                && script.Contains("-Width 1200 -Height 760", StringComparison.Ordinal)
                && script.Contains("-Width 1180 -Height 760", StringComparison.Ordinal)
                && script.Contains("-Width 900 -Height 560", StringComparison.Ordinal),
            "v0.0.14 preview script does not cover every locked acceptance viewport.");
        Require(script.Contains("artifacts\\v0.0.14", StringComparison.Ordinal),
            "v0.0.14 visual evidence is not isolated under artifacts\\v0.0.14.");

        var workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "precert-v0.0.14.yml"));
        Require(workflow.Contains("./scripts/render-v0.0.14-preview.ps1", StringComparison.Ordinal),
            "v0.0.14 CI does not render current GUI previews.");
        Require(workflow.Contains("actions/upload-artifact@v6", StringComparison.Ordinal),
            "v0.0.14 CI does not upload current visual evidence.");
        Require(workflow.Contains("artifacts/v0.0.14", StringComparison.Ordinal),
            "v0.0.14 CI does not publish the current preview evidence directory.");

        Require(File.Exists(Path.Combine(root, "scripts", "render-v0.0.13-preview.ps1")),
            "Historical v0.0.13 preview evidence pipeline must remain preserved.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
