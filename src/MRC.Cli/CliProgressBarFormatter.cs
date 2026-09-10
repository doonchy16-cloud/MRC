namespace MRC.Cli;

public static class CliProgressBarFormatter
{
    public const int DefaultCells = 20;
    private const int MinimumCells = 8;
    private const int MaximumCells = 32;

    public static CliLine Format(
        int percent,
        CliTone filledTone,
        int cells = DefaultCells)
    {
        cells = Math.Clamp(cells, MinimumCells, MaximumCells);
        var boundedPercent = Math.Clamp(percent, 0, 100);
        var filledCells = (int)Math.Round(
            boundedPercent / 100d * cells,
            MidpointRounding.AwayFromZero);
        filledCells = Math.Clamp(filledCells, 0, cells);

        var segments = new List<CliSegment>
        {
            new("[", CliTone.Secondary)
        };

        if (filledCells > 0)
        {
            segments.Add(new CliSegment(new string('█', filledCells), filledTone));
        }

        if (filledCells < cells)
        {
            segments.Add(new CliSegment(new string('░', cells - filledCells), CliTone.Secondary));
        }

        segments.Add(new CliSegment($"] {percent,3}% ", CliTone.Metadata));
        return new CliLine(filledTone, segments.ToArray());
    }
}
