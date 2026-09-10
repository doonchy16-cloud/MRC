namespace MRC.Gui.Presentation;

public static class ReferenceReplicaMetrics
{
    public const double CanonicalWidth = 2048;
    public const double CanonicalHeight = 1222;
    public const double LargeFieldInset = 41;
    public const double CardTargetWidth = 469;
    public const double CardTargetHeight = 150;
    public const double CardGap = 22;
    public const double CardHorizontalPadding = 19;
    public const double ActionHeight = 46;
    public const double MinimumCardSlotWidth = 470;
    private const double WindowChromeAllowance = 48;

    public static int ColumnCountForWidth(double windowWidth)
    {
        if (double.IsNaN(windowWidth) || double.IsInfinity(windowWidth) || windowWidth <= 0) return 1;
        var usable = Math.Max(0, windowWidth - WindowChromeAllowance);
        return Math.Clamp((int)Math.Floor(usable / MinimumCardSlotWidth), 1, 4);
    }
}
