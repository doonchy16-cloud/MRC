namespace MRC.Cli;

public sealed record CliTableRow(
    string Command,
    IReadOnlyList<string> Aliases,
    string Description);

public static class CliTableFormatter
{
    public const int DefaultWidth = 108;
    private const int MinimumWidth = 60;
    private const int CommandWidth = 16;
    private const int AliasWidth = 23;
    private const int GapWidth = 2;

    public static IReadOnlyList<CliLine> Format(
        IReadOnlyList<CliTableRow> rows,
        int width = DefaultWidth)
    {
        ArgumentNullException.ThrowIfNull(rows);
        width = Math.Max(MinimumWidth, width);

        var descriptionStart = CommandWidth + GapWidth + AliasWidth + GapWidth;
        var descriptionWidth = Math.Max(12, width - descriptionStart);
        var lines = new List<CliLine>
        {
            new(
                CliTone.Heading,
                new CliSegment("COMMAND".PadRight(CommandWidth + GapWidth), CliTone.Heading),
                new CliSegment("ALIASES".PadRight(AliasWidth + GapWidth), CliTone.Heading),
                new CliSegment("DESCRIPTION", CliTone.Heading)),
            new(new string('─', Math.Min(width, descriptionStart + descriptionWidth)), CliTone.Secondary)
        };

        foreach (var row in rows)
        {
            var command = row.Command.Trim();
            var aliases = string.Join(", ", row.Aliases.Where(alias => !string.IsNullOrWhiteSpace(alias)).Select(alias => alias.Trim()));
            var wrapped = Wrap(row.Description, descriptionWidth);

            lines.Add(new CliLine(
                CliTone.Heading,
                new CliSegment(command.PadRight(CommandWidth + GapWidth), CliTone.Heading),
                new CliSegment(aliases.PadRight(AliasWidth + GapWidth), CliTone.Secondary),
                new CliSegment(wrapped[0], CliTone.Normal)));

            foreach (var continuation in wrapped.Skip(1))
            {
                lines.Add(new CliLine(
                    CliTone.Normal,
                    new CliSegment(new string(' ', descriptionStart), CliTone.Normal),
                    new CliSegment(continuation, CliTone.Normal)));
            }
        }

        return lines;
    }

    private static IReadOnlyList<string> Wrap(string text, int width)
    {
        text = (text ?? string.Empty).Trim();
        if (text.Length == 0) return new[] { string.Empty };

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lines = new List<string>();
        var current = new System.Text.StringBuilder();

        foreach (var word in words)
        {
            if (current.Length == 0)
            {
                AppendWordOrChunks(word, width, lines, current);
                continue;
            }

            if (current.Length + 1 + word.Length <= width)
            {
                current.Append(' ').Append(word);
                continue;
            }

            lines.Add(current.ToString());
            current.Clear();
            AppendWordOrChunks(word, width, lines, current);
        }

        if (current.Length > 0) lines.Add(current.ToString());
        return lines.Count == 0 ? new[] { string.Empty } : lines;
    }

    private static void AppendWordOrChunks(
        string word,
        int width,
        List<string> lines,
        System.Text.StringBuilder current)
    {
        var remaining = word;
        while (remaining.Length > width)
        {
            lines.Add(remaining[..width]);
            remaining = remaining[width..];
        }

        if (remaining.Length > 0) current.Append(remaining);
    }
}
