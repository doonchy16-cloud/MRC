using System.Globalization;
using System.Text.Json;

namespace MRC.Core.Runners;

internal static class RunnerIdentityParser
{
    public static RunnerDescriptor Parse(string runnerDirectory)
    {
        string? agentName = null;
        string? gitHubUrl = null;
        string? repositoryName = null;
        long? agentId = null;
        string? workFolder = null;

        try
        {
            var metadataPath = Path.Combine(runnerDirectory, ".runner");
            using var document = JsonDocument.Parse(File.ReadAllText(metadataPath));
            var root = document.RootElement;

            agentName = GetOptionalString(root, "agentName");
            gitHubUrl = GetOptionalString(root, "gitHubUrl");
            agentId = GetOptionalLong(root, "agentId");
            workFolder = GetOptionalString(root, "workFolder");
            repositoryName = DeriveRepositoryName(gitHubUrl);

            if (string.IsNullOrWhiteSpace(agentName))
            {
                throw new InvalidDataException(".runner is missing agentName.");
            }

            if (string.IsNullOrWhiteSpace(gitHubUrl) || string.IsNullOrWhiteSpace(repositoryName))
            {
                throw new InvalidDataException(".runner is missing a usable gitHubUrl.");
            }

            return new RunnerDescriptor(
                RunnerPath.Normalize(runnerDirectory),
                agentName,
                gitHubUrl,
                repositoryName,
                agentId,
                workFolder,
                null);
        }
        catch (Exception ex) when (ex is IOException or JsonException or InvalidDataException or UnauthorizedAccessException)
        {
            return new RunnerDescriptor(
                RunnerPath.Normalize(runnerDirectory),
                agentName,
                gitHubUrl,
                repositoryName,
                agentId,
                workFolder,
                $"Runner identity could not be parsed safely: {ex.Message}");
        }
    }

    internal static string? DeriveRepositoryName(string? gitHubUrl)
    {
        if (string.IsNullOrWhiteSpace(gitHubUrl))
        {
            return null;
        }

        var trimmed = gitHubUrl.Trim().TrimEnd('/', '\\');
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            return segments.Length == 0 ? null : Uri.UnescapeDataString(segments[^1]);
        }

        var fallback = trimmed.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        return fallback.Length == 0 ? null : fallback[^1];
    }

    private static string? GetOptionalString(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static long? GetOptionalLong(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var number))
        {
            return number;
        }

        if (value.ValueKind == JsonValueKind.String &&
            long.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
        {
            return number;
        }

        return null;
    }
}
