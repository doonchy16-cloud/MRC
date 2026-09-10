using System.Runtime.CompilerServices;

internal static class Task34V014MinimumBusyEvidenceContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifyBusyFocusedPreviewCapability();
        VerifyMinimumBusyEvidenceFrame();
        Console.WriteLine("PASS  Task34 v0.0.14 minimum-size BUSY action evidence contract");
    }

    private static void VerifyBusyFocusedPreviewCapability()
    {
        var root = Directory.GetCurrentDirectory();
        var preview = File.ReadAllText(Path.Combine(root, "tools", "MRC.Pass3.Preview", "Program.cs"));

        Require(preview.Contains("focusState", StringComparison.Ordinal)
                && preview.Contains("RunnerState.BUSY", StringComparison.Ordinal)
                && preview.Contains("ScrollIntoView", StringComparison.Ordinal),
            "Preview renderer cannot focus a BUSY runner for minimum-size action evidence.");
    }

    private static void VerifyMinimumBusyEvidenceFrame()
    {
        var script = File.ReadAllText(Path.Combine(
            Directory.GetCurrentDirectory(), "scripts", "render-v0.0.14-preview.ps1"));

        Require(script.Contains("MRC-v0.0.14-900x560-busy.png", StringComparison.Ordinal)
                && script.Contains("-FocusState 'BUSY'", StringComparison.Ordinal),
            "v0.0.14 preview pipeline does not generate the required BUSY-focused 900x560 evidence frame.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
