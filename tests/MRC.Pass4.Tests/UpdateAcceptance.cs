using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using MRC.Core.Updating;

namespace MRC.Pass4.Tests;

internal static class UpdateAcceptance
{
    public static int Run()
    {
        var tests = new (string Name, Action Body)[]
        {
            ("successful update verifies SHA-256 installs immutable version and switches atomically", SuccessfulUpdate),
            ("checksum mismatch leaves the active version untouched", ChecksumMismatchRollsBack),
            ("invalid candidate payload is never activated", InvalidPayloadNeverActivates),
            ("already-current release is a no-op without asset downloads", AlreadyCurrentIsNoOp),
            ("ZIP path traversal is rejected without escaping the install root", ZipTraversalIsRejected),
            ("GitHub release source resolves latest release and exact authority assets", GitHubReleaseSourceContract),
            ("update aliases are live and help no longer describes update as future work", CliUpdateSurface)
        };

        var failures = 0;
        Console.WriteLine($"MRC PASS 4 update harness — {tests.Length} tests");
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

        Console.WriteLine(failures == 0
            ? $"PASS  all {tests.Length} PASS 4 update tests"
            : $"FAIL  {failures} of {tests.Length} PASS 4 update tests");
        return failures;
    }

    private static void SuccessfulUpdate()
    {
        using var temp = new TempDirectory();
        SeedInstalled(temp.Path, "0.1.0", "old-cli");
        var package = BuildPackage("0.1.1", cli: "new-cli", gui: "new-gui");
        var source = FakeSource.For("0.1.1", package, ValidChecksums("0.1.1", package));
        var result = new UpdateService(source, temp.Path).RunAsync().GetAwaiter().GetResult();

        Require(result.Outcome == UpdateOutcome.Updated, $"Expected Updated, got {result.Outcome}: {result.Message}");
        Require(File.ReadAllText(Path.Combine(temp.Path, "current.version")) == "0.1.1", "Active pointer was not switched to 0.1.1.");
        Require(File.ReadAllText(Path.Combine(temp.Path, "previous.version")) == "0.1.0", "Previous-version rollback pointer was not retained.");
        Require(File.ReadAllText(Path.Combine(temp.Path, "versions", "0.1.0", "MRC.exe")) == "old-cli", "Previous immutable version was modified.");
        Require(File.ReadAllText(Path.Combine(temp.Path, "versions", "0.1.1", "MRC.exe")) == "new-cli", "New CLI payload was not installed.");
        Require(File.ReadAllText(Path.Combine(temp.Path, "versions", "0.1.1", "MRC.Gui.exe")) == "new-gui", "New GUI payload was not installed.");
    }

    private static void ChecksumMismatchRollsBack()
    {
        using var temp = new TempDirectory();
        SeedInstalled(temp.Path, "0.1.0", "old-cli");
        var package = BuildPackage("0.1.1", cli: "new-cli", gui: "new-gui");
        var source = FakeSource.For("0.1.1", package, $"{new string('0', 64)}  MRC-v0.1.1-win-x64.zip\n");
        var result = new UpdateService(source, temp.Path).RunAsync().GetAwaiter().GetResult();

        Require(result.Outcome == UpdateOutcome.Failed, "Checksum mismatch must fail the update.");
        Require(File.ReadAllText(Path.Combine(temp.Path, "current.version")) == "0.1.0", "Checksum failure changed the active version.");
        Require(!Directory.Exists(Path.Combine(temp.Path, "versions", "0.1.1")), "Checksum failure activated or retained an unverified candidate version.");
    }

    private static void InvalidPayloadNeverActivates()
    {
        using var temp = new TempDirectory();
        SeedInstalled(temp.Path, "0.1.0", "old-cli");
        var package = BuildPackage("0.1.1", cli: "new-cli", gui: null);
        var source = FakeSource.For("0.1.1", package, ValidChecksums("0.1.1", package));
        var result = new UpdateService(source, temp.Path).RunAsync().GetAwaiter().GetResult();

        Require(result.Outcome == UpdateOutcome.Failed, "Missing MRC.Gui.exe must fail candidate validation.");
        Require(File.ReadAllText(Path.Combine(temp.Path, "current.version")) == "0.1.0", "Invalid payload changed the active pointer.");
        Require(!Directory.Exists(Path.Combine(temp.Path, "versions", "0.1.1")), "Invalid payload was retained as an installed version.");
    }

    private static void AlreadyCurrentIsNoOp()
    {
        using var temp = new TempDirectory();
        SeedInstalled(temp.Path, "0.1.0", "old-cli");
        var source = FakeSource.For("0.1.0", Array.Empty<byte>(), string.Empty);
        var result = new UpdateService(source, temp.Path).RunAsync().GetAwaiter().GetResult();

        Require(result.Outcome == UpdateOutcome.UpToDate, $"Expected UpToDate, got {result.Outcome}.");
        Require(source.DownloadCount == 0, "Already-current update must not download release assets.");
        Require(File.ReadAllText(Path.Combine(temp.Path, "current.version")) == "0.1.0", "No-op update changed current.version.");
    }

    private static void ZipTraversalIsRejected()
    {
        using var temp = new TempDirectory();
        SeedInstalled(temp.Path, "0.1.0", "old-cli");
        var package = BuildPackage("0.1.1", cli: "new-cli", gui: "new-gui", traversalEntry: "../escape.txt");
        var source = FakeSource.For("0.1.1", package, ValidChecksums("0.1.1", package));
        var result = new UpdateService(source, temp.Path).RunAsync().GetAwaiter().GetResult();

        Require(result.Outcome == UpdateOutcome.Failed, "Path-traversal candidate must fail validation/extraction.");
        Require(!File.Exists(Path.Combine(temp.Path, "escape.txt")), "Candidate ZIP escaped the staging boundary.");
        Require(File.ReadAllText(Path.Combine(temp.Path, "current.version")) == "0.1.0", "Traversal rejection changed current.version.");
    }

    private static void GitHubReleaseSourceContract()
    {
        var handler = new RecordingHandler(request =>
        {
            Require(request.RequestUri?.AbsoluteUri == "https://api.github.com/repos/doonchy16-cloud/MRC/releases/latest",
                $"Unexpected release endpoint: {request.RequestUri}");
            const string json = "{\"tag_name\":\"v0.1.1\",\"assets\":[{\"name\":\"MRC-v0.1.1-win-x64.zip\",\"browser_download_url\":\"https://example.test/package\"},{\"name\":\"SHA256SUMS.txt\",\"browser_download_url\":\"https://example.test/sums\"}]}";
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        });
        using var client = new HttpClient(handler);
        var release = new GitHubReleaseSource(client).ResolveLatestAsync().GetAwaiter().GetResult();
        Require(release.Version == new Version(0, 1, 1), $"Resolved wrong version: {release.Version}");
        Require(release.Assets.Any(asset => asset.Name == "MRC-v0.1.1-win-x64.zip"), "Package asset was not resolved.");
        Require(release.Assets.Any(asset => asset.Name == "SHA256SUMS.txt"), "Checksum authority asset was not resolved.");
    }

    private static void CliUpdateSurface()
    {
        BuildCli();
        foreach (var alias in new[] { "-update", "--update" })
        {
            var result = Run(CliExe(), alias);
            Require(result.ExitCode != 4, $"{alias} is still reserved instead of routing to PASS 4 update.");
            Require(result.Output.Contains("MRC Update", StringComparison.OrdinalIgnoreCase), $"{alias} omitted active update heading.\n{result.Output}");
        }

        var help = Run(CliExe(), "--help");
        Require(!help.Output.Contains("Update MRC (implemented in PASS 4)", StringComparison.Ordinal), "Help still describes Update as future work.");
    }

    private static byte[] BuildPackage(string version, string cli, string? gui, string? traversalEntry = null)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(archive, "manifest.json", $"{{\"product\":\"Main Runner Control\",\"version\":\"{version}\",\"channel\":\"stable\",\"runtime\":\"win-x64\",\"targetMachine\":\"Main-PC\",\"runnerRoot\":\"D:\\\\Git_Runners_Main\",\"canonicalCommand\":\"MRC\"}}");
            WriteEntry(archive, "payload/MRC.exe", cli);
            if (gui is not null) WriteEntry(archive, "payload/MRC.Gui.exe", gui);
            if (traversalEntry is not null) WriteEntry(archive, traversalEntry, "escaped");
        }
        return stream.ToArray();
    }

    private static void WriteEntry(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name);
        using var writer = new StreamWriter(entry.Open(), Encoding.UTF8, leaveOpen: false);
        writer.Write(content);
    }

    private static string ValidChecksums(string version, byte[] package)
    {
        var hash = Convert.ToHexString(SHA256.HashData(package)).ToLowerInvariant();
        return $"{hash}  MRC-v{version}-win-x64.zip\n";
    }

    private static void SeedInstalled(string root, string version, string cli)
    {
        Directory.CreateDirectory(Path.Combine(root, "versions", version));
        File.WriteAllText(Path.Combine(root, "versions", version, "MRC.exe"), cli);
        File.WriteAllText(Path.Combine(root, "versions", version, "MRC.Gui.exe"), "old-gui");
        File.WriteAllText(Path.Combine(root, "current.version"), version);
    }

    private static void BuildCli()
    {
        var result = Run("dotnet", "build", Path.Combine(RepoRoot(), "src", "MRC.Cli", "MRC.Cli.csproj"), "-c", "Release", "-v", "minimal");
        Require(result.ExitCode == 0, $"CLI build failed.\n{result.Output}");
    }

    private static string CliExe() => Path.Combine(RepoRoot(), "src", "MRC.Cli", "bin", "Release", "net10.0", "MRC.exe");

    private static ProcessResult Run(string fileName, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            WorkingDirectory = RepoRoot(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);
        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Could not start {fileName}.");
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return new ProcessResult(process.ExitCode, stdout + stderr);
    }

    private static string RepoRoot()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Auth", "0000_MasterAuth.md"))) return current.FullName;
            current = current.Parent;
        }
        throw new InvalidOperationException("Could not locate MRC repository root.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed record ProcessResult(int ExitCode, string Output);

    private sealed class FakeSource : IUpdateReleaseSource
    {
        private readonly UpdateRelease _release;
        private readonly Dictionary<string, byte[]> _assets;
        public int DownloadCount { get; private set; }

        private FakeSource(UpdateRelease release, Dictionary<string, byte[]> assets)
        {
            _release = release;
            _assets = assets;
        }

        public static FakeSource For(string version, byte[] package, string checksums)
        {
            var packageName = $"MRC-v{version}-win-x64.zip";
            var release = new UpdateRelease(
                $"v{version}",
                Version.Parse(version),
                new[]
                {
                    new UpdateAsset(packageName, new Uri($"https://example.test/{packageName}")),
                    new UpdateAsset("SHA256SUMS.txt", new Uri("https://example.test/SHA256SUMS.txt"))
                });
            return new FakeSource(release, new Dictionary<string, byte[]>(StringComparer.Ordinal)
            {
                [packageName] = package,
                ["SHA256SUMS.txt"] = Encoding.UTF8.GetBytes(checksums)
            });
        }

        public Task<UpdateRelease> ResolveLatestAsync(CancellationToken cancellationToken = default) => Task.FromResult(_release);

        public Task<byte[]> DownloadAssetAsync(UpdateAsset asset, CancellationToken cancellationToken = default)
        {
            DownloadCount++;
            return Task.FromResult(_assets[asset.Name]);
        }
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;
        public RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) => _handler = handler;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(_handler(request));
    }

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "mrc-pass4-update-" + Guid.NewGuid().ToString("N"));
        public TempDirectory() => Directory.CreateDirectory(Path);
        public void Dispose()
        {
            try { if (Directory.Exists(Path)) Directory.Delete(Path, recursive: true); }
            catch { }
        }
    }
}
