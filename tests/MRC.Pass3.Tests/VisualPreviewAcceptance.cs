namespace MRC.Pass3.Tests;

internal static class VisualPreviewAcceptance
{
    public static int Run()
    {
        var tests = new (string Name, Action Body)[]
        {
            ("deterministic WPF preview renderer exists", PreviewRendererExists),
            ("preview renderer covers all runtime states and long identities", PreviewDataCoverage),
            ("preview script enforces exact 1180x760 PNG output", PreviewScriptContract),
            ("PASS 3 CI renders and uploads the visual artifact", WorkflowPublishesPreview)
        };

        var failures = 0;
        Console.WriteLine($"MRC PASS 3 visual-preview harness — {tests.Length} tests");
        foreach (var test in tests)
        {
            try
            {
                test.Body();
                Console.WriteLine($"PASS  {test.Name}");
            }
            catch (Exception ex)
            {
                failures++;
                Console.WriteLine($"FAIL  {test.Name}");
                Console.WriteLine($"      {ex.Message}");
            }
        }

        Console.WriteLine(failures == 0
            ? $"PASS  all {tests.Length} PASS 3 visual-preview tests"
            : $"FAIL  {failures} of {tests.Length} PASS 3 visual-preview tests");
        Console.WriteLine();
        return failures;
    }

    private static void PreviewRendererExists()
    {
        Require(File.Exists(Path.Combine(RepoRoot(), "tools", "MRC.Pass3.Preview", "MRC.Pass3.Preview.csproj")),
            "Preview renderer project is missing.");
        Require(File.Exists(Path.Combine(RepoRoot(), "tools", "MRC.Pass3.Preview", "Program.cs")),
            "Preview renderer entry point is missing.");
    }

    private static void PreviewDataCoverage()
    {
        var code = File.ReadAllText(Path.Combine(RepoRoot(), "tools", "MRC.Pass3.Preview", "Program.cs"));
        foreach (var state in new[] { "RunnerState.IDLE", "RunnerState.BUSY", "RunnerState.OFF", "RunnerState.ERROR", "RunnerState.STARTING", "RunnerState.STOPPING" })
        {
            Require(code.Contains(state, StringComparison.Ordinal), $"Preview sample is missing {state}.");
        }
        Require(code.Contains("VERY-LONG", StringComparison.Ordinal), "Preview sample must include an intentionally long runner/repository identity.");
        Require(code.Contains("RenderTargetBitmap", StringComparison.Ordinal), "Preview must render the real WPF visual tree.");
        Require(code.Contains("PngBitmapEncoder", StringComparison.Ordinal), "Preview must encode a real PNG.");
    }

    private static void PreviewScriptContract()
    {
        var scriptPath = Path.Combine(RepoRoot(), "scripts", "render-pass3-preview.ps1");
        Require(File.Exists(scriptPath), "PASS 3 preview script is missing.");
        var script = File.ReadAllText(scriptPath);
        Require(script.Contains("MRC-PASS3-1180x760.png", StringComparison.Ordinal), "Canonical preview filename is missing.");
        Require(script.Contains("1180", StringComparison.Ordinal) && script.Contains("760", StringComparison.Ordinal),
            "Preview script must enforce 1180x760 dimensions.");
        Require(script.Contains("PNG", StringComparison.OrdinalIgnoreCase), "Preview script must validate PNG output.");
    }

    private static void WorkflowPublishesPreview()
    {
        var workflow = File.ReadAllText(Path.Combine(RepoRoot(), ".github", "workflows", "pass3.yml"));
        Require(workflow.Contains("Render PASS 3 preview", StringComparison.Ordinal), "PASS 3 workflow does not render the preview.");
        Require(workflow.Contains("Upload PASS 3 preview", StringComparison.Ordinal), "PASS 3 workflow does not upload the preview.");
        Require(workflow.Contains("actions/upload-artifact@v4", StringComparison.Ordinal), "PASS 3 preview artifact uploader is missing.");
        Require(workflow.Contains("artifacts/pass3/MRC-PASS3-1180x760.png", StringComparison.Ordinal), "PASS 3 workflow does not publish the canonical PNG path.");
    }

    private static string RepoRoot()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Auth", "0000_MasterAuth.md"))) return current.FullName;
            current = current.Parent;
        }
        throw new InvalidOperationException("Could not locate MRC repository root.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
