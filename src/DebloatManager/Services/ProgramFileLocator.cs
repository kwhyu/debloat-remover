using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace DebloatManager.Services;

public static partial class ProgramFileLocator
{
    private const int MaximumFolderDepth = 2;
    private const string InstallerProductsPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\S-1-5-18\Products";

    private static readonly SignatureResult NotChecked = new(Models.OriginStatus.NotChecked, null, null);

    // Windows and its installer cache strip or alter some files, so only a valid signature counts there.
    public static SignatureResult InspectBestFile(UninstallEntry entry, Func<string, SignatureResult>? inspect = null)
    {
        inspect ??= SignatureInspector.Inspect;

        foreach (var file in Candidates(entry))
        {
            var result = inspect(file);
            if (result.Status == Models.OriginStatus.Signed || !IsInWindowsFolder(file)) return result;
        }

        return NotChecked;
    }

    public static IEnumerable<string> Candidates(UninstallEntry entry)
    {
        var candidates = new[] { FromDisplayIcon(entry.DisplayIcon) }
            .Concat(FromInstallFolder(entry.InstallLocation, entry.DisplayName))
            .Append(FromUninstaller(entry))
            .Append(FromInstallerCache(entry));

        return candidates.OfType<string>().Where(IsCheckable).Distinct(StringComparer.OrdinalIgnoreCase);
    }

    // Windows Installer stores product codes with each group of hex digits reordered.
    public static string? ToInstallerProductKey(string? productCode)
    {
        if (productCode is null || !ProductCodePattern().IsMatch(productCode)) return null;

        var hex = productCode.Trim('{', '}').Replace("-", string.Empty).ToUpperInvariant();
        var key = Reverse(hex[..8]) + Reverse(hex[8..12]) + Reverse(hex[12..16]);
        for (var index = 16; index < hex.Length; index += 2)
        {
            key += hex[index + 1].ToString() + hex[index];
        }

        return key;
    }

    private static string Reverse(string text) => new(text.Reverse().ToArray());

    private static string? FromDisplayIcon(string? displayIcon)
    {
        if (string.IsNullOrWhiteSpace(displayIcon)) return null;

        var expanded = Environment.ExpandEnvironmentVariables(displayIcon).Trim();
        var comma = expanded.LastIndexOf(',');
        if (comma > 0 && int.TryParse(expanded[(comma + 1)..], out _)) expanded = expanded[..comma];

        return expanded.Trim().Trim('"');
    }

    private static IEnumerable<string> FromInstallFolder(string? installLocation, string? displayName)
    {
        var folder = LeftoverCleaner.Normalize(installLocation);
        if (folder is null || !Directory.Exists(folder)) return Array.Empty<string>();

        var name = LeftoverPlanner.CleanName(displayName);
        return ProgramsIn(folder, MaximumFolderDepth)
            .Where(file => !Path.GetFileName(file).Contains("unins", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(file => name.Contains(Path.GetFileNameWithoutExtension(file), StringComparison.OrdinalIgnoreCase))
            .Take(3);
    }

    private static IEnumerable<string> ProgramsIn(string folder, int depth)
    {
        string[] files;
        string[] folders;
        try
        {
            files = Directory.GetFiles(folder, "*.exe");
            folders = depth > 0 ? Directory.GetDirectories(folder) : Array.Empty<string>();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            yield break;
        }

        foreach (var file in files) yield return file;
        foreach (var child in folders)
        {
            foreach (var file in ProgramsIn(child, depth - 1)) yield return file;
        }
    }

    private static string? FromUninstaller(UninstallEntry entry)
        => UninstallCommandParser.Parse(entry.UninstallCommand, entry.KeyName, entry.IsWindowsInstaller)?.TargetFile;

    private static string? FromInstallerCache(UninstallEntry entry)
    {
        var key = ToInstallerProductKey(entry.KeyName);
        if (key is null) return null;

        using var properties = Registry.LocalMachine.OpenSubKey($@"{InstallerProductsPath}\{key}\InstallProperties");
        return properties?.GetValue("LocalPackage") as string;
    }

    private static bool IsCheckable(string path)
    {
        if (!Path.IsPathRooted(path) || !File.Exists(path)) return false;

        var extension = Path.GetExtension(path);
        return extension.Equals(".exe", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".msi", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".dll", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsInWindowsFolder(string path)
    {
        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        return path.StartsWith(windows + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"^\{[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}\}$")]
    private static partial Regex ProductCodePattern();
}
