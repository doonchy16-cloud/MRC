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

    public async Task WriteLineAsync(string text, CliTone tone)
    {
        if (!ColorEnabled)
        {
            await _writer.WriteLineAsync(text);
            return;
        }

        var previous = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = CliPalette.ColorFor(tone);
            await _writer.WriteLineAsync(text);
        }
        finally
        {
            Console.ForegroundColor = previous;
        }
    }
}
