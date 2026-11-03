using System.IO;
using System.Text.RegularExpressions;
using DebloatManager.Models;

namespace DebloatManager.Services;

public sealed record LeftoverFolder(string Path, bool CanDelete, string? Reason, bool IsInstallFolder);

public sealed record LeftoverContext(
    IReadOnlyList<string> InstallRoots,
    IReadOnlyList<string> DataRoots,
    IReadOnlyList<string> OtherLocations,
    IReadOnlySet<string> OtherNames,
    Func<string, bool> DirectoryExists);

public static partial class LeftoverPlanner
{
    private const int MinimumNameLength = 3;
    private const string SharedReason = "Another program or Windows may use this folder.";
    private const string VendorReason = "This looks like a folder shared by the publisher's products.";
    private const string UnconfirmedReason = "Could not confirm that it belongs to this program.";

    public static IReadOnlyList<LeftoverFolder> ForItem(AppItem item)
    {
        var entries = InstalledProgramRegistry.ReadAll()
            .Where(entry => !string.Equals(entry.KeyPath, item.UninstallKeyPath, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var otherLocations = entries.Select(entry => entry.InstallLocation)
            .Concat(entries.Select(entry => UninstallerFolder(entry.UninstallCommand, entry.KeyName, entry.IsWindowsInstaller)))
            .OfType<string>()
            .ToList();

        var otherNames = entries.Select(entry => CleanName(entry.DisplayName))
            .Where(name => name.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var context = new LeftoverContext(LeftoverCleaner.InstallRoots(), LeftoverCleaner.DataRoots(), otherLocations, otherNames, Directory.Exists);
        var uninstaller = UninstallCommandParser.Parse(item.UninstallCommand, item.Identifier, item.IsWindowsInstaller)?.TargetFile;
        return Plan(item.Name, item.Publisher, item.InstallLocation, uninstaller, context);
    }

    public static IReadOnlyList<LeftoverFolder> Plan(string name, string publisher, string? installLocation, string? uninstallerFile, LeftoverContext context)
    {
        var folders = new List<LeftoverFolder>();
        var cleanName = CleanName(name);
        var dataNames = new List<string> { cleanName };

        var installFolder = LeftoverCleaner.Normalize(installLocation) ?? FolderOfUninstaller(uninstallerFile, cleanName);
        if (installFolder is not null && context.DirectoryExists(installFolder))
        {
            var evaluated = EvaluateInstallFolder(installFolder, cleanName, publisher, context);
            folders.Add(evaluated);

            // A folder name only identifies data when the folder itself clearly belongs to this program.
            if (evaluated.CanDelete && FolderNameMatches(installFolder, cleanName))
            {
                dataNames.Add(Path.GetFileName(installFolder));
            }
        }

        foreach (var path in DataFolderCandidates(dataNames, publisher, context))
        {
            if (!context.DirectoryExists(path) || folders.Any(f => f.Path.Equals(path, StringComparison.OrdinalIgnoreCase))) continue;

            var usedByOther = context.OtherNames.Contains(Path.GetFileName(path));
            var canDelete = !usedByOther && LeftoverCleaner.IsSafeToDelete(path, context.DataRoots, context.OtherLocations);
            folders.Add(new LeftoverFolder(path, canDelete, canDelete ? null : SharedReason, false));
        }

        return folders;
    }

    public static string CleanName(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName)) return string.Empty;

        var withoutNoise = NoisePattern().Replace(displayName, " ");
        return WhitespacePattern().Replace(withoutNoise, " ").Trim(' ', '-', '_', ',');
    }

    private static LeftoverFolder EvaluateInstallFolder(string path, string cleanName, string publisher, LeftoverContext context)
    {
        if (IsVendorFolder(path, cleanName, publisher))
        {
            return new LeftoverFolder(path, false, VendorReason, true);
        }

        bool canDelete;
        string? reason;

        if (LeftoverCleaner.IsInsideAny(path, context.InstallRoots))
        {
            canDelete = LeftoverCleaner.IsSafeToDelete(path, context.InstallRoots, context.OtherLocations);
            reason = canDelete ? null : SharedReason;
        }
        else if (LeftoverCleaner.OverlapsAny(path, context.OtherLocations) || LeftoverCleaner.ContainsThisApp(path) || IsSystemLocation(path))
        {
            canDelete = false;
            reason = SharedReason;
        }
        else
        {
            canDelete = FolderNameMatches(path, cleanName);
            reason = canDelete ? null : UnconfirmedReason;
        }

        return new LeftoverFolder(path, canDelete, reason, true);
    }

    // Folders named after the publisher, such as Program Files\Adobe, usually hold other products too.
    private static bool IsVendorFolder(string path, string cleanName, string publisher)
    {
        var folder = Compact(Path.GetFileName(path));
        if (folder.Length < MinimumNameLength || folder.Equals(Compact(cleanName), StringComparison.OrdinalIgnoreCase)) return false;

        return Compact(publisher).Contains(folder, StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> DataFolderCandidates(IEnumerable<string> dataNames, string publisher, LeftoverContext context)
    {
        var names = dataNames
            .Where(name => name.Length >= MinimumNameLength)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var vendors = new[] { publisher, publisher.Split(' ', ',')[0] }
            .Where(vendor => vendor.Length >= MinimumNameLength && !vendor.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var root in context.DataRoots)
        {
            foreach (var name in names)
            {
                yield return Path.Combine(root, name);
                foreach (var vendor in vendors) yield return Path.Combine(root, vendor, name);
            }
        }
    }

    private static string? UninstallerFolder(string? command, string keyName, bool isWindowsInstaller)
    {
        var target = UninstallCommandParser.Parse(command, keyName, isWindowsInstaller)?.TargetFile;
        return target is null ? null : LeftoverCleaner.Normalize(Path.GetDirectoryName(target));
    }

    // Launchers such as steam.exe also uninstall games, so only a matching uninstaller identifies a folder.
    private static string? FolderOfUninstaller(string? uninstallerFile, string cleanName)
    {
        if (string.IsNullOrEmpty(uninstallerFile)) return null;
        if (!Path.GetFileName(uninstallerFile).Contains("unins", StringComparison.OrdinalIgnoreCase)) return null;

        var folder = LeftoverCleaner.Normalize(Path.GetDirectoryName(uninstallerFile));
        return folder is not null && FolderNameMatches(folder, cleanName) ? folder : null;
    }

    private static bool IsSystemLocation(string path)
    {
        if (Path.GetDirectoryName(path) is null) return true;

        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var profiles = Path.GetDirectoryName(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        return LeftoverCleaner.IsInsideAny(path, new[] { windows, profiles }.OfType<string>());
    }

    private static bool FolderNameMatches(string path, string cleanName)
    {
        var folder = Compact(Path.GetFileName(path));
        var name = Compact(cleanName);
        if (folder.Length < MinimumNameLength || name.Length < MinimumNameLength) return false;

        return folder.Contains(name, StringComparison.OrdinalIgnoreCase) || name.Contains(folder, StringComparison.OrdinalIgnoreCase);
    }

    private static string Compact(string text) => NonAlphanumericPattern().Replace(text, string.Empty);

    [GeneratedRegex(@"\([^)]*\)|\bv?\d+(\.\d+)+\S*|\b(x64|x86|64-bit|32-bit)\b", RegexOptions.IgnoreCase)]
    private static partial Regex NoisePattern();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();

    [GeneratedRegex(@"[^\p{L}\p{N}]")]
    private static partial Regex NonAlphanumericPattern();
}
