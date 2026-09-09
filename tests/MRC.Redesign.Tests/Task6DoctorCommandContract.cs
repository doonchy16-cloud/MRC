using System.Reflection;
using System.Runtime.CompilerServices;
using MRC.Cli;
using MRC.Core.Diagnostics;

internal static class Task6DoctorCommandContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifyAutomaticLowRiskRepairMode().GetAwaiter().GetResult();
        Console.WriteLine("PASS  Task6E1 Doctor automatic low-risk repair mode");

        VerifyStructuredDoctorPresentation().GetAwaiter().GetResult();
        Console.WriteLine("PASS  Task6E2 structured semantic Doctor presentation");
    }

    private static async Task VerifyAutomaticLowRiskRepairMode()
    {
        var runnerType = typeof(Func<DoctorRunOptions, CancellationToken, Task<DoctorReport>>);
        var constructor = typeof(CliDispatcher).GetConstructor(new[] { typeof(IGuiLauncher), runnerType });
        Require(constructor is not null,
            "CliDispatcher lacks an injectable Doctor runner seam for deterministic repair-mode verification.");

        DoctorRunOptions? captured = null;
        Func<DoctorRunOptions, CancellationToken, Task<DoctorReport>> doctorRunner = (options, _) =>
        {
            captured = options;
            return Task.FromResult(new DoctorReport(
                Array.Empty<DoctorFinding>(),
                Array.Empty<DoctorRepairAction>(),
                Array.Empty<DoctorVerificationResult>(),
                DoctorHealth.Healthy));
        };

        var dispatcher = (CliDispatcher)constructor!.Invoke(new object[]
        {
            new SuccessfulLauncher(),
            doctorRunner
        });

        var output = new StringWriter();
        var error = new StringWriter();
        var exitCode = await dispatcher.ExecuteAsync(new[] { "-doctor" }, output, error);

        Require(exitCode == 0, $"MRC -doctor returned exit code {exitCode} with a healthy fake report.");
        Require(captured is not null, "MRC -doctor did not invoke the injected Doctor runner.");
        Require(captured!.ApplyAutomaticRepairs,
            "MRC -doctor does not enable automatic low-risk repairs.");
        Require(captured.ApprovedRepairIds is null || captured.ApprovedRepairIds.Count == 0,
            "MRC -doctor implicitly approves higher-risk repairs; approval-required repairs must remain gated.");
    }

    private static async Task VerifyStructuredDoctorPresentation()
    {
        var report = SampleReport();
        var method = typeof(CliPresentation).GetMethod(
            "DoctorLines",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(DoctorReport) },
            modifiers: null);
        Require(method is not null, "CliPresentation.DoctorLines(DoctorReport) is missing.");

        var result = method!.Invoke(null, new object[] { report }) as System.Collections.IEnumerable;
        Require(result is not null, "CliPresentation.DoctorLines returned null.");
        var lines = result!.Cast<object>().ToArray();

        string Text(object line) => line.GetType().GetProperty("Text")?.GetValue(line)?.ToString() ?? string.Empty;
        string Tone(object line) => line.GetType().GetProperty("Tone")?.GetValue(line)?.ToString() ?? string.Empty;

        foreach (var section in new[] { "FIND", "REPAIR", "VERIFY", "RESULT" })
        {
            var sectionLine = lines.SingleOrDefault(line => Text(line).Equals(section, StringComparison.OrdinalIgnoreCase));
            Require(sectionLine is not null, $"Doctor presentation is missing the {section} section.");
            Require(Tone(sectionLine!) == "Heading",
                $"Doctor {section} section tone is {Tone(sectionLine!)}, expected Heading.");
        }

        RequireTone(lines, Text, Tone, "[PASS] Authority check", "Success");
        RequireTone(lines, Text, Tone, "[WARN] Rollback", "Warning");
        RequireTone(lines, Text, Tone, "[FAIL] PATH installation", "Error");
        RequireTone(lines, Text, Tone, "Repair PATH", "Success");
        RequireTone(lines, Text, Tone, "find.fail", "Success");
        RequireTone(lines, Text, Tone, "Health: REPAIRED", "Success");

        Func<DoctorRunOptions, CancellationToken, Task<DoctorReport>> doctorRunner = (_, _) => Task.FromResult(report);
        var dispatcher = new CliDispatcher(new SuccessfulLauncher(), doctorRunner);
        var output = new StringWriter();
        var error = new StringWriter();
        var exitCode = await dispatcher.ExecuteAsync(new[] { "--doctor" }, output, error);
        var text = output.ToString();

        Require(exitCode == 0, $"Structured Doctor report returned exit code {exitCode}.");
        Require(string.IsNullOrWhiteSpace(error.ToString()), $"Structured Doctor report wrote to stderr: {error}");
        foreach (var section in new[] { "FIND", "REPAIR", "VERIFY", "RESULT" })
        {
            Require(text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Any(line => line.Trim().Equals(section, StringComparison.OrdinalIgnoreCase)),
                $"MRC --doctor output is missing the {section} section: {text}");
        }
        Require(!text.Contains("\u001b[", StringComparison.Ordinal),
            "Redirected Doctor output contains ANSI escape sequences.");
    }

    private static DoctorReport SampleReport() => new(
        new[]
        {
            new DoctorFinding("find.pass", "Authority check", DoctorCheckStatus.Pass, "Authority is valid."),
            new DoctorFinding("find.warn", "Rollback", DoctorCheckStatus.Warning, "Rollback payload is unavailable."),
            new DoctorFinding(
                "find.fail",
                "PATH installation",
                DoctorCheckStatus.Fail,
                "PATH requires repair.",
                Repairable: true,
                Risk: DoctorRepairRisk.Automatic)
        },
        new[]
        {
            new DoctorRepairAction(
                "find.fail",
                "Repair PATH",
                DoctorRepairRisk.Automatic,
                Attempted: true,
                Succeeded: true,
                VerificationPassed: true,
                Message: "PATH repaired.")
        },
        new[]
        {
            new DoctorVerificationResult("find.fail", Passed: true, "PATH verification passed.")
        },
        DoctorHealth.Repaired);

    private static void RequireTone(
        IEnumerable<object> lines,
        Func<object, string> text,
        Func<object, string> tone,
        string fragment,
        string expectedTone)
    {
        var line = lines.FirstOrDefault(item => text(item).Contains(fragment, StringComparison.OrdinalIgnoreCase));
        Require(line is not null, $"Doctor presentation is missing '{fragment}'.");
        Require(tone(line!) == expectedTone,
            $"Doctor line '{fragment}' tone is {tone(line!)}, expected {expectedTone}.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class SuccessfulLauncher : IGuiLauncher
    {
        public GuiLaunchResult Launch() => new(true, "MRC GUI launch requested.");
    }
}
