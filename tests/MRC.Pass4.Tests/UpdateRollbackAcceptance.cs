using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using MRC.Core.Updating;

namespace MRC.Pass4.Tests;

internal static class UpdateRollbackAcceptance
{
    public static int Run()
    {
        var tests = new (string Name, Action Body)[]
        {
            ("post-activation verification failure restores previous current pointer", VerificationFailureRollsBack),
            ("live CLI update uses executable version-response verification", CliUsesExecutableVerifier)
        };

        var failures = 0;
        Console.WriteLine($"MRC PASS 4 update rollback harness — {tests.Length} tests");
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
            ? $"PASS  all {tests.Length} PASS 4 update rollback tests"
            : $"FAIL  {failures} of {tests.Length} PASS 4 update rollback tests");
        return failures;
    }

    private static void VerificationFailureRollsBack()
    {
        using var temp = new TempDirectory();
        SeedInstalled(temp.Path, "0.1.0");
        var package = BuildPackage("0.1.1");
        var source = new FakeSource("0.1.1", package);
        var result = new UpdateService(source, temp.Path, new FailingVerifier()).RunAsync().GetAwaiter().GetResult();

        Require(result.Outcome == UpdateOutcome.Failed, "Failed activation verification must fail the update.");
        Require(File.ReadAllText(Path.Combine(temp.Path, "current.version")) == "0.1.0", "Activation verification failure did not restore current.version.");
        Require(Directory.Exists(Path.Combine(temp.Path, "versions", "0.1.0")), "Previous immutable version was removed during rollback.");
        Require(!Directory.Exists(Path.Combine(temp.Path, "versions", "0.1.1")), "Rejected candidate version remained installed after rollback.");
    }

    private static void CliUsesExecutableVerifier()
    {
        var source = File.ReadAllText(Path.Combine(RepoRoot(), "src", "MRC.Cli", "CliDispatcher.cs"));
        Require(source.Contains("new ExecutableVersionVerifier()", StringComparison.Ordinal),
            "Live MRC update must verify the candidate executable's reported version after activation.");
    }

    private static byte[] BuildPackage(string version)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            Write(archive, "manifest.json", $"{{\"product\":\"Main Runner Control\",\"version\":\"{version}\",\"channel\":\"stable\",\"runtime\":\"win-x64\",\"targetMachine\":\"Main-PC\",\"runnerRoot\":\"D:\\\\Git_Runners_Main\",\"canonicalCommand\":\"MRC\"}}");
            Write(archive, "payload/MRC.exe", "new-cli");
            Write(archive, "payload/MRC.Gui.exe", "new-gui");
        }
        return stream.ToArray();
    }

    private static void Write(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name);
        using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
        writer.Write(content);
    }

    private static void SeedInstalled(string root, string version)
    {
        var versionRoot = Path.Combine(root, "versions", version);
        Directory.CreateDirectory(versionRoot);
        File.WriteAllText(Path.Combine(versionRoot, "MRC.exe"), "old-cli");
        File.WriteAllText(Path.Combine(versionRoot, "MRC.Gui.exe"), "old-gui");
        File.WriteAllText(Path.Combine(root, "current.version"), version);
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

    private sealed class FailingVerifier : IUpdateActivationVerifier
    {
        public Task<ActivationVerificationResult> VerifyAsync(string versionRoot, Version expectedVersion, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ActivationVerificationResult(false, "synthetic version-response mismatch"));
    }

    private sealed class FakeSource : IUpdateReleaseSource
    {
        private readonly UpdateRelease _release;
        private readonly Dictionary<string, byte[]> _assets;

        public FakeSource(string version, byte[] package)
        {
            var packageName = $"MRC-v{version}-win-x64.zip";
            var hash = Convert.ToHexString(SHA256.HashData(package)).ToLowerInvariant();
            _release = new UpdateRelease($"v{version}", Version.Parse(version), new[]
            {
                new UpdateAsset(packageName, new Uri($"https://example.test/{packageName}")),
                new UpdateAsset("SHA256SUMS.txt", new Uri("https://example.test/SHA256SUMS.txt"))
            });
            _assets = new Dictionary<string, byte[]>(StringComparer.Ordinal)
            {
                [packageName] = package,
                ["SHA256SUMS.txt"] = Encoding.UTF8.GetBytes($"{hash}  {packageName}\n")
            };
        }

        public Task<UpdateRelease> ResolveLatestAsync(CancellationToken cancellationToken = default) => Task.FromResult(_release);
        public Task<byte[]> DownloadAssetAsync(UpdateAsset asset, CancellationToken cancellationToken = default) => Task.FromResult(_assets[asset.Name]);
    }

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "mrc-pass4-rollback-" + Guid.NewGuid().ToString("N"));
        public TempDirectory() => Directory.CreateDirectory(Path);
        public void Dispose()
        {
            try { if (Directory.Exists(Path)) Directory.Delete(Path, recursive: true); }
            catch { }
        }
    }
}
