using System.Collections.Concurrent;
using MRC.Core.Runners;

namespace MRC.Core.Runtime;

internal enum RunnerTransitionKind
{
    Starting,
    Stopping,
    Error
}

internal sealed record RunnerTransition(
    RunnerTransitionKind Kind,
    DateTimeOffset RequestedAt,
    DateTimeOffset ExpiresAt,
    string? Error);

internal sealed class RunnerTransitionTracker
{
    internal static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
    private readonly ConcurrentDictionary<string, RunnerTransition> _transitions = new(StringComparer.OrdinalIgnoreCase);

    public RunnerTransition? Get(string runnerDirectory) =>
        _transitions.TryGetValue(Key(runnerDirectory), out var transition) ? transition : null;

    public void MarkStarting(string runnerDirectory, DateTimeOffset now) =>
        _transitions[Key(runnerDirectory)] = new RunnerTransition(
            RunnerTransitionKind.Starting,
            now,
            now.Add(DefaultTimeout),
            null);

    public void MarkStopping(string runnerDirectory, DateTimeOffset now) =>
        _transitions[Key(runnerDirectory)] = new RunnerTransition(
            RunnerTransitionKind.Stopping,
            now,
            now.Add(DefaultTimeout),
            null);

    public void MarkError(string runnerDirectory, DateTimeOffset now, string error) =>
        _transitions[Key(runnerDirectory)] = new RunnerTransition(
            RunnerTransitionKind.Error,
            now,
            DateTimeOffset.MaxValue,
            error);

    public void Clear(string runnerDirectory) =>
        _transitions.TryRemove(Key(runnerDirectory), out _);

    private static string Key(string runnerDirectory) => RunnerPath.Normalize(runnerDirectory);
}
