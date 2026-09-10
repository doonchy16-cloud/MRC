namespace MRC.Gui.Presentation;

public static class RunnerResponsiveLayout
{
    public const double MinimumCardSlotWidth = 470;
    private const double WindowChromeAllowance = 48;

    public static int ColumnCountForWidth(double windowWidth)
    {
        if (double.IsNaN(windowWidth) || double.IsInfinity(windowWidth) || windowWidth <= 0)
        {
            return 1;
        }

        var usableWidth = Math.Max(0, windowWidth - WindowChromeAllowance);
        var columns = (int)Math.Floor(usableWidth / MinimumCardSlotWidth);
        return Math.Clamp(columns, 1, 4);
    }
}
