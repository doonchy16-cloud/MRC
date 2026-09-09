using System.Net.Http.Headers;
using System.Text.Json;

namespace MRC.Core.Updating;

public sealed class GitHubReleaseSource : IUpdateReleaseSource
{
    private static readonly Uri LatestReleaseEndpoint = new("https://api.github.com/repos/doonchy16-cloud/MRC/releases/latest");
    private readonly HttpClient _httpClient;

    public GitHubReleaseSource(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<UpdateRelease> ResolveLatestAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, LatestReleaseEndpoint);
        request.Headers.UserAgent.ParseAdd("MRC/0.1.0");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;
        var tagName = root.GetProperty("tag_name").GetString()
            ?? throw new InvalidDataException("Latest GitHub release omitted tag_name.");
        var rawVersion = tagName.Trim().TrimStart('v', 'V');
        if (!Version.TryParse(rawVersion, out var version))
        {
            throw new InvalidDataException($"Latest GitHub release tag '{tagName}' is not a valid MRC version.");
        }

        var assets = new List<UpdateAsset>();
        foreach (var element in root.GetProperty("assets").EnumerateArray())
        {
            var name = element.GetProperty("name").GetString();
            var download = element.GetProperty("browser_download_url").GetString();
            if (string.IsNullOrWhiteSpace(name) || !Uri.TryCreate(download, UriKind.Absolute, out var uri)) continue;
            assets.Add(new UpdateAsset(name, uri));
        }

        return new UpdateRelease(tagName, version, assets);
    }

    public async Task<byte[]> DownloadAssetAsync(UpdateAsset asset, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(asset);
        using var request = new HttpRequestMessage(HttpMethod.Get, asset.DownloadUrl);
        request.Headers.UserAgent.ParseAdd("MRC/0.1.0");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }
}
