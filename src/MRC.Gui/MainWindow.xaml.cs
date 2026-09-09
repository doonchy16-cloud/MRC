using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using MRC.Core;
using MRC.Core.Runtime;
using MRC.Gui.Presentation;

namespace MRC.Gui;

public partial class MainWindow : Window
{
    private readonly RunnerDashboardViewModel _dashboard = new();
    private readonly RunnerAnimationClock _animationClock = new();
    private readonly DispatcherTimer _refreshTimer = new DispatcherTimer();
    private readonly DispatcherTimer _animationTimer = new DispatcherTimer();
    private RunnerEngine? _engine;
    private bool _refreshInProgress;
    private bool _previewMode;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _dashboard;

        _refreshTimer.Interval = TimeSpan.FromSeconds(3);
        _refreshTimer.Tick += async (_, _) => await RefreshDashboardAsync();

        _animationTimer.Interval = TimeSpan.FromMilliseconds(55);
        _animationTimer.Tick += (_, _) => _animationClock.Tick(DateTimeOffset.UtcNow, _dashboard.Rows);

        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    public void ConfigurePreview(IReadOnlyList<RunnerSnapshot> snapshots)
    {
        ArgumentNullException.ThrowIfNull(snapshots);
        _previewMode = true;
        _dashboard.ApplySnapshots(snapshots);
        MachineValue.Text = "MAIN-PC";
        RootValue.Text = MrcConstants.RunnerRoot;
        BoundaryValue.Text = "AUTHORIZED";
        BoundaryValue.Foreground = BrushFromHex("#39E58C");
        BoundaryDot.Fill = BrushFromHex("#39E58C");
        SetBoundaryDetails("Deterministic PASS 3 visual preview data.");
        RefreshStatusValue.Text = $"Preview snapshot • {_dashboard.TotalCount} runners • rendered without runtime control";
        _animationClock.Tick(DateTimeOffset.UtcNow, _dashboard.Rows);
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_previewMode) return;

        var fence = EnvironmentFence.EvaluateCurrent();
        RenderBoundary(fence);

        if (fence.IsAuthorized)
        {
            try
            {
                _engine = new RunnerEngine();
                await RefreshDashboardAsync();
                _refreshTimer.Start();
                _animationTimer.Start();
            }
            catch (Exception ex)
            {
                _engine = null;
                RefreshStatusValue.Text = $"Engine unavailable • {ex.Message}";
                RefreshStatusValue.Foreground = BrushFromHex("#FF8A3D");
            }
        }
        else
        {
            _dashboard.ApplySnapshots(Array.Empty<RunnerSnapshot>());
            RefreshStatusValue.Text = $"CONTROL BLOCKED • {fence.Code}";
            RefreshStatusValue.Foreground = BrushFromHex("#FF8A3D");
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _refreshTimer.Stop();
        _animationTimer.Stop();
    }

    private void RenderBoundary(EnvironmentFenceResult fence)
    {
        MachineValue.Text = string.IsNullOrWhiteSpace(fence.MachineName) ? "UNKNOWN" : fence.MachineName;
        RootValue.Text = MrcConstants.RunnerRoot;
        BoundaryValue.Text = fence.IsAuthorized ? "AUTHORIZED" : $"BLOCKED — {fence.Code}";
        var boundaryBrush = BrushFromHex(fence.IsAuthorized ? "#39E58C" : "#FF8A3D");
        BoundaryValue.Foreground = boundaryBrush;
        BoundaryDot.Fill = boundaryBrush;
        SetBoundaryDetails(fence.Message);
    }

    private void SetBoundaryDetails(string message)
    {
        BoundaryValue.ToolTip = message;
        BoundaryDot.ToolTip = message;
        AutomationProperties.SetHelpText(BoundaryValue, message);
    }

    private async Task RefreshDashboardAsync()
    {
        if (_engine is null || _refreshInProgress || _previewMode) return;

        _refreshInProgress = true;
        try
        {
            RefreshStatusValue.Text = "Refreshing runner state…";
            RefreshStatusValue.Foreground = BrushFromHex("#AAB6C3");
            var snapshots = await Task.Run(() => _engine.Refresh());
            _dashboard.ApplySnapshots(snapshots);
            _animationClock.Tick(DateTimeOffset.UtcNow, _dashboard.Rows);
            RefreshStatusValue.Text = $"Updated {DateTime.Now:HH:mm:ss} • {_dashboard.TotalCount} runners • stable repository/name order";
            RefreshStatusValue.Foreground = BrushFromHex("#AAB6C3");
        }
        catch (Exception ex)
        {
            RefreshStatusValue.Text = $"Refresh error • {ex.Message}";
            RefreshStatusValue.Foreground = BrushFromHex("#FF8A3D");
        }
        finally
        {
            _refreshInProgress = false;
        }
    }

    private void SearchBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        _dashboard.SearchText = SearchBox.Text;
    }

    private void FilterButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string tag } || !Enum.TryParse<RunnerFilter>(tag, out var filter)) return;
        _dashboard.SelectedFilter = filter;

        foreach (var button in FilterPanel.Children.OfType<Button>())
        {
            var isSelected = string.Equals(button.Tag as string, tag, StringComparison.OrdinalIgnoreCase);
            button.BorderBrush = BrushFromHex(isSelected ? "#35D9FF" : "#263342");
            button.Foreground = BrushFromHex(isSelected ? "#F4F7FA" : "#AAB6C3");
        }
    }

    private async void ManualRefreshButton_OnClick(object sender, RoutedEventArgs e)
    {
        await RefreshDashboardAsync();
    }

    private static Brush BrushFromHex(string value) =>
        (Brush)new BrushConverter().ConvertFromString(value)!;
}
