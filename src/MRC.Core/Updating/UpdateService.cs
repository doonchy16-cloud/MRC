using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MRC.Core.Updating;

public sealed class UpdateService
{
    private readonly IUpdateReleaseSource _releaseSource;
    private readonly string _installRoot;
    private readonly IUpdateActivationVerifier _activationVerifier;

    public UpdateService(
        IUpdateReleaseSource releaseSource,
        string installRoot,
        IUpdateActivationVerifier? activationVerifier = null)
    {
        _releaseSource = releaseSource ?? throw new ArgumentNullException(nameof(releaseSource));
        if (string.IsNullOrWhiteSpace(installRoot)) throw new ArgumentException("Install root is required.", nameof(installRoot));
        _installRoot = Path.GetFullPath(installRoot);
        _activationVerifier = activationVerifier ?? new PayloadActivationVerifier();
    }

    public async Task<UpdateResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var currentFile = UnderInstallRoot("current.version");
        if (!File.Exists(currentFile))
        {
            return new UpdateResult(UpdateOutcome.Failed, $"MRC installation is incomplete: {currentFile} is missing.");
        }

        if (!Version.TryParse((await File.ReadAllTextAsync(currentFile, cancellationToken)).Trim(), out var currentVersion))
        {
            return new UpdateResult(UpdateOutcome.Failed, "MRC current.version does not contain a valid version.");
        }

        UpdateRelease release;
        try
        {
            release = await _releaseSource.ResolveLatestAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            return new UpdateResult(UpdateOutcome.Failed, $"Release resolution failed: {ex.Message}", currentVersion, currentVersion);
        }

        if (release.Version <= currentVersion)
        {
            return new UpdateResult(UpdateOutcome.UpToDate, $"MRC {currentVersion} is already current.", currentVersion, currentVersion);
        }

        var packageName = $"MRC-v{release.Version}-win-x64.zip";
        var packageAsset = release.Assets.SingleOrDefault(asset => string.Equals(asset.Name, packageName, StringComparison.Ordinal));
        var checksumAsset = release.Assets.SingleOrDefault(asset => string.Equals(asset.Name, "SHA256SUMS.txt", StringComparison.Ordinal));
        if (packageAsset is null || checksumAsset is null)
        {
            return new UpdateResult(
                UpdateOutcome.Failed,
                $"Release {release.TagName} does not contain required assets {packageName} and SHA256SUMS.txt.",
                currentVersion,
                currentVersion);
        }

        byte[] packageBytes;
        byte[] checksumBytes;
        try
        {
            packageBytes = await _releaseSource.DownloadAssetAsync(packageAsset, cancellationToken);
            checksumBytes = await _releaseSource.DownloadAssetAsync(checksumAsset, cancellationToken);
        }
        catch (Exception ex)
        {
            return new UpdateResult(UpdateOutcome.Failed, $"Release download failed: {ex.Message}", currentVersion, currentVersion);
        }

        var checksumText = Encoding.UTF8.GetString(checksumBytes);
        var expectedHash = FindExpectedHash(checksumText, packageName);
        if (expectedHash is null)
        {
            return new UpdateResult(UpdateOutcome.Failed, $"SHA256SUMS.txt does not contain a valid SHA-256 entry for {packageName}.", currentVersion, currentVersion);
        }

        var actualHash = Convert.ToHexString(SHA256.HashData(packageBytes)).ToLowerInvariant();
        if (!string.Equals(expectedHash, actualHash, StringComparison.OrdinalIgnoreCase))
        {
            return new UpdateResult(UpdateOutcome.Failed, $"SHA-256 verification failed for {packageName}.", currentVersion, currentVersion);
        }

        var stagingParent = UnderInstallRoot("staging");
        var stagingRoot = Path.Combine(stagingParent, Guid.NewGuid().ToString("N"));
        var extractRoot = Path.Combine(stagingRoot, "package");
        var versionRoot = UnderInstallRoot("versions", release.Version.ToString());
        var candidateInstalled = false;
        var activeSwitched = false;

        try
        {
            Directory.CreateDirectory(extractRoot);
            ExtractSafely(packageBytes, extractRoot);
            ValidateCandidate(extractRoot, release.Version);

            if (Directory.Exists(versionRoot))
            {
                throw new InvalidOperationException($"Immutable version directory already exists: {versionRoot}");
            }

            var payloadRoot = Path.Combine(extractRoot, "payload");
            Directory.CreateDirectory(Path.GetDirectoryName(versionRoot)!);
            Directory.Move(payloadRoot, versionRoot);
            candidateInstalled = true;

            var previousFile = UnderInstallRoot("previous.version");
            AtomicWrite(previousFile, currentVersion.ToString());
            AtomicWrite(currentFile, release.Version.ToString());
            activeSwitched = true;

            if (!Version.TryParse((await File.ReadAllTextAsync(currentFile, cancellationToken)).Trim(), out var activated)
                || activated != release.Version)
            {
                throw new InvalidOperationException("Atomic activation pointer verification failed.");
            }

            var verification = await _activationVerifier.VerifyAsync(versionRoot, release.Version, cancellationToken);
            if (!verification.Success)
            {
                throw new InvalidOperationException($"Activated candidate verification failed: {verification.Message}");
            }

            return new UpdateResult(
                UpdateOutcome.Updated,
                $"MRC updated from {currentVersion} to {release.Version}. Previous version retained for rollback.",
                currentVersion,
                release.Version);
        }
        catch (Exception ex)
        {
            if (activeSwitched)
            {
                try { AtomicWrite(currentFile, currentVersion.ToString()); }
                catch { }
            }

            if (candidateInstalled)
            {
                try { if (Directory.Exists(versionRoot)) Directory.Delete(versionRoot, recursive: true); }
                catch { }
            }

            return new UpdateResult(UpdateOutcome.Failed, $"Update candidate was not activated: {ex.Message}", currentVersion, currentVersion);
        }
        finally
        {
            try { if (Directory.Exists(stagingRoot)) Directory.Delete(stagingRoot, recursive: true); }
            catch { }
        }
    }

    private string UnderInstallRoot(params string[] segments)
    {
        var path = segments.Aggregate(_installRoot, Path.Combine);
        var full = Path.GetFullPath(path);
        var rootWithSeparator = _installRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!full.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Update path escaped the MRC install root: {full}");
        }
        return full;
    }

    private static string? FindExpectedHash(string checksumText, string packageName)
    {
        foreach (var rawLine in checksumText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = rawLine.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length < 2 || !string.Equals(parts[^1], packageName, StringComparison.Ordinal)) continue;
            var hash = parts[0];
            if (hash.Length == 64 && hash.All(Uri.IsHexDigit)) return hash.ToLowerInvariant();
        }
        return null;
    }

    private static void ExtractSafely(byte[] packageBytes, string extractRoot)
    {
        var root = Path.GetFullPath(extractRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        using var memory = new MemoryStream(packageBytes, writable: false);
        using var archive = new ZipArchive(memory, ZipArchiveMode.Read);
        foreach (var entry in archive.Entries)
        {
            var target = Path.GetFullPath(Path.Combine(extractRoot, entry.FullName.Replace('/', Path.DirectorySeparatorChar)));
            if (!target.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"ZIP entry escapes the staging boundary: {entry.FullName}");
            }

            if (string.IsNullOrEmpty(entry.Name))
            {
                Directory.CreateDirectory(target);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            using var input = entry.Open();
            using var output = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            input.CopyTo(output);
        }
    }

    private static void ValidateCandidate(string extractRoot, Version expectedVersion)
    {
        var manifestPath = Path.Combine(extractRoot, "manifest.json");
        var payloadRoot = Path.Combine(extractRoot, "payload");
        if (!File.Exists(manifestPath)) throw new InvalidDataException("Candidate manifest.json is missing.");
        if (!File.Exists(Path.Combine(payloadRoot, "MRC.exe"))) throw new InvalidDataException("Candidate payload/MRC.exe is missing.");
        if (!File.Exists(Path.Combine(payloadRoot, "MRC.Gui.exe"))) throw new InvalidDataException("Candidate payload/MRC.Gui.exe is missing.");

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;
        RequireManifest(root, "product", MrcConstants.ProductName);
        RequireManifest(root, "version", expectedVersion.ToString());
        RequireManifest(root, "channel", MrcConstants.ReleaseChannel);
        RequireManifest(root, "runtime", "win-x64");
        RequireManifest(root, "targetMachine", MrcConstants.TargetMachineName);
        RequireManifest(root, "runnerRoot", MrcConstants.RunnerRoot);
        RequireManifest(root, "canonicalCommand", MrcConstants.CliName);
    }

    private static void RequireManifest(JsonElement root, string property, string expected)
    {
        if (!root.TryGetProperty(property, out var element)
            || !string.Equals(element.GetString(), expected, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Candidate manifest {property} does not match authority '{expected}'.");
        }
    }

    private static void AtomicWrite(string path, string value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temp = $"{path}.tmp-{Guid.NewGuid():N}";
        try
        {
            File.WriteAllText(temp, value, Encoding.ASCII);
            File.Move(temp, path, overwrite: true);
        }
        finally
        {
            try { if (File.Exists(temp)) File.Delete(temp); }
            catch { }
        }
    }
}

public sealed class PayloadActivationVerifier : IUpdateActivationVerifier
{
    public Task<ActivationVerificationResult> VerifyAsync(
        string versionRoot,
        Version expectedVersion,
        CancellationToken cancellationToken = default)
    {
        var success = File.Exists(Path.Combine(versionRoot, "MRC.exe"))
            && File.Exists(Path.Combine(versionRoot, "MRC.Gui.exe"));
        return Task.FromResult(new ActivationVerificationResult(
            success,
            success ? $"Payload for {expectedVersion} is present." : "Required executable payload is missing."));
    }
}

public sealed class ExecutableVersionVerifier : IUpdateActivationVerifier
{
    public async Task<ActivationVerificationResult> VerifyAsync(
        string versionRoot,
        Version expectedVersion,
        CancellationToken cancellationToken = default)
    {
        var executable = Path.Combine(versionRoot, "MRC.exe");
        if (!File.Exists(executable)) return new ActivationVerificationResult(false, $"{executable} is missing.");

        try
        {
            var startInfo = new ProcessStartInfo(executable)
            {
                WorkingDirectory = versionRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add("--version");
            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Could not start candidate MRC.exe.");
            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            var output = (await stdoutTask) + (await stderrTask);
            var expected = $"Version: {expectedVersion}";
            var success = process.ExitCode == 0 && output.Contains(expected, StringComparison.Ordinal);
            return new ActivationVerificationResult(
                success,
                success ? $"Candidate reported {expected}." : $"Candidate version verification failed. Exit {process.ExitCode}. Output: {output.Trim()}");
        }
        catch (Exception ex)
        {
            return new ActivationVerificationResult(false, ex.Message);
        }
    }
}
