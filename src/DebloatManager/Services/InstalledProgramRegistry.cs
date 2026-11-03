using Microsoft.Win32;

namespace DebloatManager.Services;

public sealed record UninstallEntry(
    RegistryHive Hive,
    string KeyPath,
    string KeyName,
    string? DisplayName,
    string Publisher,
    string Version,
    string? InstallLocation,
    string? UninstallCommand,
    bool IsWindowsInstaller,
    bool IsHidden,
    string? DisplayIcon = null,
    string? Website = null,
    string? InstallDate = null);

public static class InstalledProgramRegistry
{
    private const string UninstallPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
    private const string Wow64UninstallPath = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall";

    private static readonly (RegistryHive Hive, string Path)[] Roots =
    {
        (RegistryHive.LocalMachine, UninstallPath),
        (RegistryHive.LocalMachine, Wow64UninstallPath),
        (RegistryHive.CurrentUser, UninstallPath),
        (RegistryHive.CurrentUser, Wow64UninstallPath),
    };

    public static List<UninstallEntry> ReadAll()
    {
        var entries = new List<UninstallEntry>();

        foreach (var (hive, path) in Roots)
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
            using var parent = baseKey.OpenSubKey(path);
            if (parent is null) continue;

            foreach (var name in parent.GetSubKeyNames())
            {
                using var key = parent.OpenSubKey(name);
                if (key is not null) entries.Add(ReadEntry(hive, $@"{path}\{name}", name, key));
            }
        }

        return entries;
    }

    public static bool EntryExists(RegistryHive hive, string keyPath)
    {
        using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
        using var key = baseKey.OpenSubKey(keyPath);
        return key is not null;
    }

    private static UninstallEntry ReadEntry(RegistryHive hive, string keyPath, string keyName, RegistryKey key)
    {
        var releaseType = key.GetValue("ReleaseType") as string;
        var isHidden = key.GetValue("SystemComponent") is int and 1
            || key.GetValue("ParentKeyName") is string
            || releaseType is "Hotfix" or "Update" or "Security Update" or "ServicePack";

        return new UninstallEntry(
            hive,
            keyPath,
            keyName,
            key.GetValue("DisplayName") as string,
            key.GetValue("Publisher") as string ?? "Unknown",
            key.GetValue("DisplayVersion") as string ?? "-",
            key.GetValue("InstallLocation") as string,
            key.GetValue("QuietUninstallString") as string ?? key.GetValue("UninstallString") as string,
            key.GetValue("WindowsInstaller") is int and 1,
            isHidden,
            key.GetValue("DisplayIcon") as string,
            key.GetValue("URLInfoAbout") as string ?? key.GetValue("HelpLink") as string,
            key.GetValue("InstallDate") as string);
    }
}
