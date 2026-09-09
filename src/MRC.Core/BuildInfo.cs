using System.Reflection;

namespace MRC.Core;

public static class BuildInfo
{
    public static string Version
    {
        get
        {
            var informational = typeof(BuildInfo).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;

            if (!string.IsNullOrWhiteSpace(informational))
            {
                var separator = informational.IndexOf('+', StringComparison.Ordinal);
                return separator >= 0 ? informational[..separator] : informational;
            }

            var assemblyVersion = typeof(BuildInfo).Assembly.GetName().Version;
            return assemblyVersion is null
                ? "unknown"
                : $"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}";
        }
    }
}
