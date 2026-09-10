using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using MRC.Core;
using MRC.Core.Updating;

internal static class Task5UpdateContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifyReleaseAuthority();
        VerifyLegacyBootstrapCompatibility();
        VerifyReleaseCollectionSelection();
        VerifyProgressContract();
        Console.WriteLine("PASS  Task5 release-stage/update contract");
    }

    private static void VerifyReleaseAuthority()
    {
        var core = typeof(BuildInfo).Assembly;
        var stage = core.GetType("MRC.Core.ReleaseStage");
        Require(stage is not null && stage.IsEnum, "ReleaseStage is missing.");
        Require(Enum.GetNames(stage!).SequenceEqual(new[] { "PreCertification", "Final" }),
            "ReleaseStage must be PreCertification/Final.");

        var authority = core.GetType("MRC.Core.ReleaseAuthority");
        Require(authority is not null, "ReleaseAuthority is missing.");
        var current = authority!.GetProperty("Current", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
        Require(current is not null, "ReleaseAuthority.Current is missing.");
        var currentType = current!.GetType();
        foreach (var property in new[] { "Version", "Channel", "Stage", "FinalTarget" })
        {
            Require(currentType.GetProperty(property) is not null, $"ReleaseInfo.{property} is missing.");
        }
        Require(currentType.GetProperty("Version")!.GetValue(current)?.ToString() == "0.0.12",
            $"Redesign build must identify as 0.0.12, got {currentType.GetProperty("Version")!.GetValue(current)}.");
        Require(currentType.GetProperty("Stage")!.GetValue(current)?.ToString() == "PreCertification",
            "v0.0.12 must report PreCertification stage.");
        Require(currentType.GetProperty("FinalTarget")!.GetValue(current)?.ToString() == "0.1.0",
            "Final target must remain v0.1.0.");
    }

    private static void VerifyLegacyBootstrapCompatibility()
    {
        var bootstrap = new Version(0, 0, 12);
        Require(ReleaseAuthority.ChannelFor(bootstrap) == "precert",
            "Installed v0.0.12 application authority must remain precert.");
        Require(ReleaseAuthority.ManifestCompatibilityChannelFor(bootstrap) == "stable",
            "v0.0.12 package manifest must use stable transport channel so installed v0.0.11 can validate the bootstrap update.");
        Require(ReleaseAuthority.ManifestCompatibilityChannelFor(new Version(0, 0, 13)) == "precert",
            "The legacy transport bridge must be limited to v0.0.12; later pre-cert packages must use precert.");
        Require(ReleaseAuthority.ManifestCompatibilityChannelFor(ReleaseAuthority.FinalTargetVersion) == "stable",
            "Final v0.1.0 package transport channel must remain stable.");
    }

    private static void VerifyReleaseCollectionSelection()
    {
        var payload = """
        [
          {
            "tag_name": "v9.9.9",
            "draft": false,
            "prerelease": false,
            "assets": []
          },
          {
            "tag_name": "v0.0.12",
            "draft": false,
            "prerelease": true,
            "assets": [
              { "name": "MRC-v0.0.12-win-x64.zip", "browser_download_url": "https://example.invalid/MRC-v0.0.12-win-x64.zip" },
              { "name": "SHA256SUMS.txt", "browser_download_url": "https://example.invalid/SHA256SUMS.txt" }
            ]
          },
          {
            "tag_name": "v0.0.11",
            "draft": false,
            "prerelease": false,
            "assets": []
          }
        ]
        """;
        var handler = new CaptureHandler(payload);
        using var client = new HttpClient(handler);
        var source = new GitHubReleaseSource(client);
        UpdateRelease release;
        try
        {
            release = source.ResolveLatestAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Release collection selection is not implemented: {ex.Message}", ex);
        }

        Require(handler.LastUri is not null, "Release source made no HTTP request.");
        Require(!handler.LastUri!.AbsolutePath.EndsWith("/releases/latest", StringComparison.OrdinalIgnoreCase),
            "Updater still uses /releases/latest, which cannot truthfully support pre-cert releases.");
        Require(handler.LastUri.AbsolutePath.EndsWith("/releases", StringComparison.OrdinalIgnoreCase),
            $"Updater must enumerate the releases collection, requested {handler.LastUri}.");
        Require(release.Version == new Version(0, 0, 12),
            $"Release selector must choose newest allowed MRC release v0.0.12 and reject unrelated v9.9.9; got {release.Version}.");
    }

    private static void VerifyProgressContract()
    {
        var core = typeof(UpdateService).Assembly;
        var stage = core.GetType("MRC.Core.Updating.UpdateProgressStage");
        Require(stage is not null && stage.IsEnum, "UpdateProgressStage is missing.");
        var expected = new[]
        {
            "Resolve", "Compare", "Download", "Sha256Verify", "ManifestValidate",
            "Install", "Activate", "ActivationVerify", "RollbackRetention", "Complete"
        };
        Require(Enum.GetNames(stage!).SequenceEqual(expected),
            $"Update progress stages are wrong: {string.Join(", ", Enum.GetNames(stage!))}.");

        var progress = core.GetType("MRC.Core.Updating.UpdateProgress");
        Require(progress is not null, "UpdateProgress model is missing.");
        foreach (var property in new[] { "Stage", "Message", "Percent" })
        {
            Require(progress!.GetProperty(property) is not null, $"UpdateProgress.{property} is missing.");
        }

        var hasProgressConstructor = typeof(UpdateService).GetConstructors()
            .Any(ctor => ctor.GetParameters().Any(parameter =>
                parameter.ParameterType.IsGenericType
                && parameter.ParameterType.GetGenericTypeDefinition() == typeof(IProgress<>)));
        Require(hasProgressConstructor, "UpdateService does not accept an IProgress<UpdateProgress> sink.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        private readonly string _payload;
        public CaptureHandler(string payload) => _payload = payload;
        public Uri? LastUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_payload, Encoding.UTF8, "application/json")
            });
        }
    }
}
