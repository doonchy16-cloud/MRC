namespace MRC.Cli;

public enum CliTone
{
    Normal = 0,
    Heading = 1,
    Success = 2,
    Warning = 3,
    Error = 4,
    Path = 5,
    Metadata = 6,
    Secondary = 7,
    Command = 8
}

public static class CliPalette
{
    public static ConsoleColor ColorFor(CliTone tone) => tone switch
    {
        CliTone.Normal => ConsoleColor.White,
        CliTone.Heading => ConsoleColor.Cyan,
        CliTone.Success => ConsoleColor.Green,
        CliTone.Warning => ConsoleColor.Yellow,
        CliTone.Error => ConsoleColor.Red,
        CliTone.Path => ConsoleColor.Cyan,
        CliTone.Metadata => ConsoleColor.Magenta,
        CliTone.Secondary => ConsoleColor.DarkGray,
        CliTone.Command => ConsoleColor.Yellow,
        _ => ConsoleColor.White
    };
}
