namespace MRC.Gui.Presentation;

public static class RunnerResponsiveLayout
{
    public const double MinimumCardSlotWidth = ReferenceReplicaMetrics.MinimumCardSlotWidth;

    public static int ColumnCountForWidth(double windowWidth) =>
        ReferenceReplicaMetrics.ColumnCountForWidth(windowWidth);
}
