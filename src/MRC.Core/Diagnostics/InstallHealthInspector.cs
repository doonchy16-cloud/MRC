namespace MRC.Core.Diagnostics;

public sealed record InstallHealthSnapshot(
    string InstallRoot,
    string BinRoot,
    string CurrentVersionFile,
    string LauncherPath,
    string? ActiveVersion,
    string? ActiveVersionRoot,
    int ExpectedPathEntryCount,
    IReadOnlyList<string> StaleMrcPathEntries,
    IReadOnlyList<string> ValidVersionRoots,
    bool LauncherExists,
    bool ActivePayloadValid)
{
    public bool PathHealthy => ExpectedPathEntryCount == 1 && StaleMrcPathEntries.Count == 0;
    public bool PointerHealthy => !string.IsNullOrWhiteSpace(ActiveVersion) && ActivePayloadValid;
    public bool RollbackAvailable => ValidVersionRoots.Any(path =>
        ActiveVersionRoot is null || !string.Equals(path, ActiveVersionRoot, StringComparison.OrdinalIgnoreCase));
}

public sealed class InstallHealthInspector
{
    private readonly string _installRoot;
    private readonly Func<string> _readUserPath;
    private readonly Action<string> _writeUserPath;

    public InstallHealthInspector(
        string? installRoot = null,
        Func<string>? readUserPath = null,
        Action<string>? writeUserPath = null)
    {
        _installRoot = installRoot ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MRC");
        _readUserPath = readUserPath ?? (() => Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? string.Empty);
        _writeUserPath = writeUserPath ?? (value => Environment.SetEnvironmentVariable("PATH", value, EnvironmentVariableTarget.User));
    }

    public string InstallRoot => _installRoot;
    public string BinRoot => Path.Combine(_installRoot, "bin");
    public string CurrentVersionFile => Path.Combine(_installRoot, "current.version");
    public string LauncherPath => Path.Combine(BinRoot, "MRC.cmd");

    public InstallHealthSnapshot Inspect()
    {
        var userPath = _readUserPath();
        var pathEntries = SplitPath(userPath);
        var expectedNormalized = NormalizePath(BinRoot);
        var expectedCount = pathEntries.Count(entry => string.Equals(NormalizePath(entry), expectedNormalized, StringComparison.OrdinalIgnoreCase));
        var stale = pathEntries
            .Where(entry => LooksLikeMrcBin(entry) && !string.Equals(NormalizePath(entry), expectedNormalized, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        string? activeVersion = null;
        if (File.Exists(CurrentVersionFile))
        {
            activeVersion = File.ReadAllText(CurrentVersionFile).Trim();
            if (string.IsNullOrWhiteSpace(activeVersion)) activeVersion = null;
        }

        var versionsRoot = Path.Combine(_installRoot, "versions");
        var validVersions = Directory.Exists(versionsRoot)
            ? Directory.EnumerateDirectories(versionsRoot, "*", SearchOption.TopDirectoryOnly)
                .Where(IsValidVersionPayload)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray()
            : Array.Empty<string>();

        var activeRoot = activeVersion is null ? null : Path.Combine(versionsRoot, activeVersion);
        var activeValid = activeRoot is not null && IsValidVersionPayload(activeRoot);

        return new InstallHealthSnapshot(
            _installRoot,
            BinRoot,
            CurrentVersionFile,
            LauncherPath,
            activeVersion,
            activeRoot,
            expectedCount,
            stale,
            validVersions,
            File.Exists(LauncherPath),
            activeValid);
    }

    public void RepairUserPath()
    {
        var entries = SplitPath(_readUserPath())
            .Where(entry => !LooksLikeMrcBin(entry))
            .ToList();
        entries.Add(BinRoot);
        _writeUserPath(string.Join(Path.PathSeparator, entries));
    }

    public bool RepairActivePointer()
    {
        var snapshot = Inspect();
        var best = snapshot.ValidVersionRoots
            .Select(path => new { Path = path, Name = Path.GetFileName(path), Parsed = ParseVersion(Path.GetFileName(path)) })
            .Where(candidate => candidate.Parsed is not null)
            .OrderByDescending(candidate => candidate.Parsed)
            .FirstOrDefault();
        if (best is null) return false;

        Directory.CreateDirectory(_installRoot);
        var temp = CurrentVersionFile + ".doctor-" + Guid.NewGuid().ToString("N");
        File.WriteAllText(temp, best.Name);
        File.Move(temp, CurrentVersionFile, true);
        return true;
    }

    public void RepairLauncher()
    {
        Directory.CreateDirectory(BinRoot);
        File.WriteAllText(LauncherPath, LauncherText);
    }

    private static bool IsValidVersionPayload(string versionRoot) =>
        Directory.Exists(versionRoot)
        && File.Exists(Path.Combine(versionRoot, "MRC.exe"))
        && File.Exists(Path.Combine(versionRoot, "MRC.Gui.exe"));

    private bool LooksLikeMrcBin(string entry)
    {
        var normalized = NormalizePath(entry);
        if (string.IsNullOrWhiteSpace(normalized)) return false;
        if (string.Equals(normalized, NormalizePath(BinRoot), StringComparison.OrdinalIgnoreCase)) return true;
        return normalized.EndsWith(Path.DirectorySeparatorChar + "MRC" + Path.DirectorySeparatorChar + "bin", StringComparison.OrdinalIgnoreCase)
               || normalized.EndsWith(Path.AltDirectorySeparatorChar + "MRC" + Path.AltDirectorySeparatorChar + "bin", StringComparison.OrdinalIgnoreCase);
    }

    private static Version? ParseVersion(string? value) => Version.TryParse(value, out var parsed) ? parsed : null;

    private static List<string> SplitPath(string value) => value
        .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .ToList();

    private static string NormalizePath(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return value.Trim().Trim('"').TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    public const string LauncherText = "@echo off\r\nsetlocal\r\nset \"MRC_HOME=%LOCALAPPDATA%\\MRC\"\r\nif not exist \"%MRC_HOME%\\current.version\" (\r\n  echo MRC installation is incomplete: current.version is missing. 1>&2\r\n  exit /b 3\r\n)\r\nset /p MRC_VERSION=<\"%MRC_HOME%\\current.version\"\r\nset \"MRC_EXE=%MRC_HOME%\\versions\\%MRC_VERSION%\\MRC.exe\"\r\nif not exist \"%MRC_EXE%\" (\r\n  echo MRC installation is incomplete: %MRC_EXE% is missing. 1>&2\r\n  exit /b 3\r\n)\r\n\"%MRC_EXE%\" %*\r\nexit /b %ERRORLEVEL%\r\n";
}
