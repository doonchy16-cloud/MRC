using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MRC.Core;
using MRC.Core.Control;
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
    private RunnerOperationsService? _operations;
    private bool _refreshInProgress;
    private bool _operationInProgress;
    private bool _previewMode;

    public MainWindow()
    {
        InitializeComponent();
        Icon = BitmapFrame.Create(new Uri("pack://application:,,,/MRC.Gui;component/Assets/MRC.ico", UriKind.Absolute));
        VersionValue.Text = $"v{BuildInfo.Version} // PRE-CERT";
        DataContext = _dashboard;
        RunnerList.Loaded += (_, _) => ApplyRunnerScrollBarStyle();

        _refreshTimer.Interval = TimeSpan.FromSeconds(3);
        _refreshTimer.Tick += async (_, _) => await RefreshDashboardAsync();

        _animationTimer.Interval = TimeSpan.FromMilliseconds(50);
        _animationTimer.Tick += (_, _) => _animationClock.Tick(DateTimeOffset.UtcNow, _dashboard.Rows);

        SizeChanged += (_, _) => ApplyResponsiveScale();
        PreviewKeyDown += MainWindow_OnPreviewKeyDown;
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    public void ConfigurePreview(IReadOnlyList<RunnerSnapshot> snapshots)
    {
        ArgumentNullException.ThrowIfNull(snapshots);
        ConfigurePreview(new RunnerRuntimeReport(snapshots, Array.Empty<RunnerSystemFinding>()));
    }

    public void ConfigurePreview(RunnerRuntimeReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        _previewMode = true;
        _dashboard.ApplyRuntimeReport(report);
        MachineValue.Text = "MAIN-PC";
        RootValue.Text = MrcConstants.RunnerRoot;
        BoundaryValue.Text = "AUTHORIZED";
        BoundaryValue.Foreground = BrushFromHex("#39E58C");
        BoundaryDot.Fill = BrushFromHex("#39E58C");
        SetBoundaryDetails($"Deterministic v{BuildInfo.Version} visual preview data — controls shown but runtime operation disabled.");
        RefreshStatusValue.Text = $"v{BuildInfo.Version} preview • {_dashboard.TotalCount} runners • operations disabled in render mode";
        OperationsPanel.IsEnabled = false;
        RunnerList.IsHitTestVisible = false;
        _animationClock.Tick(DateTimeOffset.UtcNow, _dashboard.Rows);
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyResponsiveScale();
        if (_previewMode) return;

        var fence = EnvironmentFence.EvaluateCurrent();
        RenderBoundary(fence);

        if (fence.IsAuthorized)
        {
            try
            {
                _engine = new RunnerEngine();
                _operations = new RunnerOperationsService(_engine);
                OperationsPanel.IsEnabled = true;
                RunnerList.IsHitTestVisible = true;
                await RefreshDashboardAsync();
                _refreshTimer.Start();
                _animationTimer.Start();
            }
            catch (Exception ex)
            {
                _engine = null;
                _operations = null;
                OperationsPanel.IsEnabled = false;
                RunnerList.IsHitTestVisible = false;
                RefreshStatusValue.Text = $"Engine unavailable • {ex.Message}";
                RefreshStatusValue.Foreground = BrushFromHex("#FF8A3D");
            }
        }
        else
        {
            _dashboard.ApplySnapshots(Array.Empty<RunnerSnapshot>());
            OperationsPanel.IsEnabled = false;
            RunnerList.IsHitTestVisible = false;
            RefreshStatusValue.Text = $"CONTROL BLOCKED • {fence.Code}";
            RefreshStatusValue.Foreground = BrushFromHex("#FF8A3D");
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _refreshTimer.Stop();
        _animationTimer.Stop();
    }

    private void ApplyResponsiveScale()
    {
        if (ActualWidth <= 0 || ActualHeight <= 0) return;

        var scale = Math.Clamp(
            Math.Min(ActualWidth / 1200d, ActualHeight / 760d),
            1d,
            1.5d);

        if (RootSurface.LayoutTransform is ScaleTransform current
            && Math.Abs(current.ScaleX - scale) < 0.001
            && Math.Abs(current.ScaleY - scale) < 0.001)
        {
            return;
        }

        RootSurface.LayoutTransform = new ScaleTransform(scale, scale);
    }

    private void MainWindow_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (SearchBox.IsKeyboardFocusWithin || Keyboard.Modifiers != ModifierKeys.None) return;
        if (e.Key is not Key.OemQuestion and not Key.Divide) return;

        SearchBox.Focus();
        SearchBox.SelectAll();
        e.Handled = true;
    }

    private void ApplyRunnerScrollBarStyle()
    {
        if (Application.Current?.TryFindResource("DarkScrollBarStyle") is not Style style) return;
        ApplyRunnerScrollBarStyleRecursive(RunnerList, style);
    }

    private static void ApplyRunnerScrollBarStyleRecursive(DependencyObject root, Style style)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is ScrollBar scrollBar && scrollBar.Orientation == Orientation.Vertical)
            {
                scrollBar.Style = style;
            }
            ApplyRunnerScrollBarStyleRecursive(child, style);
        }
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
        if (_engine is null || _refreshInProgress || _previewMode || _operationInProgress) return;

        _refreshInProgress = true;
        try
        {
            RefreshStatusValue.Text = "Refreshing runner state…";
            RefreshStatusValue.Foreground = BrushFromHex("#AAB6C3");
            var report = await Task.Run(() => _engine.RefreshReport());
            _dashboard.ApplyRuntimeReport(report);
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

    private async void TurnAllOnButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_previewMode || _operations is null || _operationInProgress) return;

        RunnerBulkOperationResult result;
        BeginOperation("TURN ALL ON • starting verified OFF runners…");
        try
        {
            result = await _operations.TurnAllOnAsync();
        }
        catch (Exception ex)
        {
            RenderOperationException("TURN ALL ON", ex);
            return;
        }
        finally
        {
            EndOperation();
        }

        await RefreshDashboardAsync();
        RenderBulkResult("TURN ALL ON", result);
    }

    private async void TurnAllOffButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_previewMode || _operations is null || _operationInProgress) return;

        var idleCount = _dashboard.Rows.Count(row => row.State == RunnerState.IDLE);
        var busyCount = _dashboard.Rows.Count(row => row.State == RunnerState.BUSY);
        var confirmation = MessageBox.Show(
            this,
            $"Stop {idleCount} IDLE runner(s)?\n\n{busyCount} BUSY runner(s) will remain running.\n\nTURN ALL OFF never force-stops BUSY runners.",
            "Confirm TURN ALL OFF",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);
        if (confirmation != MessageBoxResult.Yes) return;

        RunnerBulkOperationResult result;
        BeginOperation("TURN ALL OFF • stopping verified IDLE runners…");
        try
        {
            result = await Task.Run(() => _operations.TurnAllOff());
        }
        catch (Exception ex)
        {
            RenderOperationException("TURN ALL OFF", ex);
            return;
        }
        finally
        {
            EndOperation();
        }

        await RefreshDashboardAsync();
        RenderBulkResult("TURN ALL OFF", result);
    }

    private async void RunnerPrimaryControl_OnClick(object sender, RoutedEventArgs e)
    {
        if (_previewMode || _operations is null || _operationInProgress) return;
        if (sender is not Button { CommandParameter: RunnerRowViewModel row }) return;

        if (row.State == RunnerState.ERROR)
        {
            var dialog = new DiagnosticDialog(row) { Owner = this };
            dialog.ShowDialog();
            return;
        }

        if (row.State is not RunnerState.OFF and not RunnerState.IDLE) return;

        RunnerControlResult result;
        BeginOperation($"{row.RunnerName} • applying verified control…");
        try
        {
            if (row.State == RunnerState.OFF)
            {
                result = await Task.Run(() => _operations.Start(row.Runner));
            }
            else
            {
                result = await Task.Run(() => _operations.StopIdle(row.Runner));
            }
        }
        catch (Exception ex)
        {
            RenderOperationException(row.RunnerName, ex);
            return;
        }
        finally
        {
            EndOperation();
        }

        await RefreshDashboardAsync();
        RenderControlResult(row, result);
    }

    private async void RunnerMoreControl_OnClick(object sender, RoutedEventArgs e)
    {
        if (_previewMode || _operations is null || _operationInProgress) return;
        if (sender is not Button { CommandParameter: RunnerRowViewModel row }) return;
        if (row.State != RunnerState.BUSY) return;

        RunnerControlResult preflight;
        try
        {
            preflight = await Task.Run(() => _operations.ForceStopBusy(row.Runner, confirmed: false));
        }
        catch (Exception ex)
        {
            RenderOperationException(row.RunnerName, ex);
            return;
        }

        if (preflight.Outcome != RunnerControlOutcome.ConfirmationRequired)
        {
            await RefreshDashboardAsync();
            RenderControlResult(row, preflight);
            return;
        }

        var confirmation = MessageBox.Show(
            this,
            $"Force-stop BUSY runner '{row.RunnerName}'?\n\nThis can interrupt an active GitHub Actions job. Any in-progress job may fail immediately.\n\nUse this only when interruption is intentional.",
            "Force-stop BUSY runner",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);
        if (confirmation != MessageBoxResult.Yes) return;

        RunnerControlResult result;
        BeginOperation($"{row.RunnerName} • force-stop requested…");
        try
        {
            result = await Task.Run(() => _operations.ForceStopBusy(row.Runner, confirmed: true));
        }
        catch (Exception ex)
        {
            RenderOperationException(row.RunnerName, ex);
            return;
        }
        finally
        {
            EndOperation();
        }

        await RefreshDashboardAsync();
        RenderControlResult(row, result);
    }

    private void BeginOperation(string message)
    {
        _operationInProgress = true;
        _refreshTimer.Stop();
        OperationsPanel.IsEnabled = false;
        RunnerList.IsHitTestVisible = false;
        ManualRefreshButton.IsEnabled = false;
        RefreshStatusValue.Text = message;
        RefreshStatusValue.Foreground = BrushFromHex("#AAB6C3");
    }

    private void EndOperation()
    {
        _operationInProgress = false;
        var canOperate = !_previewMode && _operations is not null;
        OperationsPanel.IsEnabled = canOperate;
        RunnerList.IsHitTestVisible = canOperate;
        ManualRefreshButton.IsEnabled = true;
        if (canOperate) _refreshTimer.Start();
    }

    private void RenderControlResult(RunnerRowViewModel row, RunnerControlResult result)
    {
        RefreshStatusValue.Text = $"{row.RunnerName} • {result.Message}";
        RefreshStatusValue.Foreground = result.Outcome switch
        {
            RunnerControlOutcome.Error => BrushFromHex("#FF8A3D"),
            RunnerControlOutcome.BusyProtected or RunnerControlOutcome.ConfirmationRequired => BrushFromHex("#FFD166"),
            RunnerControlOutcome.Starting or RunnerControlOutcome.Stopping or RunnerControlOutcome.ForceStopping => BrushFromHex("#39E58C"),
            _ => BrushFromHex("#AAB6C3")
        };
    }

    private void RenderBulkResult(string operation, RunnerBulkOperationResult result)
    {
        RefreshStatusValue.Text = $"{operation} • {result.Succeeded}/{result.Attempted} succeeded • {result.BusySkipped} BUSY skipped • {result.Errors} errors • {result.Skipped} total skipped";
        RefreshStatusValue.Foreground = result.Errors > 0 ? BrushFromHex("#FF8A3D") : BrushFromHex("#39E58C");
    }

    private void RenderOperationException(string operation, Exception ex)
    {
        RefreshStatusValue.Text = $"{operation} error • {ex.Message}";
        RefreshStatusValue.Foreground = BrushFromHex("#FF8A3D");
    }

    private static Brush BrushFromHex(string value) =>
        (Brush)new BrushConverter().ConvertFromString(value)!;
}
