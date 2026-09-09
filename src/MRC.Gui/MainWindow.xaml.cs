using System.Windows;
using System.Windows.Media;
using MRC.Core;

namespace MRC.Gui;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => RenderBoundary();
    }

    private void RenderBoundary()
    {
        var fence = EnvironmentFence.EvaluateCurrent();
        MachineValue.Text = string.IsNullOrWhiteSpace(fence.MachineName) ? "UNKNOWN" : fence.MachineName;
        RootValue.Text = MrcConstants.RunnerRoot;
        BoundaryValue.Text = fence.IsAuthorized ? "AUTHORIZED" : $"BLOCKED — {fence.Code}";
        BoundaryValue.Foreground = BrushFromHex(fence.IsAuthorized ? "#39E58C" : "#FF8A3D");
        DetailsValue.Text = fence.Message;
    }

    private static Brush BrushFromHex(string value) =>
        (Brush)new BrushConverter().ConvertFromString(value)!;
}
