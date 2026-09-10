namespace MRC.Cli;

public sealed class CliRenderer
{
    private readonly TextWriter _writer;

    public CliRenderer(TextWriter writer)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        ColorEnabled =
            (ReferenceEquals(writer, Console.Out) && !Console.IsOutputRedirected)
            || (ReferenceEquals(writer, Console.Error) && !Console.IsErrorRedirected);
    }

    public bool ColorEnabled { get; }

    public Task WriteLineAsync(string text, CliTone tone) =>
        WriteLineAsync(new CliLine(text, tone));

    public async Task WriteLineAsync(CliLine line)
    {
        ArgumentNullException.ThrowIfNull(line);

        if (!ColorEnabled)
        {
            await _writer.WriteLineAsync(line.Text);
            return;
        }

        var previous = Console.ForegroundColor;
        try
        {
            foreach (var segment in line.Segments)
            {
                Console.ForegroundColor = CliPalette.ColorFor(segment.Tone);
                await _writer.WriteAsync(segment.Text);
            }

            await _writer.WriteLineAsync();
        }
        finally
        {
            Console.ForegroundColor = previous;
        }
    }
}
