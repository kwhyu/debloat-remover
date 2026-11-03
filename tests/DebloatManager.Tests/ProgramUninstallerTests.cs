using System.Diagnostics;
using System.IO;
using DebloatManager.Models;
using DebloatManager.Services;
using Microsoft.Win32;
using Xunit;

namespace DebloatManager.Tests;

// Uses only a throwaway per-user registry entry and folders that each test creates and deletes.
public sealed class ProgramUninstallerTests : IDisposable
{
    private readonly string id = $"DebloatManagerTest-{Guid.NewGuid():N}";
    private readonly string keyPath;
    private readonly string folder;
    private readonly string scriptFolder;

    public ProgramUninstallerTests()
    {
        keyPath = $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{id}";
        folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", id);
        scriptFolder = Path.Combine(Path.GetTempPath(), id);
        Directory.CreateDirectory(folder);
        Directory.CreateDirectory(scriptFolder);
        using var key = Registry.CurrentUser.CreateSubKey(keyPath);
        key.SetValue("DisplayName", id);
    }

    public void Dispose()
    {
        Registry.CurrentUser.DeleteSubKeyTree(keyPath, throwOnMissingSubKey: false);
        if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        if (Directory.Exists(scriptFolder)) Directory.Delete(scriptFolder, recursive: true);
    }

    private string DeleteKeyCommand => $"reg delete \"HKCU\\{keyPath}\" /f >nul";

    private string WriteUninstaller(string body)
    {
        var path = Path.Combine(scriptFolder, "remover.bat");
        File.WriteAllText(path, $"@echo off\r\n{body}\r\n");
        return path;
    }

    private AppItem BuildItem(string command, string? installLocation = null) => new()
    {
        Name = id,
        Identifier = id,
        Type = AppType.Win32App,
        UninstallCommand = command,
        UninstallHive = RegistryHive.CurrentUser,
        UninstallKeyPath = keyPath,
        InstallLocation = installLocation
    };

    private static ProgramUninstaller Uninstaller() => new(TimeSpan.FromSeconds(2));

    private LeftoverFolder Planned(string path, bool canDelete = true, string? reason = null) => new(path, canDelete, reason, true);

    [Fact]
    public async Task ReportsSuccessOnlyWhenTheEntryIsGone()
    {
        var script = WriteUninstaller(DeleteKeyCommand);

        var result = await Uninstaller().RunAsync(BuildItem($"\"{script}\""));

        Assert.True(result.Success, result.Message);
        Assert.False(InstalledProgramRegistry.EntryExists(RegistryHive.CurrentUser, keyPath));
    }

    [Fact]
    public async Task ReportsFailureWhenUninstallerExitsCleanlyButProgramRemains()
    {
        var script = WriteUninstaller("exit /b 0");

        var result = await Uninstaller().RunAsync(BuildItem($"\"{script}\""));

        Assert.False(result.Success);
        Assert.StartsWith("Tried to uninstall", result.Message);
    }

    [Fact]
    public async Task WaitsForDetachedChildOfTheUninstaller()
    {
        var script = WriteUninstaller($"start \"\" /b cmd /c \"ping -n 8 127.0.0.1 >nul & {DeleteKeyCommand}\"\r\nexit /b 0");

        var watch = Stopwatch.StartNew();
        var result = await Uninstaller().RunAsync(BuildItem($"\"{script}\""));

        Assert.True(result.Success, result.Message);
        Assert.True(watch.Elapsed > TimeSpan.FromSeconds(4), "Should outlast the 2 second confirmation window.");
    }

    [Fact]
    public async Task LeavesFoldersWhenCleanupIsNotChosen()
    {
        var script = WriteUninstaller(DeleteKeyCommand);
        File.WriteAllText(Path.Combine(folder, "leftover.txt"), "x");

        var result = await Uninstaller().RunAsync(BuildItem($"\"{script}\"", folder));

        Assert.True(result.Success, result.Message);
        Assert.True(File.Exists(Path.Combine(folder, "leftover.txt")));
        Assert.Contains("left in place", result.Message);
    }

    [Fact]
    public async Task DeletesPlannedFoldersWhenCleanupIsChosen()
    {
        var script = WriteUninstaller(DeleteKeyCommand);
        var data = Directory.CreateDirectory(Path.Combine(folder, "data"));
        var file = Path.Combine(data.FullName, "leftover.txt");
        File.WriteAllText(file, "x");
        File.SetAttributes(file, FileAttributes.ReadOnly);

        var result = await Uninstaller().RunAsync(BuildItem($"\"{script}\"", folder), new[] { Planned(folder) });

        Assert.True(result.Success, result.Message);
        Assert.Empty(result.Issues);
        Assert.False(Directory.Exists(folder));
    }

    [Fact]
    public async Task ReportsLockedFilesInsteadOfClaimingACleanRemoval()
    {
        var script = WriteUninstaller(DeleteKeyCommand);
        File.WriteAllText(Path.Combine(folder, "free.txt"), "x");
        var locked = Path.Combine(folder, "locked.txt");
        using var handle = new FileStream(locked, FileMode.Create, FileAccess.ReadWrite, FileShare.None);

        var item = BuildItem($"\"{script}\"", folder);
        var result = await Uninstaller().RunAsync(item, new[] { Planned(folder) });

        Assert.True(result.Success, result.Message);
        var issue = Assert.Single(result.Issues);
        Assert.StartsWith("Tried to delete", issue);
        Assert.False(File.Exists(Path.Combine(folder, "free.txt")));
        Assert.True(File.Exists(locked));
    }

    [Fact]
    public async Task ReportsFoldersThatWereKeptForSafety()
    {
        var script = WriteUninstaller(DeleteKeyCommand);

        var result = await Uninstaller().RunAsync(BuildItem($"\"{script}\"", folder), new[] { Planned(folder, false, "Shared.") });

        Assert.StartsWith("Not deleted", Assert.Single(result.Issues));
        Assert.True(Directory.Exists(folder));
    }

    [Fact]
    public async Task NeverDeletesAnythingWhenUninstallFails()
    {
        var script = WriteUninstaller("exit /b 1");

        var result = await Uninstaller().RunAsync(BuildItem($"\"{script}\"", folder), new[] { Planned(folder) });

        Assert.False(result.Success);
        Assert.True(Directory.Exists(folder));
    }

    [Fact]
    public async Task DeletingAFolderDoesNotFollowLinks()
    {
        var target = Path.Combine(scriptFolder, "target");
        Directory.CreateDirectory(target);
        var precious = Path.Combine(target, "precious.txt");
        File.WriteAllText(precious, "x");
        var link = Path.Combine(folder, "link");

        using (var mklink = Process.Start(new ProcessStartInfo("cmd.exe", $"/c mklink /J \"{link}\" \"{target}\"") { CreateNoWindow = true, UseShellExecute = false })!)
        {
            await mklink.WaitForExitAsync();
        }

        Assert.True(Directory.Exists(link));

        var result = await LeftoverCleaner.DeleteFolderAsync(folder);

        Assert.True(result.Success, result.FirstError);
        Assert.False(Directory.Exists(folder));
        Assert.True(File.Exists(precious));
    }

    [Fact]
    public async Task ReportsMissingUninstallerFile()
    {
        var result = await Uninstaller().RunAsync(BuildItem(Path.Combine(folder, "missing.exe")));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ActionServiceShowsWhenFilesRemain()
    {
        var script = WriteUninstaller(DeleteKeyCommand);
        var item = BuildItem($"\"{script}\"", folder);

        var result = await new AppActionService().RemoveAsync(item, new[] { Planned(folder, false, "Shared.") });

        Assert.True(result.Success, result.Message);
        Assert.True(item.IsRemoved);
        Assert.Equal("Files remain", item.Status);
    }

    [Fact]
    public async Task ActionServiceMarksItemFromVerifiedResult()
    {
        var failing = BuildItem($"\"{WriteUninstaller("exit /b 0")}\"");

        var result = await new AppActionService().RemoveAsync(failing);

        Assert.False(result.Success);
        Assert.False(failing.IsRemoved);
        Assert.Equal("Failed", failing.Status);
    }
}
