using System.Runtime.CompilerServices;

internal static class Task45V0015EvidenceContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        var root = RepoRoot();
        var renderPath = Path.Combine(root, "scripts", "render-v0.0.15-preview.ps1");
        var comparisonPath = Path.Combine(root, "scripts", "build-v0.0.15-reference-comparison.ps1");
        var previewPath = Path.Combine(root, "tools", "MRC.Pass3.Preview", "Program.cs");
        var workflowPath = Path.Combine(root, ".github", "workflows", "precert-v0.0.15.yml");

        Require(File.Exists(renderPath), "render-v0.0.15-preview.ps1 is missing.");
        Require(File.Exists(comparisonPath), "build-v0.0.15-reference-comparison.ps1 is missing.");

        var render = File.ReadAllText(renderPath);
        var comparison = File.ReadAllText(comparisonPath);
        var preview = File.ReadAllText(previewPath);
        var workflow = File.ReadAllText(workflowPath);

        foreach (var file in new[]
                 {
                     "MRC-v0.0.15-2048x1222.png",
                     "MRC-v0.0.15-1200x760.png",
                     "MRC-v0.0.15-1180x760.png",
                     "MRC-v0.0.15-900x560.png",
                     "MRC-v0.0.15-900x560-busy.png",
                     "MRC-v0.0.15-1200x760-drawer.png",
                     "MRC-v0.0.15-2048x1222-mixed.png"
                 })
        {
            Require(render.Contains(file, StringComparison.Ordinal), $"Render matrix is missing '{file}'.");
        }

        foreach (var file in new[] { "reference-side-by-side.png", "reference-overlay-50.png" })
        {
            Require(comparison.Contains(file, StringComparison.Ordinal), $"Reference comparison is missing '{file}'.");
        }

        Require(render.Contains("artifacts\\v0.0.15", StringComparison.Ordinal),
            "Render output root is not artifacts\\v0.0.15.");
        Require(render.Contains("PixelWidth", StringComparison.Ordinal)
                && render.Contains("PixelHeight", StringComparison.Ordinal),
            "Render script does not verify PNG dimensions.");

        foreach (var flag in new[] { "--width", "--height", "--output", "--focus-state", "--drawer", "--mixed-state" })
        {
            Require(preview.Contains(flag, StringComparison.Ordinal), $"Preview tool is missing deterministic flag '{flag}'.");
        }
        Require(preview.Contains("ConfigurePreview", StringComparison.Ordinal),
            "Preview tool no longer uses operation-disabled preview mode.");

        Require(comparison.Contains("PresentationCore", StringComparison.Ordinal)
                && comparison.Contains("RenderTargetBitmap", StringComparison.Ordinal)
                && comparison.Contains("4096", StringComparison.Ordinal)
                && comparison.Contains("2048", StringComparison.Ordinal)
                && comparison.Contains("0.5", StringComparison.Ordinal),
            "Reference comparison geometry or WPF-only implementation is incomplete.");

        Require(workflow.Contains("Render v0.0.15 GUI previews", StringComparison.Ordinal)
                && workflow.Contains("actions/upload-artifact@v6", StringComparison.Ordinal)
                && workflow.Contains("MRC-v0.0.15-gui-previews", StringComparison.Ordinal)
                && workflow.Contains("artifacts/v0.0.15", StringComparison.Ordinal),
            "v0.0.15 development CI does not render and upload the GUI evidence package.");
    }

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Auth", "0000_MasterAuth.md")))
                return directory.FullName;
            directory = directory.Parent;
        }
        throw new InvalidOperationException("Could not locate MRC repository root.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
