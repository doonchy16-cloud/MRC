using System.Reflection;
using System.Runtime.CompilerServices;
using MRC.Cli;

internal static class Task26V014HelpTableContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifyHelpIsRealTerminalTable();
        VerifyWidthAwareDescriptionWrapping();
        VerifyReusableFormatterAuthority();
        Console.WriteLine("PASS  Task26 v0.0.14 help terminal table contract");
    }

    private static void VerifyHelpIsRealTerminalTable()
    {
        var lines = CliPresentation.HelpLines();
        var header = lines.SingleOrDefault(line =>
            line.Text.Contains("COMMAND", StringComparison.Ordinal)
            && line.Text.Contains("ALIASES", StringComparison.Ordinal)
            && line.Text.Contains("DESCRIPTION", StringComparison.Ordinal));
        Require(header is not null,
            "V0.0.14 help is missing the COMMAND / ALIASES / DESCRIPTION table header.");

        Require(!lines.Any(line => line.Text.Contains("alias:", StringComparison.OrdinalIgnoreCase)
                                   || line.Text.Contains("aliases:", StringComparison.OrdinalIgnoreCase)),
            "Help still renders aliases as separate footnote lines instead of one logical command row.");

        VerifyRow(lines, "MRC", Array.Empty<string>(), "Open or focus the GUI");
        VerifyRow(lines, "MRC --version", new[] { "MRC -v", "MRC -version" }, "Show installed version information");
        VerifyRow(lines, "MRC --help", new[] { "MRC -h", "MRC -help" }, "Show this help");
        VerifyRow(lines, "MRC --doctor", new[] { "MRC -doctor" }, "automatic low-risk repairs");
        VerifyRow(lines, "MRC --diagnose", new[] { "MRC -diagnose" }, "read-only runner/process diagnostics");
        VerifyRow(lines, "MRC --update", new[] { "MRC -update" }, "atomically activate an allowed release");
        VerifyRow(lines, "MRC --check", new[] { "MRC -check" }, "available update without installing it");
    }

    private static void VerifyRow(
        IReadOnlyList<CliLine> lines,
        string canonical,
        IReadOnlyList<string> aliases,
        string descriptionFragment)
    {
        var row = lines.SingleOrDefault(line =>
            line.Text.StartsWith(canonical, StringComparison.Ordinal)
            && line.Text.Contains(descriptionFragment, StringComparison.OrdinalIgnoreCase));
        Require(row is not null, $"Help is missing canonical command row '{canonical}'.");

        Require(row!.Segments.Any(segment =>
                segment.Tone == CliTone.Command
                && segment.Text.Trim().Equals("MRC", StringComparison.Ordinal)),
            $"Help row '{canonical}' does not preserve the yellow MRC command token.");

        if (!canonical.Equals("MRC", StringComparison.Ordinal))
        {
            var option = canonical["MRC ".Length..];
            Require(row.Segments.Any(segment =>
                    segment.Tone == CliTone.Heading
                    && segment.Text.Equals(option, StringComparison.Ordinal)),
                $"Help row '{canonical}' does not preserve the cyan option token '{option}'.");
        }

        Require(row.Segments.Any(segment =>
                segment.Tone == CliTone.Normal
                && segment.Text.Contains(descriptionFragment, StringComparison.OrdinalIgnoreCase)),
            $"Help row '{canonical}' is missing its white description content.");

        foreach (var alias in aliases)
        {
            Require(row.Segments.Any(segment =>
                    segment.Tone == CliTone.Secondary
                    && segment.Text.Contains(alias, StringComparison.Ordinal)),
                $"Help row '{canonical}' does not preserve alias '{alias}' in the ALIASES column.");
        }
    }

    private static void VerifyWidthAwareDescriptionWrapping()
    {
        var method = typeof(CliPresentation).GetMethod(
            "HelpLines",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(int) },
            modifiers: null);
        Require(method is not null,
            "Help presentation has no width-aware path for deterministic DESCRIPTION-column wrapping.");

        var lines = (IReadOnlyList<CliLine>)method!.Invoke(null, new object[] { 72 })!;
        Require(lines.All(line => line.Text.Length <= 72),
            "Width-aware help emits a line wider than the requested ordinary terminal width.");

        var header = lines.Single(line =>
            line.Text.Contains("COMMAND", StringComparison.Ordinal)
            && line.Text.Contains("ALIASES", StringComparison.Ordinal)
            && line.Text.Contains("DESCRIPTION", StringComparison.Ordinal));
        var descriptionColumn = header.Text.IndexOf("DESCRIPTION", StringComparison.Ordinal);
        Require(descriptionColumn > 0, "DESCRIPTION column could not be located.");

        var continuation = lines.FirstOrDefault(line =>
            line.Text.Length > descriptionColumn
            && string.IsNullOrWhiteSpace(line.Text[..descriptionColumn])
            && !string.IsNullOrWhiteSpace(line.Text[descriptionColumn..]));
        Require(continuation is not null,
            "Narrow help does not wrap long descriptions under the DESCRIPTION column.");
        Require(continuation!.Segments.Where(segment => !string.IsNullOrWhiteSpace(segment.Text))
                .All(segment => segment.Tone == CliTone.Normal),
            "Wrapped DESCRIPTION continuation text is not rendered as ordinary white prose.");
    }

    private static void VerifyReusableFormatterAuthority()
    {
        var root = RepoRoot();
        var formatter = Path.Combine(root, "src", "MRC.Cli", "CliTableFormatter.cs");
        Require(File.Exists(formatter),
            "Central reusable CLI table formatter is missing.");
        var presentation = File.ReadAllText(Path.Combine(root, "src", "MRC.Cli", "CliPresentation.cs"));
        Require(presentation.Contains("CliTableFormatter", StringComparison.Ordinal),
            "Help table still uses ad hoc spacing instead of the central reusable formatter.");
    }

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Auth", "0000_MasterAuth.md")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }
        throw new InvalidOperationException("Could not locate MRC repository root.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
