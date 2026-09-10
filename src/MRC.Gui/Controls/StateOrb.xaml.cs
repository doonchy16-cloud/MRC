using System.Windows;
using System.Windows.Controls;
using MRC.Core.Runtime;

namespace MRC.Gui.Controls;

public partial class StateOrb : UserControl
{
    public static readonly DependencyProperty StateProperty = DependencyProperty.Register(
        nameof(State),
        typeof(RunnerState),
        typeof(StateOrb),
        new PropertyMetadata(RunnerState.OFF));

    public static readonly DependencyProperty IntensityProperty = DependencyProperty.Register(
        nameof(Intensity),
        typeof(double),
        typeof(StateOrb),
        new PropertyMetadata(1.0));

    public StateOrb()
    {
        InitializeComponent();
    }

    public RunnerState State
    {
        get => (RunnerState)GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

    public double Intensity
    {
        get => (double)GetValue(IntensityProperty);
        set => SetValue(IntensityProperty, value);
    }
}
