namespace MRC.Pass3.Tests;

internal static class VisualPreviewAcceptance
{
    public static int Run()
    {
        var tests = new (string Name, Action Body)[]
        {
            ("deterministic WPF preview renderer exists", PreviewRendererExists),
            ("preview covers all states long identities and external Lotto warning", PreviewDataCoverage),
            ("preview renderer supports default and minimum geometry", ResponsiveRendererContract),
            ("v0.0.12 preview script enforces exact canonical PNG outputs", PreviewScriptContract),
            ("final redesign CI renders and uploads both visual artifacts", WorkflowPublishesPreview)
        };
        var failures=0;
        Console.WriteLine($"MRC PASS 3 visual-preview harness — {tests.Length} tests");
        foreach(var test in tests){try{test.Body();Console.WriteLine($"PASS  {test.Name}");}catch(Exception ex){failures++;Console.WriteLine($"FAIL  {test.Name}\n      {ex.Message}");}}
        Console.WriteLine(failures==0?$"PASS  all {tests.Length} PASS 3 visual-preview tests":$"FAIL  {failures} of {tests.Length} PASS 3 visual-preview tests");
        Console.WriteLine(); return failures;
    }

    private static void PreviewRendererExists()
    {
        Require(File.Exists(Path.Combine(RepoRoot(),"tools","MRC.Pass3.Preview","MRC.Pass3.Preview.csproj")),"Preview renderer project is missing.");
        Require(File.Exists(Path.Combine(RepoRoot(),"tools","MRC.Pass3.Preview","Program.cs")),"Preview renderer entry point is missing.");
    }

    private static void PreviewDataCoverage()
    {
        var code=PreviewCode();
        foreach(var state in new[]{"RunnerState.IDLE","RunnerState.BUSY","RunnerState.OFF","RunnerState.ERROR","RunnerState.STARTING","RunnerState.STOPPING"}) Require(code.Contains(state),$"Preview sample is missing {state}.");
        Require(code.Contains("VERY-LONG"),"Preview must include long identity stress data.");
        Require(code.Contains("Lotto_MainPC_Runner") && code.Contains("RunnerSystemFindingKind.External"),"Preview must include the real external Lotto warning case.");
        Require(code.Contains("RenderTargetBitmap") && code.Contains("PngBitmapEncoder"),"Preview must render the real WPF visual tree to PNG.");
    }

    private static void ResponsiveRendererContract()
    {
        var code=PreviewCode();
        Require(code.Contains("1180")&&code.Contains("760")&&code.Contains("900")&&code.Contains("560"),"Renderer does not preserve both canonical geometries.");
        Require(code.Contains("args.Length > 1")&&code.Contains("args.Length > 2"),"Renderer must accept explicit width/height.");
    }

    private static void PreviewScriptContract()
    {
        var path=Path.Combine(RepoRoot(),"scripts","render-v0.0.12-preview.ps1"); Require(File.Exists(path),"v0.0.12 preview script is missing.");
        var s=File.ReadAllText(path);
        Require(s.Contains("MRC-v0.0.12-1180x760.png")&&s.Contains("MRC-v0.0.12-900x560.png"),"Canonical v0.0.12 preview filenames are missing.");
        Require(s.Contains("1180")&&s.Contains("760")&&s.Contains("900")&&s.Contains("560")&&s.Contains("PngBitmapDecoder"),"Preview script does not verify exact PNG geometry.");
    }

    private static void WorkflowPublishesPreview()
    {
        var w=File.ReadAllText(Path.Combine(RepoRoot(),".github","workflows","redesign-v0.0.12.yml"));
        Require(w.Contains("Render canonical v0.0.12 previews"),"Final workflow does not render canonical previews.");
        Require(w.Contains("Upload v0.0.12 pre-cert evidence"),"Final workflow does not upload pre-cert evidence.");
        Require(w.Contains("actions/upload-artifact@v6"),"Current artifact uploader is missing.");
        Require(w.Contains("MRC-v0.0.12-1180x760.png")&&w.Contains("MRC-v0.0.12-900x560.png"),"Workflow does not require both canonical PNGs.");
    }

    private static string PreviewCode()=>File.ReadAllText(Path.Combine(RepoRoot(),"tools","MRC.Pass3.Preview","Program.cs"));
    private static string RepoRoot(){var d=new DirectoryInfo(Directory.GetCurrentDirectory());while(d is not null){if(File.Exists(Path.Combine(d.FullName,"Auth","0000_MasterAuth.md")))return d.FullName;d=d.Parent;}throw new InvalidOperationException("Could not locate MRC repository root.");}
    private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
}
