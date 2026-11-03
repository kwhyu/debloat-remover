using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using DebloatManager.Models;
using Microsoft.Win32;

namespace DebloatManager.Services;

public sealed class ProgramUninstaller
{
    private static readonly TimeSpan UninstallTimeout = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);

    private readonly TimeSpan confirmationWindow;

    public ProgramUninstaller(TimeSpan? confirmationWindow = null)
    {
        this.confirmationWindow = confirmationWindow ?? TimeSpan.FromSeconds(20);
    }

    public async Task<OperationResult> RunAsync(AppItem item, IReadOnlyList<LeftoverFolder>? foldersToClean = null)
    {
        if (item.UninstallHive is not { } hive || string.IsNullOrEmpty(item.UninstallKeyPath))
        {
            return OperationResult.Fail("The registry entry for this program is missing.");
        }

        if (!InstalledProgramRegistry.EntryExists(hive, item.UninstallKeyPath))
        {
            return await FinishAsync(item, "is already gone", false, foldersToClean);
        }

        var keyName = item.UninstallKeyPath[(item.UninstallKeyPath.LastIndexOf('\\') + 1)..];
        var invocation = UninstallCommandParser.Parse(item.UninstallCommand, keyName, item.IsWindowsInstaller);
        if (invocation is null)
        {
            return OperationResult.Fail("This program did not register an uninstaller.");
        }

        if (Path.IsPathRooted(invocation.FileName) && !File.Exists(invocation.FileName))
        {
            return OperationResult.Fail($"The uninstaller file is missing: {invocation.FileName}");
        }

        foreach (var folder in LeftoverPlanner.ForItem(item).Where(f => f.IsInstallFolder && f.CanDelete))
        {
            StopProcessesUnder(folder.Path);
        }

        ProcessTreeResult result;
        try
        {
            result = await ProcessTreeRunner.RunAsync(BuildStartInfo(invocation), UninstallTimeout);
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            return OperationResult.Fail($"The uninstaller could not start: {ex.Message}");
        }

        if (result.TimedOut)
        {
            return OperationResult.Fail("Tried to uninstall, but the uninstaller did not finish within 30 minutes.");
        }

        if (!await WaitForEntryRemovalAsync(hive, item.UninstallKeyPath))
        {
            return OperationResult.Fail(DescribeFailure(invocation, result.ExitCode));
        }

        var restart = invocation.IsMsi && result.ExitCode is 3010 or 1641;
        return await FinishAsync(item, "was removed and verified", restart, foldersToClean);
    }

    private static async Task<OperationResult> FinishAsync(AppItem item, string outcome, bool restart, IReadOnlyList<LeftoverFolder>? foldersToClean)
    {
        var message = $"'{item.Name}' {outcome}.";

        if (foldersToClean is null)
        {
            var kept = Directory.Exists(item.InstallLocation) ? $" Its folder was left in place: {item.InstallLocation}" : string.Empty;
            return OperationResult.Ok(message + kept, restart);
        }

        var deleted = 0;
        var issues = new List<string>();

        foreach (var folder in foldersToClean)
        {
            if (!Directory.Exists(folder.Path)) continue;

            if (!folder.CanDelete)
            {
                issues.Add($"Not deleted: {folder.Path}. {folder.Reason}");
                continue;
            }

            var result = await LeftoverCleaner.DeleteFolderAsync(folder.Path);
            if (result.Success)
            {
                deleted++;
            }
            else
            {
                issues.Add($"Tried to delete {folder.Path}, but {result.RemainingItems} item(s) remain. {result.FirstError}".TrimEnd());
            }
        }

        if (deleted > 0) message += $" Deleted {deleted} leftover folder(s).";
        return OperationResult.Ok(message, restart, issues);
    }

    private static ProcessStartInfo BuildStartInfo(UninstallInvocation invocation)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = invocation.FileName,
            Arguments = invocation.Arguments,
            UseShellExecute = false
        };

        var directory = Path.GetDirectoryName(invocation.TargetFile ?? invocation.FileName);
        if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
        {
            startInfo.WorkingDirectory = directory;
        }

        return startInfo;
    }

    private async Task<bool> WaitForEntryRemovalAsync(RegistryHive hive, string keyPath)
    {
        var deadline = DateTime.UtcNow + confirmationWindow;
        while (DateTime.UtcNow < deadline)
        {
            if (!InstalledProgramRegistry.EntryExists(hive, keyPath)) return true;
            await Task.Delay(PollInterval);
        }

        return !InstalledProgramRegistry.EntryExists(hive, keyPath);
    }

    private static string DescribeFailure(UninstallInvocation invocation, int exitCode)
    {
        if (invocation.IsMsi)
        {
            return exitCode switch
            {
                1602 or 1223 => "The uninstall was cancelled.",
                1618 => "Another installation is running. Wait for it to finish and try again.",
                3010 or 1641 => "Restart Windows to finish removing this program, then scan again.",
                _ => $"Tried to uninstall, but Windows Installer could not remove this program (code {exitCode})."
            };
        }

        return $"Tried to uninstall (code {exitCode}), but the program is still registered. It may have been cancelled, may need a restart, or may still be running in its own window.";
    }

    private static void StopProcessesUnder(string folder)
    {
        var prefix = folder.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                if (process.Id == Environment.ProcessId) continue;

                var path = process.MainModule?.FileName;
                if (path is not null && path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or NotSupportedException)
            {
                // Protected or already exited processes cannot be inspected.
            }
            finally
            {
                process.Dispose();
            }
        }
    }
}
