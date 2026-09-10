using System.Runtime.CompilerServices;
using MRC.Gui.Presentation;

internal static class Task36V0015ReferenceMetricsContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        Require(ReferenceReplicaMetrics.CardTargetWidth == 469, "Card target width drifted.");
        Require(ReferenceReplicaMetrics.CardTargetHeight == 150, "Card target height drifted.");
        Require(ReferenceReplicaMetrics.CardGap == 22, "Card gap drifted.");
        Require(ReferenceReplicaMetrics.ActionHeight == 46, "Action height drifted.");
        Require(ReferenceReplicaMetrics.ColumnCountForWidth(2048) == 4, "2048 must use 4 columns.");
        Require(ReferenceReplicaMetrics.ColumnCountForWidth(1200) == 2, "1200 must use 2 columns.");
        Require(ReferenceReplicaMetrics.ColumnCountForWidth(1180) == 2, "1180 must use 2 columns.");
        Require(ReferenceReplicaMetrics.ColumnCountForWidth(900) == 1, "900 must use 1 column.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
