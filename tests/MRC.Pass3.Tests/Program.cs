using MRC.Core.Runners;
using MRC.Core.Runtime;
using MRC.Gui.Presentation;

namespace MRC.Pass3.Tests;

internal static class Program
{
    private static int Main()
    {
        var animationFailures = AnimationAcceptance.Run();

        var tests = new (string Name, Action Body)[]
        {
            ("dashboard sorts by repository then runner name", StableSort),
            ("search matches runner repository and folder", SearchCoverage),
            ("canonical filters expose intended states", CanonicalFilters),
            ("state counters describe all current runners", Counters),
            ("refresh reuses row objects for stable identity", StableRowIdentity),
            ("state changes never reorder rows", StateChangeDoesNotReorder),
            ("vanished runners are removed cleanly", VanishedRunnerRemoval),
            ("row presentation exposes truthful state text", TruthfulStateText)
        };

        var failures = 0;
        Console.WriteLine($"MRC PASS 3 presentation harness — {tests.Length} tests");
        foreach (var test in tests)
        {
            try
            {
                test.Body();
                Console.WriteLine($"PASS  {test.Name}");
            }
            catch (Exception ex)
            {
                failures++;
                Console.WriteLine($"FAIL  {test.Name}");
                Console.WriteLine($"      {ex.Message}");
            }
        }

        Console.WriteLine();
        Console.WriteLine(failures == 0
            ? $"PASS  all {tests.Length} PASS 3 presentation tests"
            : $"FAIL  {failures} of {tests.Length} PASS 3 presentation tests");
        return failures == 0 && animationFailures == 0 ? 0 : 1;
    }

    private static void StableSort()
    {
        var vm = new RunnerDashboardViewModel();
        vm.ApplySnapshots(new[]
        {
            Snap(@"C:\R\z", "Zulu", "Beta", RunnerState.IDLE),
            Snap(@"C:\R\b", "Bravo", "Alpha", RunnerState.OFF),
            Snap(@"C:\R\a", "Alpha", "Alpha", RunnerState.BUSY)
        });

        Require(string.Join("|", vm.Rows.Select(r => r.RunnerName)) == "Alpha|Bravo|Zulu",
            "Rows were not sorted repository then runner name.");
    }

    private static void SearchCoverage()
    {
        var vm = Seed();

        vm.SearchText = "atlas";
        Require(vm.VisibleRows.Count == 1 && vm.VisibleRows[0].RunnerName == "Build_A", "Runner-name search failed.");

        vm.SearchText = "project-beta";
        Require(vm.VisibleRows.Count == 1 && vm.VisibleRows[0].RepositoryName == "Project-Beta", "Repository search failed.");

        vm.SearchText = "folder-two";
        Require(vm.VisibleRows.Count == 1 && vm.VisibleRows[0].DirectoryPath.Contains("folder-two", StringComparison.OrdinalIgnoreCase), "Folder search failed.");
    }

    private static void CanonicalFilters()
    {
        var vm = new RunnerDashboardViewModel();
        vm.ApplySnapshots(new[]
        {
            Snap(@"C:\R\off", "Off", "Repo", RunnerState.OFF),
            Snap(@"C:\R\idle", "Idle", "Repo", RunnerState.IDLE),
            Snap(@"C:\R\busy", "Busy", "Repo", RunnerState.BUSY),
            Snap(@"C:\R\error", "Error", "Repo", RunnerState.ERROR),
            Snap(@"C:\R\starting", "Starting", "Repo", RunnerState.STARTING),
            Snap(@"C:\R\stopping", "Stopping", "Repo", RunnerState.STOPPING)
        });

        var expected = new Dictionary<RunnerFilter, RunnerState?>
        {
            [RunnerFilter.ALL] = null,
            [RunnerFilter.IDLE] = RunnerState.IDLE,
            [RunnerFilter.BUSY] = RunnerState.BUSY,
            [RunnerFilter.OFF] = RunnerState.OFF,
            [RunnerFilter.ERROR] = RunnerState.ERROR
        };

        Require(Enum.GetNames<RunnerFilter>().SequenceEqual(new[] { "ALL", "IDLE", "BUSY", "OFF", "ERROR" }),
            "RunnerFilter must expose exactly the five canonical filters.");

        foreach (var pair in expected)
        {
            vm.SelectedFilter = pair.Key;
            if (pair.Value is null)
            {
                Require(vm.VisibleRows.Count == 6, "ALL did not include every runtime state.");
            }
            else
            {
                Require(vm.VisibleRows.Count == 1 && vm.VisibleRows[0].State == pair.Value,
                    $"Filter {pair.Key} returned incorrect rows.");
            }
        }
    }

    private static void Counters()
    {
        var vm = new RunnerDashboardViewModel();
        vm.ApplySnapshots(new[]
        {
            Snap(@"C:\R\1", "A", "Repo", RunnerState.OFF),
            Snap(@"C:\R\2", "B", "Repo", RunnerState.IDLE),
            Snap(@"C:\R\3", "C", "Repo", RunnerState.IDLE),
            Snap(@"C:\R\4", "D", "Repo", RunnerState.BUSY),
            Snap(@"C:\R\5", "E", "Repo", RunnerState.ERROR),
            Snap(@"C:\R\6", "F", "Repo", RunnerState.STARTING)
        });

        Require(vm.TotalCount == 6, "Total counter is wrong.");
        Require(vm.IdleCount == 2, "IDLE counter is wrong.");
        Require(vm.BusyCount == 1, "BUSY counter is wrong.");
        Require(vm.OffCount == 1, "OFF counter is wrong.");
        Require(vm.ErrorCount == 1, "ERROR counter is wrong.");
        Require(vm.TransitionCount == 1, "Transition counter is wrong.");
    }

    private static void StableRowIdentity()
    {
        var vm = new RunnerDashboardViewModel();
        vm.ApplySnapshots(new[] { Snap(@"C:\R\same", "Agent", "Repo", RunnerState.OFF) });
        var original = vm.Rows.Single();
        vm.ApplySnapshots(new[] { Snap(@"C:\R\same", "Agent", "Repo", RunnerState.IDLE) });
        Require(ReferenceEquals(original, vm.Rows.Single()), "Refresh recreated a row instead of updating it in place.");
        Require(original.State == RunnerState.IDLE, "Reused row did not receive the new state.");
    }

    private static void StateChangeDoesNotReorder()
    {
        var vm = new RunnerDashboardViewModel();
        vm.ApplySnapshots(new[]
        {
            Snap(@"C:\R\a", "A", "Alpha", RunnerState.BUSY),
            Snap(@"C:\R\b", "B", "Beta", RunnerState.OFF)
        });
        var before = vm.Rows.Select(r => r.DirectoryPath).ToArray();
        vm.ApplySnapshots(new[]
        {
            Snap(@"C:\R\a", "A", "Alpha", RunnerState.OFF),
            Snap(@"C:\R\b", "B", "Beta", RunnerState.BUSY)
        });
        Require(before.SequenceEqual(vm.Rows.Select(r => r.DirectoryPath)), "State change reordered rows.");
    }

    private static void VanishedRunnerRemoval()
    {
        var vm = Seed();
        vm.ApplySnapshots(new[] { Snap(@"C:\R\folder-one", "Build_A", "Project-Alpha", RunnerState.IDLE) });
        Require(vm.Rows.Count == 1 && vm.Rows[0].RunnerName == "Build_A", "Vanished runner remained in dashboard.");
    }

    private static void TruthfulStateText()
    {
        var vm = new RunnerDashboardViewModel();
        foreach (var state in Enum.GetValues<RunnerState>())
        {
            vm.ApplySnapshots(new[] { Snap(@"C:\R\one", "Agent", "Repo", state) });
            Require(vm.Rows.Single().StateText == state.ToString(), $"State text lied for {state}.");
        }
    }

    private static RunnerDashboardViewModel Seed()
    {
        var vm = new RunnerDashboardViewModel();
        vm.ApplySnapshots(new[]
        {
            Snap(@"C:\R\folder-one", "Build_A", "Project-Alpha", RunnerState.IDLE, github: "https://github.com/x/atlas"),
            Snap(@"C:\R\folder-two", "Build_B", "Project-Beta", RunnerState.BUSY)
        });
        return vm;
    }

    private static RunnerSnapshot Snap(string path, string agent, string repository, RunnerState state, string? github = null) =>
        new(new RunnerDescriptor(path, agent, github ?? $"https://github.com/x/{repository}", repository, 1, "_work", null), state, state == RunnerState.ERROR ? "error" : null);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
