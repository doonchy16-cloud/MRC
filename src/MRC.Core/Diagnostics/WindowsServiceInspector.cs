using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace MRC.Core.Diagnostics;

public sealed class WindowsServiceInspector
{
    private static readonly Regex StateRegex = new(@"STATE\s*:\s*\d+\s+(?<state>[A-Z_]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex PidRegex = new(@"PID\s*:\s*(?<pid>\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public IReadOnlyList<WindowsServiceInfo> Inspect()
    {
        if (!OperatingSystem.IsWindows()) return Array.Empty<WindowsServiceInfo>();

        var results = new List<WindowsServiceInfo>();
        try
        {
            using var services = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services");
            if (services is null) return results;

            foreach (var serviceName in services.GetSubKeyNames()
                         .Where(name => name.StartsWith("actions.runner.", StringComparison.OrdinalIgnoreCase))
                         .OrderBy(name => name, StringComparer.OrdinalIgnoreCase))
            {
                using var service = services.OpenSubKey(serviceName);
                if (service is null) continue;

                var displayName = service.GetValue("DisplayName") as string;
                var imagePath = service.GetValue("ImagePath") as string;
                var objectName = service.GetValue("ObjectName") as string;
                var startValue = service.GetValue("Start");
                var startMode = StartMode(startValue);
                var status = QueryStatus(serviceName);

                results.Add(new WindowsServiceInfo(
                    serviceName,
                    displayName,
                    status.State,
                    startMode,
                    objectName,
                    imagePath,
                    status.ProcessId));
            }
        }
        catch
        {
            // Diagnostics must remain read-only and best-effort. Missing service metadata is surfaced by absence.
        }

        return results;
    }

    private static (string State, int? ProcessId) QueryStatus(string serviceName)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "sc.exe",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add("queryex");
            startInfo.ArgumentList.Add(serviceName);

            using var process = Process.Start(startInfo);
            if (process is null) return ("UNKNOWN", null);
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(3000);

            var state = StateRegex.Match(output);
            var pid = PidRegex.Match(output);
            int? processId = null;
            if (pid.Success && int.TryParse(pid.Groups["pid"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) && parsed > 0)
            {
                processId = parsed;
            }

            return (state.Success ? state.Groups["state"].Value.ToUpperInvariant() : "UNKNOWN", processId);
        }
        catch
        {
            return ("UNKNOWN", null);
        }
    }

    private static string StartMode(object? raw) => raw switch
    {
        0 => "Boot",
        1 => "System",
        2 => "Auto",
        3 => "Manual",
        4 => "Disabled",
        _ => "Unknown"
    };
}
