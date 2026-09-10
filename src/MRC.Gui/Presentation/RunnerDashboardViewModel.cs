using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MRC.Core.Runtime;

namespace MRC.Gui.Presentation;

public sealed class RunnerDashboardViewModel : INotifyPropertyChanged
{
    private string _searchText = string.Empty;
    private RunnerFilter _selectedFilter = RunnerFilter.ALL;
    private int _totalCount;
    private int _idleCount;
    private int _busyCount;
    private int _offCount;
    private int _errorCount;
    private int _transitionCount;
    private int _cardColumnCount = 2;
    private bool _hasSystemFindings;
    private string _systemFindingsSummary = "No external or unattributed runner processes observed.";

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<RunnerRowViewModel> Rows { get; } = new();
    public ObservableCollection<RunnerRowViewModel> VisibleRows { get; } = new();
    public ObservableCollection<RunnerSystemFinding> SystemFindings { get; } = new();

    public string SearchText
    {
        get => _searchText;
        set
        {
            var normalized = value ?? string.Empty;
            if (_searchText == normalized) return;
            _searchText = normalized;
            OnPropertyChanged();
            RefreshVisibleRows();
        }
    }

    public RunnerFilter SelectedFilter
    {
        get => _selectedFilter;
        set
        {
            if (_selectedFilter == value) return;
            _selectedFilter = value;
            OnPropertyChanged();
            RefreshVisibleRows();
        }
    }

    public int TotalCount { get => _totalCount; private set => Set(ref _totalCount, value); }
    public int IdleCount { get => _idleCount; private set => Set(ref _idleCount, value); }
    public int BusyCount { get => _busyCount; private set => Set(ref _busyCount, value); }
    public int OffCount { get => _offCount; private set => Set(ref _offCount, value); }
    public int ErrorCount { get => _errorCount; private set => Set(ref _errorCount, value); }
    public int TransitionCount { get => _transitionCount; private set => Set(ref _transitionCount, value); }

    public int CardColumnCount
    {
        get => _cardColumnCount;
        set => Set(ref _cardColumnCount, Math.Clamp(value, 1, 3));
    }

    public bool HasSystemFindings
    {
        get => _hasSystemFindings;
        private set
        {
            if (_hasSystemFindings == value) return;
            _hasSystemFindings = value;
            OnPropertyChanged();
        }
    }

    public string SystemFindingsSummary
    {
        get => _systemFindingsSummary;
        private set
        {
            if (string.Equals(_systemFindingsSummary, value, StringComparison.Ordinal)) return;
            _systemFindingsSummary = value;
            OnPropertyChanged();
        }
    }

    public void ApplyRuntimeReport(RunnerRuntimeReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        ApplySnapshots(report.ManagedRunners);

        SystemFindings.Clear();
        foreach (var finding in report.SystemFindings
                     .OrderBy(finding => finding.Kind)
                     .ThenBy(finding => finding.ProcessId))
        {
            SystemFindings.Add(finding);
        }

        HasSystemFindings = SystemFindings.Count > 0;
        SystemFindingsSummary = BuildSystemFindingsSummary(SystemFindings);
    }

    public void ApplySnapshots(IReadOnlyList<RunnerSnapshot> snapshots)
    {
        var existing = Rows.ToDictionary(row => Normalize(row.DirectoryPath), StringComparer.OrdinalIgnoreCase);
        var desired = new List<RunnerRowViewModel>(snapshots.Count);

        foreach (var snapshot in snapshots)
        {
            var key = Normalize(snapshot.Runner.DirectoryPath);
            if (existing.TryGetValue(key, out var row))
            {
                row.Update(snapshot);
                desired.Add(row);
            }
            else
            {
                desired.Add(new RunnerRowViewModel(snapshot));
            }
        }

        desired.Sort(static (left, right) =>
        {
            var repository = StringComparer.OrdinalIgnoreCase.Compare(left.RepositoryName, right.RepositoryName);
            if (repository != 0) return repository;
            var runner = StringComparer.OrdinalIgnoreCase.Compare(left.RunnerName, right.RunnerName);
            if (runner != 0) return runner;
            return StringComparer.OrdinalIgnoreCase.Compare(left.DirectoryPath, right.DirectoryPath);
        });

        Synchronize(Rows, desired);
        RecalculateCounters();
        RefreshVisibleRows();
    }

    private void RecalculateCounters()
    {
        TotalCount = Rows.Count;
        IdleCount = Rows.Count(row => row.State == RunnerState.IDLE);
        BusyCount = Rows.Count(row => row.State == RunnerState.BUSY);
        OffCount = Rows.Count(row => row.State == RunnerState.OFF);
        ErrorCount = Rows.Count(row => row.State == RunnerState.ERROR);
        TransitionCount = Rows.Count(row => row.State is RunnerState.STARTING or RunnerState.STOPPING);
    }

    private void RefreshVisibleRows()
    {
        var desired = Rows.Where(IsVisible).ToList();
        Synchronize(VisibleRows, desired);
    }

    private bool IsVisible(RunnerRowViewModel row)
    {
        if (_selectedFilter != RunnerFilter.ALL)
        {
            var state = _selectedFilter switch
            {
                RunnerFilter.IDLE => RunnerState.IDLE,
                RunnerFilter.BUSY => RunnerState.BUSY,
                RunnerFilter.OFF => RunnerState.OFF,
                RunnerFilter.ERROR => RunnerState.ERROR,
                _ => throw new InvalidOperationException($"Unsupported filter: {_selectedFilter}")
            };
            if (row.State != state) return false;
        }

        var term = _searchText.Trim();
        if (term.Length == 0) return true;
        return row.RunnerName.Contains(term, StringComparison.OrdinalIgnoreCase)
            || row.RepositoryName.Contains(term, StringComparison.OrdinalIgnoreCase)
            || row.DirectoryPath.Contains(term, StringComparison.OrdinalIgnoreCase)
            || row.GitHubUrl.Contains(term, StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildSystemFindingsSummary(IReadOnlyCollection<RunnerSystemFinding> findings)
    {
        if (findings.Count == 0)
        {
            return "No external or unattributed runner processes observed.";
        }

        var external = findings.Count(finding => finding.Kind == RunnerSystemFindingKind.External);
        var unattributed = findings.Count(finding => finding.Kind == RunnerSystemFindingKind.Unattributed);
        var detail = string.Join(" ; ", findings.Select(finding =>
            $"{finding.Kind.ToString().ToUpperInvariant()} PID {finding.ProcessId} {finding.ProcessName} // PARENT {Value(finding.ParentProcessId)} // SESSION {Value(finding.SessionId)} // {finding.Message}"));
        return $"EXTERNAL {external} | UNATTRIBUTED {unattributed} // {detail}";
    }

    private static string Value(int? value) => value?.ToString() ?? "UNKNOWN";

    private static void Synchronize(ObservableCollection<RunnerRowViewModel> target, IReadOnlyList<RunnerRowViewModel> desired)
    {
        var desiredSet = desired.ToHashSet();
        for (var index = target.Count - 1; index >= 0; index--)
        {
            if (!desiredSet.Contains(target[index])) target.RemoveAt(index);
        }

        for (var desiredIndex = 0; desiredIndex < desired.Count; desiredIndex++)
        {
            var item = desired[desiredIndex];
            if (desiredIndex < target.Count && ReferenceEquals(target[desiredIndex], item)) continue;

            var existingIndex = target.IndexOf(item);
            if (existingIndex >= 0)
            {
                target.Move(existingIndex, desiredIndex);
            }
            else
            {
                target.Insert(desiredIndex, item);
            }
        }
    }

    private static string Normalize(string path) => path.Trim().TrimEnd('\\', '/').Replace('/', '\\');

    private void Set(ref int field, int value, [CallerMemberName] string? propertyName = null)
    {
        if (field == value) return;
        field = value;
        OnPropertyChanged(propertyName);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
