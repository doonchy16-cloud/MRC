using System.Windows;
using System.Windows.Controls;
using MRC.Gui.Presentation;

namespace MRC.Gui.Controls;

public partial class ControlDrawer : UserControl
{
    public ControlDrawer()
    {
        InitializeComponent();
    }

    public event RoutedEventHandler? TurnAllOnRequested;
    public event RoutedEventHandler? TurnAllOffRequested;
    public event RoutedEventHandler? RefreshRequested;

    public void FocusSearch()
    {
        SearchBox.Focus();
        SearchBox.SelectAll();
    }

    private void SearchBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (DataContext is RunnerDashboardViewModel dashboard)
            dashboard.SearchText = SearchBox.Text;
    }

    private void FilterButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not RunnerDashboardViewModel dashboard) return;
        if (sender is Button { Tag: string tag } && Enum.TryParse<RunnerFilter>(tag, out var filter))
            dashboard.SelectedFilter = filter;
    }

    private void TurnAllOn_OnClick(object sender, RoutedEventArgs e) =>
        TurnAllOnRequested?.Invoke(this, e);

    private void TurnAllOff_OnClick(object sender, RoutedEventArgs e) =>
        TurnAllOffRequested?.Invoke(this, e);

    private void Refresh_OnClick(object sender, RoutedEventArgs e) =>
        RefreshRequested?.Invoke(this, e);
}
