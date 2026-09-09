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
            ("successful pre-cert update verifies SHA installs immutably and switches atomically", SuccessfulUpdate),
            ("checksum mismatch leaves the active version untouched", ChecksumMismatchRollsBack),
            ("invalid candidate payload is never activated", InvalidPayloadNeverActivates),
            ("already-current release is a no-op without asset downloads", AlreadyCurrentIsNoOp),
            ("ZIP path traversal is rejected without escaping the install root", ZipTraversalIsRejected),
            ("GitHub release source enumerates authorized releases and exact assets", GitHubReleaseSourceContract),
            ("update aliases are live and help presents active update", CliUpdateSurface)
        };
        var failures = 0;
        Console.WriteLine($"MRC PASS 4 update harness — {tests.Length} tests");
        foreach (var test in tests)
        {
            try { test.Body(); Console.WriteLine($"PASS  {test.Name}"); }
            catch (Exception ex) { failures++; Console.WriteLine($"FAIL  {test.Name}\n      {ex.Message}"); }
        }
        Console.WriteLine(failures == 0 ? $"PASS  all {tests.Length} PASS 4 update tests" : $"FAIL  {failures} of {tests.Length} PASS 4 update tests");
        return failures;
    }

    private static void SuccessfulUpdate()
    {
        using var temp = new TempDirectory(); SeedInstalled(temp.Path, "0.0.11", "old-cli");
        var package = BuildPackage("0.0.12", "new-cli", "new-gui");
        var source = FakeSource.For("0.0.12", package, ValidChecksums("0.0.12", package));
        var result = new UpdateService(source, temp.Path).RunAsync().GetAwaiter().GetResult();
        Require(result.Outcome == UpdateOutcome.Updated, $"Expected Updated, got {result.Outcome}: {result.Message}");
        Require(File.ReadAllText(Path.Combine(temp.Path, "current.version")) == "0.0.12", "Active pointer was not switched to 0.0.12.");
        Require(File.ReadAllText(Path.Combine(temp.Path, "previous.version")) == "0.0.11", "Rollback pointer was not retained.");
        Require(File.ReadAllText(Path.Combine(temp.Path, "versions", "0.0.11", "MRC.exe")) == "old-cli", "Previous immutable version was modified.");
        Require(File.ReadAllText(Path.Combine(temp.Path, "versions", "0.0.12", "MRC.exe")) == "new-cli", "New CLI payload was not installed.");
    }

    private static void ChecksumMismatchRollsBack()
    {
        using var temp = new TempDirectory(); SeedInstalled(temp.Path, "0.0.11", "old-cli");
        var package = BuildPackage("0.0.12", "new-cli", "new-gui");
        var source = FakeSource.For("0.0.12", package, $"{new string('0',64)}  MRC-v0.0.12-win-x64.zip\n");
        var result = new UpdateService(source, temp.Path).RunAsync().GetAwaiter().GetResult();
        Require(result.Outcome == UpdateOutcome.Failed, "Checksum mismatch must fail update.");
        Require(File.ReadAllText(Path.Combine(temp.Path, "current.version")) == "0.0.11", "Checksum failure changed active version.");
        Require(!Directory.Exists(Path.Combine(temp.Path, "versions", "0.0.12")), "Unverified candidate was retained.");
    }

    private static void InvalidPayloadNeverActivates()
    {
        using var temp = new TempDirectory(); SeedInstalled(temp.Path, "0.0.11", "old-cli");
        var package = BuildPackage("0.0.12", "new-cli", null);
        var source = FakeSource.For("0.0.12", package, ValidChecksums("0.0.12", package));
        var result = new UpdateService(source, temp.Path).RunAsync().GetAwaiter().GetResult();
        Require(result.Outcome == UpdateOutcome.Failed, "Missing GUI must fail validation.");
        Require(File.ReadAllText(Path.Combine(temp.Path, "current.version")) == "0.0.11", "Invalid candidate changed active pointer.");
    }

    private static void AlreadyCurrentIsNoOp()
    {
        using var temp = new TempDirectory(); SeedInstalled(temp.Path, "0.0.12", "cli");
        var source = FakeSource.For("0.0.12", Array.Empty<byte>(), string.Empty);
        var result = new UpdateService(source, temp.Path).RunAsync().GetAwaiter().GetResult();
        Require(result.Outcome == UpdateOutcome.UpToDate, $"Expected UpToDate, got {result.Outcome}.");
        Require(source.DownloadCount == 0, "Already-current update must not download assets.");
    }

    private static void ZipTraversalIsRejected()
    {
        using var temp = new TempDirectory(); SeedInstalled(temp.Path, "0.0.11", "old-cli");
        var package = BuildPackage("0.0.12", "new-cli", "new-gui", "../escape.txt");
        var source = FakeSource.For("0.0.12", package, ValidChecksums("0.0.12", package));
        var result = new UpdateService(source, temp.Path).RunAsync().GetAwaiter().GetResult();
        Require(result.Outcome == UpdateOutcome.Failed, "Traversal candidate must fail.");
        Require(!File.Exists(Path.Combine(temp.Path, "escape.txt")), "ZIP escaped staging boundary.");
        Require(File.ReadAllText(Path.Combine(temp.Path, "current.version")) == "0.0.11", "Traversal rejection changed current.version.");
    }

    private static void GitHubReleaseSourceContract()
    {
        var handler = new RecordingHandler(request =>
        {
            Require(request.RequestUri?.AbsoluteUri == "https://api.github.com/repos/doonchy16-cloud/MRC/releases?per_page=30", $"Unexpected release endpoint: {request.RequestUri}");
            const string json = "[{\"tag_name\":\"v9.9.9\",\"draft\":true,\"prerelease\":true,\"assets\":[]},{\"tag_name\":\"v0.0.12\",\"draft\":false,\"prerelease\":true,\"assets\":[{\"name\":\"MRC-v0.0.12-win-x64.zip\",\"browser_download_url\":\"https://example.test/package\"},{\"name\":\"SHA256SUMS.txt\",\"browser_download_url\":\"https://example.test/sums\"}]}]";
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        });
        using var client = new HttpClient(handler);
        var release = new GitHubReleaseSource(client).ResolveLatestAsync().GetAwaiter().GetResult();
        Require(release.Version == new Version(0,0,12) && release.IsPrerelease, $"Resolved wrong authorized release: {release.Version}");
        Require(release.Assets.Any(a => a.Name == "MRC-v0.0.12-win-x64.zip") && release.Assets.Any(a => a.Name == "SHA256SUMS.txt"), "Required authority assets were not resolved.");
    }

    private static void CliUpdateSurface()
    {
        BuildCli();
        var help = Run(CliExe(), "--help");
        Require(help.ExitCode == 0 && help.Output.Contains("-update", StringComparison.OrdinalIgnoreCase), "Help omits active update command.");
        var dispatcher = File.ReadAllText(Path.Combine(RepoRoot(), "src", "MRC.Cli", "CliDispatcher.cs"));
        Require(dispatcher.Contains("UpdateProgress", StringComparison.Ordinal), "CLI update does not stream staged progress.");
    }

    private static byte[] BuildPackage(string version, string cli, string? gui, string? traversalEntry = null)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            WriteEntry(archive, "manifest.json", $"{{\"product\":\"Main Runner Control\",\"version\":\"{version}\",\"channel\":\"precert\",\"releaseStage\":\"PreCertification\",\"finalTarget\":\"0.1.0\",\"runtime\":\"win-x64\",\"targetMachine\":\"DOONCHYSCOMPUTI\",\"runnerRoot\":\"D:\\\\Git_Runners_Main\",\"canonicalCommand\":\"MRC\"}}");
            WriteEntry(archive, "payload/MRC.exe", cli); if (gui is not null) WriteEntry(archive, "payload/MRC.Gui.exe", gui); if (traversalEntry is not null) WriteEntry(archive, traversalEntry, "escaped");
        }
        return stream.ToArray();
    }
    private static void WriteEntry(ZipArchive a, string n, string c) { var e = a.CreateEntry(n); using var w = new StreamWriter(e.Open(), Encoding.UTF8, leaveOpen:false); w.Write(c); }
    private static string ValidChecksums(string version, byte[] package) => $"{Convert.ToHexString(SHA256.HashData(package)).ToLowerInvariant()}  MRC-v{version}-win-x64.zip\n";
    private static void SeedInstalled(string root, string version, string cli) { Directory.CreateDirectory(Path.Combine(root,"versions",version)); File.WriteAllText(Path.Combine(root,"versions",version,"MRC.exe"),cli); File.WriteAllText(Path.Combine(root,"versions",version,"MRC.Gui.exe"),"old-gui"); File.WriteAllText(Path.Combine(root,"current.version"),version); }
    private static void BuildCli() { var r=Run("dotnet","build",Path.Combine(RepoRoot(),"src","MRC.Cli","MRC.Cli.csproj"),"-c","Release","-v","minimal"); Require(r.ExitCode==0,$"CLI build failed.\n{r.Output}"); }
    private static string CliExe()=>Path.Combine(RepoRoot(),"src","MRC.Cli","bin","Release","net10.0","MRC.exe");
    private static ProcessResult Run(string fileName, params string[] args) { var psi=new ProcessStartInfo(fileName){WorkingDirectory=RepoRoot(),RedirectStandardOutput=true,RedirectStandardError=true,UseShellExecute=false,CreateNoWindow=true}; foreach(var a in args) psi.ArgumentList.Add(a); using var p=Process.Start(psi)??throw new InvalidOperationException($"Could not start {fileName}."); var o=p.StandardOutput.ReadToEnd()+p.StandardError.ReadToEnd(); p.WaitForExit(); return new ProcessResult(p.ExitCode,o); }
    private static string RepoRoot(){var d=new DirectoryInfo(Directory.GetCurrentDirectory());while(d is not null){if(File.Exists(Path.Combine(d.FullName,"Auth","0000_MasterAuth.md")))return d.FullName;d=d.Parent;}throw new InvalidOperationException("Could not locate MRC repository root.");}
    private static void Require(bool c,string m){if(!c)throw new InvalidOperationException(m);}
    private sealed record ProcessResult(int ExitCode,string Output);
    private sealed class FakeSource:IUpdateReleaseSource { private readonly UpdateRelease _release; private readonly Dictionary<string,byte[]> _assets; public int DownloadCount{get;private set;} private FakeSource(UpdateRelease r,Dictionary<string,byte[]> a){_release=r;_assets=a;} public static FakeSource For(string v,byte[] p,string sums){var n=$"MRC-v{v}-win-x64.zip";return new FakeSource(new UpdateRelease($"v{v}",Version.Parse(v),new[]{new UpdateAsset(n,new Uri($"https://example.test/{n}")),new UpdateAsset("SHA256SUMS.txt",new Uri("https://example.test/SHA256SUMS.txt"))},true),new(StringComparer.Ordinal){[n]=p,["SHA256SUMS.txt"]=Encoding.UTF8.GetBytes(sums)});} public Task<UpdateRelease> ResolveLatestAsync(CancellationToken c=default)=>Task.FromResult(_release); public Task<byte[]> DownloadAssetAsync(UpdateAsset a,CancellationToken c=default){DownloadCount++;return Task.FromResult(_assets[a.Name]);} }
    private sealed class RecordingHandler:HttpMessageHandler { private readonly Func<HttpRequestMessage,HttpResponseMessage> _handler; public RecordingHandler(Func<HttpRequestMessage,HttpResponseMessage> h)=>_handler=h; protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken c)=>Task.FromResult(_handler(r)); }
    private sealed class TempDirectory:IDisposable { public string Path{get;}=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"mrc-pass4-update-"+Guid.NewGuid().ToString("N")); public TempDirectory()=>Directory.CreateDirectory(Path); public void Dispose(){try{if(Directory.Exists(Path))Directory.Delete(Path,true);}catch{}} }
}
