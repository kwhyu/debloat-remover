using System.IO;

namespace DebloatManager.Services;

public sealed record FolderDeleteResult(bool Success, int RemainingItems, string? FirstError);

public static class LeftoverCleaner
{
    private static readonly HashSet<string> SharedFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Common Files", "Microsoft", "Microsoft Shared", "Microsoft.NET", "WindowsApps", "Windows Defender",
        "Windows Defender Advanced Threat Protection", "Windows Mail", "Windows Media Player", "Windows NT",
        "Windows Photo Viewer", "Windows Security", "WindowsPowerShell", "Internet Explorer", "ModifiableWindowsApps",
        "dotnet", "Packages", "Temp", "Programs"
    };

    public static IReadOnlyList<string> InstallRoots() => new[]
    {
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
    };

    public static IReadOnlyList<string> DataRoots() => new[]
    {
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)
    };

    // A folder is only eligible when it belongs to one program and sits inside a known root.
    public static bool IsSafeToDelete(string? path, IEnumerable<string> roots, IEnumerable<string> otherInstallLocations)
    {
        var target = Normalize(path);
        if (target is null) return false;

        var insideRoot = false;
        foreach (var root in roots.Select(Normalize).OfType<string>())
        {
            if (!IsInside(target, root)) continue;

            var segments = target[(root.Length + 1)..].Split(Path.DirectorySeparatorChar);
            if (SharedFolders.Contains(segments[0]) && !IsProgramsSubfolder(segments)) return false;
            insideRoot = true;
        }

        return insideRoot && !ContainsThisApp(target) && !OverlapsAny(target, otherInstallLocations);
    }

    public static bool IsInsideAny(string path, IEnumerable<string> roots)
        => roots.Select(Normalize).OfType<string>().Any(root => path.Equals(root, StringComparison.OrdinalIgnoreCase) || IsInside(path, root));

    public static bool OverlapsAny(string path, IEnumerable<string> others)
        => others.Select(Normalize).OfType<string>().Any(other =>
            other.Equals(path, StringComparison.OrdinalIgnoreCase) || IsInside(other, path) || IsInside(path, other));

    public static bool ContainsThisApp(string path)
    {
        var ownBase = Normalize(AppContext.BaseDirectory);
        return ownBase is not null && (path.Equals(ownBase, StringComparison.OrdinalIgnoreCase) || IsInside(ownBase, path));
    }

    public static async Task<FolderDeleteResult> DeleteFolderAsync(string path)
    {
        var result = DeleteOnce(path);
        if (result.Success) return result;

        // Files are often still locked for a moment after an uninstaller exits.
        await Task.Delay(TimeSpan.FromMilliseconds(1500));
        return DeleteOnce(path);
    }

    public static string? Normalize(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;

        try
        {
            var trimmed = path.Trim().Trim('"');
            if (!Path.IsPathRooted(trimmed)) return null;

            var full = Path.GetFullPath(trimmed);
            return full.Length > 3 ? full.TrimEnd(Path.DirectorySeparatorChar) : full;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }

    private static FolderDeleteResult DeleteOnce(string path)
    {
        if (!Directory.Exists(path)) return new FolderDeleteResult(true, 0, null);

        var errors = new List<string>();
        var remaining = DeleteContents(path, errors);
        if (remaining == 0) TryRun(() => Directory.Delete(path), errors);

        var success = !Directory.Exists(path);
        return new FolderDeleteResult(success, success ? 0 : Math.Max(remaining, 1), errors.FirstOrDefault());
    }

    // Links are removed without following them, so their targets are never touched.
    private static int DeleteContents(string folder, List<string> errors)
    {
        var remaining = 0;
        string[] entries;
        try
        {
            entries = Directory.GetFileSystemEntries(folder);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            errors.Add(ex.Message);
            return 1;
        }

        foreach (var entry in entries)
        {
            var attributes = File.GetAttributes(entry);
            var isDirectory = attributes.HasFlag(FileAttributes.Directory);

            if (isDirectory && !attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                remaining += DeleteContents(entry, errors);
                if (Directory.GetFileSystemEntries(entry).Length > 0) continue;
            }

            var deleted = TryRun(() =>
            {
                File.SetAttributes(entry, FileAttributes.Normal);
                if (isDirectory) Directory.Delete(entry);
                else File.Delete(entry);
            }, errors);

            if (!deleted) remaining++;
        }

        return remaining;
    }

    private static bool TryRun(Action action, List<string> errors)
    {
        try
        {
            action();
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            errors.Add(ex.Message);
            return false;
        }
    }

    private static bool IsProgramsSubfolder(string[] segments)
        => segments.Length >= 2 && segments[0].Equals("Programs", StringComparison.OrdinalIgnoreCase);

    private static bool IsInside(string path, string parent)
        => path.StartsWith(parent.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
}
