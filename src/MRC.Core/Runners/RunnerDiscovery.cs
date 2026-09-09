namespace MRC.Core.Runners;

internal static class RunnerDiscovery
{
    public static IReadOnlyList<RunnerDescriptor> Discover(string root)
    {
        if (!Directory.Exists(root))
        {
            return Array.Empty<RunnerDescriptor>();
        }

        return Directory
            .EnumerateDirectories(root, "*", SearchOption.TopDirectoryOnly)
            .Where(RunnerPath.HasSignature)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(RunnerIdentityParser.Parse)
            .ToArray();
    }
}
