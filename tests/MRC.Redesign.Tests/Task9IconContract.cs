using System.Runtime.CompilerServices;

internal static class Task9IconContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        var root = Directory.GetCurrentDirectory();
        var base64Path = Path.Combine(root, "src", "MRC.Gui", "Assets", "MRC.ico.b64");
        var generatorPath = Path.Combine(root, "scripts", "materialize-icon.ps1");
        var projectPath = Path.Combine(root, "src", "MRC.Gui", "MRC.Gui.csproj");
        var windowPath = Path.Combine(root, "src", "MRC.Gui", "MainWindow.xaml");
        var dialogPath = Path.Combine(root, "src", "MRC.Gui", "DiagnosticDialog.xaml");
        var packagePath = Path.Combine(root, "scripts", "package.ps1");
        var installPath = Path.Combine(root, "scripts", "install.ps1");

        Require(File.Exists(base64Path), "MRC icon authority src/MRC.Gui/Assets/MRC.ico.b64 is missing.");
        var iconBytes = Convert.FromBase64String(File.ReadAllText(base64Path).Trim());
        VerifyIcoHeaderAndSizes(iconBytes);

        Require(File.Exists(generatorPath), "scripts/materialize-icon.ps1 is missing.");
        var generator = File.ReadAllText(generatorPath);
        Require(generator.Contains("FromBase64String", StringComparison.Ordinal)
                && generator.Contains("MRC.ico.b64", StringComparison.Ordinal)
                && generator.Contains("MRC.ico", StringComparison.Ordinal),
            "Icon materializer does not decode the committed icon authority into MRC.ico.");

        var project = File.ReadAllText(projectPath);
        Require(project.Contains("<ApplicationIcon>Assets\\MRC.ico</ApplicationIcon>", StringComparison.Ordinal),
            "MRC.Gui.exe does not embed Assets\\MRC.ico as its application icon.");
        Require(project.Contains("MaterializeMrcIcon", StringComparison.Ordinal)
                && project.Contains("BeforeTargets=\"PrepareForBuild\"", StringComparison.Ordinal),
            "MRC.Gui.csproj does not materialize the icon before the real WPF build.");
        Require(project.Contains("Resource Include=\"Assets\\MRC.ico\"", StringComparison.Ordinal),
            "MRC.ico is not embedded as a WPF resource.");

        var window = File.ReadAllText(windowPath);
        Require(window.Contains("Icon=\"Assets/MRC.ico\"", StringComparison.Ordinal),
            "MainWindow does not explicitly use the MRC icon for titlebar/taskbar/Alt-Tab identity.");
        var dialog = File.ReadAllText(dialogPath);
        Require(dialog.Contains("Icon=\"Assets/MRC.ico\"", StringComparison.Ordinal),
            "DiagnosticDialog does not inherit the MRC icon identity.");

        var package = File.ReadAllText(packagePath);
        Require(package.Contains("MRC.ico", StringComparison.Ordinal)
                && package.Contains("$packageRoot", StringComparison.Ordinal),
            "Release packaging does not carry MRC.ico alongside the install authority.");

        var install = File.ReadAllText(installPath);
        Require(install.Contains("Main Runner Control.lnk", StringComparison.Ordinal)
                && install.Contains("CreateShortcut", StringComparison.Ordinal)
                && install.Contains("IconLocation", StringComparison.Ordinal)
                && install.Contains("MRC.ico", StringComparison.Ordinal)
                && install.Contains("$launcherPath", StringComparison.Ordinal),
            "Installer does not create the stable Main Runner Control shortcut with the MRC icon.");

        Console.WriteLine("PASS  Task9B unified application icon authority");
    }

    private static void VerifyIcoHeaderAndSizes(byte[] bytes)
    {
        Require(bytes.Length > 128, "MRC icon authority is implausibly small.");
        Require(bytes[0] == 0 && bytes[1] == 0 && bytes[2] == 1 && bytes[3] == 0,
            "MRC icon authority is not a valid ICO header.");
        var count = BitConverter.ToUInt16(bytes, 4);
        Require(count >= 4, $"MRC icon must contain at least 4 image sizes; found {count}.");

        var sizes = new HashSet<int>();
        for (var index = 0; index < count; index++)
        {
            var offset = 6 + (index * 16);
            Require(offset + 16 <= bytes.Length, "ICO directory is truncated.");
            var width = bytes[offset] == 0 ? 256 : bytes[offset];
            var height = bytes[offset + 1] == 0 ? 256 : bytes[offset + 1];
            if (width == height) sizes.Add(width);
        }

        foreach (var required in new[] { 16, 32, 48, 256 })
        {
            Require(sizes.Contains(required), $"MRC icon authority is missing the required {required}x{required} image.");
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
