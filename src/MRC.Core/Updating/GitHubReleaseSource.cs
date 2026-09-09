using System.Net.Http.Headers;
using System.Text.Json;

namespace MRC.Core.Updating;

public sealed class GitHubReleaseSource : IUpdateReleaseSource
{
    private static readonly Uri ReleasesEndpoint = new("https://api.github.com/repos/doonchy16-cloud/MRC/releases?per_page=30");
    private readonly HttpClient _httpClient;

    public GitHubReleaseSource(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<UpdateRelease> ResolveLatestAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, ReleasesEndpoint);
        request.Headers.UserAgent.ParseAdd($"MRC/{BuildInfo.Version}");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException("GitHub releases collection did not return an array.");
        }

        var releases = new List<UpdateRelease>();
        foreach (var root in document.RootElement.EnumerateArray())
        {
            if (root.TryGetProperty("draft", out var draft) && draft.ValueKind == JsonValueKind.True) continue;

            var tagName = root.TryGetProperty("tag_name", out var tagElement)
                ? tagElement.GetString()
                : null;
            if (string.IsNullOrWhiteSpace(tagName)) continue;

            var rawVersion = tagName.Trim().TrimStart('v', 'V');
            if (!Version.TryParse(rawVersion, out var version) || !ReleaseAuthority.IsAllowedVersion(version)) continue;

            var assets = new List<UpdateAsset>();
            if (root.TryGetProperty("assets", out var assetsElement) && assetsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in assetsElement.EnumerateArray())
                {
                    var name = element.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : null;
                    var download = element.TryGetProperty("browser_download_url", out var downloadElement) ? downloadElement.GetString() : null;
                    if (string.IsNullOrWhiteSpace(name) || !Uri.TryCreate(download, UriKind.Absolute, out var uri)) continue;
                    assets.Add(new UpdateAsset(name, uri));
                }
            }

            var isPrerelease = root.TryGetProperty("prerelease", out var prerelease)
                               && prerelease.ValueKind == JsonValueKind.True;
            releases.Add(new UpdateRelease(tagName, version, assets, isPrerelease));
        }

        return releases
            .OrderByDescending(release => release.Version)
            .FirstOrDefault()
            ?? throw new InvalidDataException("No authorized MRC release was found in the GitHub releases collection.");
    }

    public async Task<byte[]> DownloadAssetAsync(UpdateAsset asset, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(asset);
        using var request = new HttpRequestMessage(HttpMethod.Get, asset.DownloadUrl);
        request.Headers.UserAgent.ParseAdd($"MRC/{BuildInfo.Version}");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }
}
