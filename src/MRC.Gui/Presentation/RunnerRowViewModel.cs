using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using MRC.Core.Runners;
using MRC.Core.Runtime;

namespace MRC.Gui.Presentation;

public sealed class RunnerRowViewModel : INotifyPropertyChanged
{
    private RunnerDescriptor _runner;
    private RunnerState _state;
    private string? _error;
    private string _glyph;

    public RunnerRowViewModel(RunnerSnapshot snapshot)
    {
        _runner = snapshot.Runner;
        _state = snapshot.State;
        _error = snapshot.Error;
        _glyph = StaticGlyph(snapshot.State);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string DirectoryPath => _runner.DirectoryPath;
    public string RunnerName => string.IsNullOrWhiteSpace(_runner.AgentName) ? Path.GetFileName(_runner.DirectoryPath) : _runner.AgentName!;
    public string RepositoryName => string.IsNullOrWhiteSpace(_runner.RepositoryName) ? "UNKNOWN" : _runner.RepositoryName!;
    public string GitHubUrl => _runner.GitHubUrl ?? string.Empty;
    public RunnerDescriptor Runner => _runner;
    public RunnerState State => _state;
    public string StateText => _state.ToString();
    public string? Error => _error;

    public string Glyph
    {
        get => _glyph;
        internal set
        {
            if (_glyph == value) return;
            _glyph = value;
            OnPropertyChanged();
        }
    }

    public void Update(RunnerSnapshot snapshot)
    {
        var identityChanged = _runner != snapshot.Runner;
        var stateChanged = _state != snapshot.State;
        var errorChanged = !string.Equals(_error, snapshot.Error, StringComparison.Ordinal);
        _runner = snapshot.Runner;
        _state = snapshot.State;
        _error = snapshot.Error;

        if (identityChanged)
        {
            OnPropertyChanged(nameof(DirectoryPath));
            OnPropertyChanged(nameof(RunnerName));
            OnPropertyChanged(nameof(RepositoryName));
            OnPropertyChanged(nameof(GitHubUrl));
            OnPropertyChanged(nameof(Runner));
        }
        if (stateChanged)
        {
            Glyph = StaticGlyph(_state);
            OnPropertyChanged(nameof(State));
            OnPropertyChanged(nameof(StateText));
        }
        if (errorChanged) OnPropertyChanged(nameof(Error));
    }

    private static string StaticGlyph(RunnerState state) => state switch
    {
        RunnerState.OFF => "-",
        RunnerState.ERROR => "!",
        _ => "/"
    };

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
