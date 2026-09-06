using System.Diagnostics;
using DebloatManager.Models;

namespace DebloatManager.Services;

public sealed class AppActionService
{
    public async Task<OperationResult> EndTaskAsync(AppItem item)
    {
        if (item.Type == AppType.BackgroundService)
        {
            var (success, _, error) = await PowerShellRunner.RunCommandAsync($"Stop-Service -Name '{item.ServiceName}' -Force");
            return success
                ? OperationResult.Ok($"Service '{item.Name}' stopped.")
                : OperationResult.Fail(string.IsNullOrWhiteSpace(error) ? "Failed to stop the service." : error);
        }

        if (item.ProcessIds.Count == 0)
        {
            return OperationResult.Fail($"'{item.Name}' is not currently running.");
        }

        var failures = new List<string>();
        foreach (var pid in item.ProcessIds.ToList())
        {
            try
            {
                using var process = Process.GetProcessById(pid);
                process.Kill(entireProcessTree: true);
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }
            catch (Exception ex)
            {
                failures.Add($"PID {pid}: {ex.Message}");
            }
        }

        item.ProcessIds.Clear();
        item.IsRunning = false;
        item.Status = "Installed";

        return failures.Count == 0
            ? OperationResult.Ok($"'{item.Name}' has been ended.")
            : OperationResult.Fail($"Some processes could not be ended: {string.Join("; ", failures)}");
    }

    public async Task<OperationResult> UninstallAsync(AppItem item)
    {
        OperationResult result = item.Type switch
        {
            AppType.UwpApp => await UninstallUwpAsync(item),
            AppType.Win32App => await UninstallWin32Async(item),
            AppType.SystemFeature => await DisableSystemFeatureAsync(item),
            AppType.BackgroundService => await DisableServiceAsync(item),
            _ => OperationResult.Fail("Unknown app type.")
        };

        if (result.Success)
        {
            item.IsRemoved = true;
            item.Status = "Removed";
        }

        return result;
    }

    private static async Task<OperationResult> UninstallUwpAsync(AppItem item)
    {
        if (string.IsNullOrEmpty(item.PackageFullName))
        {
            return OperationResult.Fail("Missing package identity.");
        }

        var script = $"""
        Remove-AppxPackage -Package '{item.PackageFullName}' -AllUsers -ErrorAction Stop
        """;
        var (success, _, error) = await PowerShellRunner.RunScriptAsync(script);

        if (!success)
        {
            return OperationResult.Fail(string.IsNullOrWhiteSpace(error) ? "Failed to remove the app." : error);
        }

        if (item.IsProvisioned)
        {
            await PowerShellRunner.RunScriptAsync($"Remove-AppxProvisionedPackage -Online -PackageName '{item.PackageFullName}' -ErrorAction SilentlyContinue");
        }

        return OperationResult.Ok($"'{item.Name}' has been removed.");
    }

    private static async Task<OperationResult> UninstallWin32Async(AppItem item)
    {
        if (string.IsNullOrEmpty(item.UninstallCommand))
        {
            return OperationResult.Fail("No uninstall command found for this application.");
        }

        try
        {
            var command = item.UninstallCommand!;
            var isMsiInstaller = command.Contains("msiexec", StringComparison.OrdinalIgnoreCase);
            if (isMsiInstaller && !item.UninstallIsQuiet && !command.Contains("/quiet", StringComparison.OrdinalIgnoreCase) && !command.Contains("/qn", StringComparison.OrdinalIgnoreCase))
            {
                command += " /quiet /norestart";
            }

            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"{command}\"",
                UseShellExecute = false,
                CreateNoWindow = false
            };

            using var process = Process.Start(psi);
            if (process is null)
            {
                return OperationResult.Fail("Failed to start the uninstaller.");
            }

            await process.WaitForExitAsync();

            return process.ExitCode == 0
                ? OperationResult.Ok($"'{item.Name}' uninstaller finished.")
                : OperationResult.Ok($"'{item.Name}' uninstaller exited with code {process.ExitCode}. Verify it was removed.");
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Failed to run the uninstaller: {ex.Message}");
        }
    }

    private static async Task<OperationResult> DisableSystemFeatureAsync(AppItem item)
    {
        if (string.IsNullOrEmpty(item.FeatureName))
        {
            return OperationResult.Fail("Missing feature name.");
        }

        var script = $"Disable-WindowsOptionalFeature -Online -FeatureName '{item.FeatureName}' -NoRestart -ErrorAction Stop";
        var (success, _, error) = await PowerShellRunner.RunScriptAsync(script);

        return success
            ? OperationResult.Ok($"'{item.Name}' has been disabled. A restart may be required.")
            : OperationResult.Fail(string.IsNullOrWhiteSpace(error) ? "Failed to disable the feature." : error);
    }

    private static async Task<OperationResult> DisableServiceAsync(AppItem item)
    {
        if (string.IsNullOrEmpty(item.ServiceName))
        {
            return OperationResult.Fail("Missing service name.");
        }

        var script = $"""
        Stop-Service -Name '{item.ServiceName}' -Force -ErrorAction SilentlyContinue
        Set-Service -Name '{item.ServiceName}' -StartupType Disabled -ErrorAction Stop
        """;
        var (success, _, error) = await PowerShellRunner.RunScriptAsync(script);

        return success
            ? OperationResult.Ok($"'{item.Name}' has been stopped and disabled.")
            : OperationResult.Fail(string.IsNullOrWhiteSpace(error) ? "Failed to disable the service." : error);
    }
}
