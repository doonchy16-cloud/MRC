using System.Windows;
using System.Windows.Controls;
using MRC.Core.Runtime;
using MRC.Gui.Presentation;

namespace MRC.Gui.Controls;

public partial class RunnerControlCard : UserControl
{
    public static readonly DependencyProperty RowProperty = DependencyProperty.Register(
        nameof(Row), typeof(RunnerRowViewModel), typeof(RunnerControlCard));

    public RunnerControlCard()
    {
        InitializeComponent();
    }

    public RunnerRowViewModel? Row
    {
        get => (RunnerRowViewModel?)GetValue(RowProperty);
        set => SetValue(RowProperty, value);
    }

    public event EventHandler<RunnerCardActionRequestedEventArgs>? ActionRequested;

    private void Raise(RunnerCardAction action)
    {
        if (Row is null) return;
        ActionRequested?.Invoke(this, new RunnerCardActionRequestedEventArgs(Row, action));
    }

    private void Primary_OnClick(object sender, RoutedEventArgs e) =>
        Raise(Row?.State == RunnerState.BUSY ? RunnerCardAction.ForceStop : RunnerCardAction.Start);

    private void Stop_OnClick(object sender, RoutedEventArgs e) => Raise(RunnerCardAction.Stop);
    private void Restart_OnClick(object sender, RoutedEventArgs e) => Raise(RunnerCardAction.Restart);
    private void Details_OnClick(object sender, RoutedEventArgs e) => Raise(RunnerCardAction.Details);
}
