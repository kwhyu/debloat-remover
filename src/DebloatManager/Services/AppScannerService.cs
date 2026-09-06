using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using DebloatManager.Models;
using Microsoft.Win32;

namespace DebloatManager.Services;

public sealed class AppScannerService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<List<AppItem>> ScanAllAsync(IProgress<string>? progress = null)
    {
        var items = new List<AppItem>();
        var uwpApps = new List<AppItem>();

        await RunStepAsync("UWP apps", progress, async () =>
        {
            uwpApps = await ScanUwpAppsAsync(progress);
            items.AddRange(uwpApps);
        });

        await RunStepAsync("provisioned packages", progress, async () => await MarkProvisionedAsync(uwpApps, progress));

        await RunStepAsync("installed programs", progress, () =>
        {
            var win32Apps = ScanWin32Apps().ToList();
            items.AddRange(win32Apps);
            progress?.Report($"Found {win32Apps.Count} installed programs.");
            return Task.CompletedTask;
        });

        await RunStepAsync("Windows optional features", progress, async () => items.AddRange(await ScanOptionalFeaturesAsync(progress)));

        await RunStepAsync("background services", progress, async () => items.AddRange(await ScanServicesAsync(progress)));

        try
        {
            progress?.Report("Matching running processes...");
            MatchRunningProcesses(items);
        }
        catch (Exception ex)
        {
            progress?.Report($"Process matching failed: {ex.Message}");
        }

        return items;
    }

    // Runs one scan step in isolation so a failure there (bad PowerShell output, JSON
    // shape mismatch, etc.) cannot discard results already found by other steps.
    private static async Task RunStepAsync(string stepName, IProgress<string>? progress, Func<Task> step)
    {
        try
        {
            await step();
        }
        catch (Exception ex)
        {
            progress?.Report($"Skipped {stepName} due to an error: {ex.Message}");
        }
    }

    private async Task<List<AppItem>> ScanUwpAppsAsync(IProgress<string>? progress)
    {
        // Get-AppxPackage without -AllUsers can return an empty result when the process is
        // elevated (a known Windows quirk), so prefer -AllUsers since this app always runs
        // as Administrator, and fall back to the per-user query if that is unavailable.
        const string script = """
        try {
            $apps = Get-AppxPackage -AllUsers -ErrorAction Stop
        } catch {
            $apps = Get-AppxPackage
        }
        $apps = $apps | Where-Object { -not $_.IsFramework -and -not $_.IsResourcePackage }
        $result = foreach ($app in $apps) {
            [PSCustomObject]@{
                Name = $app.Name
                PackageFullName = $app.PackageFullName
                PackageFamilyName = $app.PackageFamilyName
                Publisher = $app.Publisher
                Version = $app.Version.ToString()
                NonRemovable = [bool]$app.NonRemovable
                InstallLocation = $app.InstallLocation
                SignatureKind = $app.SignatureKind.ToString()
            }
        }
        @($result) | ConvertTo-Json -Compress -Depth 3
        """;

        var (success, output, error) = await PowerShellRunner.RunScriptAsync(script);
        if (!success)
        {
            progress?.Report($"UWP app scan failed: {(string.IsNullOrWhiteSpace(error) ? "unknown PowerShell error" : error)}");
            return new List<AppItem>();
        }

        if (string.IsNullOrWhiteSpace(output))
        {
            progress?.Report("UWP app scan returned no packages.");
            return new List<AppItem>();
        }

        var dtos = JsonSerializer.Deserialize<List<UwpAppDto>>(output, JsonOptions) ?? new();
        var result = new List<AppItem>();

        foreach (var dto in dtos)
        {
            if (string.IsNullOrWhiteSpace(dto.Name)) continue;

            var identifier = dto.PackageFamilyName ?? dto.Name;
            var publisherName = ExtractCommonName(dto.Publisher) ?? "Microsoft Corporation";
            var catalogMatch = BloatwareCatalog.Match(identifier) ?? BloatwareCatalog.Match(dto.Name)
                ?? BloatwareCatalog.ClassifyUwpFallback(dto.Name, publisherName, dto.NonRemovable, dto.SignatureKind);

            result.Add(new AppItem
            {
                Name = dto.Name,
                Publisher = publisherName,
                Version = dto.Version ?? "-",
                Type = AppType.UwpApp,
                Risk = catalogMatch?.Risk ?? RiskLevel.Unknown,
                Category = catalogMatch?.Category ?? "Uncategorized UWP App",
                Notes = catalogMatch?.Notes ?? string.Empty,
                PackageFullName = dto.PackageFullName,
                PackageFamilyName = dto.PackageFamilyName,
                NonRemovable = dto.NonRemovable,
                InstallLocation = dto.InstallLocation
            });
        }

        progress?.Report($"Found {result.Count} UWP apps.");
        return result;
    }

    private async Task MarkProvisionedAsync(List<AppItem> uwpApps, IProgress<string>? progress)
    {
        const string script = """
        @(Get-AppxProvisionedPackage -Online | Select-Object DisplayName, PackageName) | ConvertTo-Json -Compress
        """;

        var (success, output, error) = await PowerShellRunner.RunScriptAsync(script);
        if (!success)
        {
            progress?.Report($"Provisioned package scan skipped: {(string.IsNullOrWhiteSpace(error) ? "unknown PowerShell error" : error)}");
            return;
        }
        if (string.IsNullOrWhiteSpace(output)) return;

        var dtos = JsonSerializer.Deserialize<List<ProvisionedDto>>(output, JsonOptions) ?? new();
        var provisionedNames = dtos.Select(d => d.PackageName).Where(n => n is not null).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var app in uwpApps)
        {
            if (app.PackageFullName is not null && provisionedNames.Contains(app.PackageFullName))
            {
                app.IsProvisioned = true;
            }
        }
    }

    private static IEnumerable<AppItem> ScanWin32Apps()
    {
        var roots = new (RegistryKey Hive, string Path)[]
        {
            (Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"),
            (Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"),
            (Registry.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"),
        };

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (hive, path) in roots)
        {
            using var uninstallKey = hive.OpenSubKey(path);
            if (uninstallKey is null) continue;

            foreach (var subKeyName in uninstallKey.GetSubKeyNames())
            {
                using var subKey = uninstallKey.OpenSubKey(subKeyName);
                if (subKey is null) continue;

                var displayName = subKey.GetValue("DisplayName") as string;
                if (string.IsNullOrWhiteSpace(displayName)) continue;
                if (!seen.Add(displayName)) continue;

                if (subKey.GetValue("SystemComponent") is int systemComponent && systemComponent == 1) continue;

                var releaseType = subKey.GetValue("ReleaseType") as string;
                if (releaseType is "Hotfix" or "Update" or "Security Update" or "ServicePack") continue;

                var quietUninstall = subKey.GetValue("QuietUninstallString") as string;
                var uninstallString = quietUninstall ?? subKey.GetValue("UninstallString") as string;
                var publisher = subKey.GetValue("Publisher") as string ?? "Unknown";
                var version = subKey.GetValue("DisplayVersion") as string ?? "-";
                var installLocation = subKey.GetValue("InstallLocation") as string;

                var catalogMatch = BloatwareCatalog.Match(displayName) ?? BloatwareCatalog.Match(publisher)
                    ?? BloatwareCatalog.ClassifyWin32Fallback(displayName, publisher);

                yield return new AppItem
                {
                    Name = displayName,
                    Publisher = publisher,
                    Version = version,
                    Type = AppType.Win32App,
                    Risk = catalogMatch?.Risk ?? RiskLevel.Unknown,
                    Category = catalogMatch?.Category ?? "Third-Party Application",
                    Notes = catalogMatch?.Notes ?? string.Empty,
                    UninstallCommand = uninstallString,
                    UninstallIsQuiet = quietUninstall is not null,
                    InstallLocation = installLocation
                };
            }
        }
    }

    private async Task<List<AppItem>> ScanOptionalFeaturesAsync(IProgress<string>? progress)
    {
        const string script = """
        @(Get-WindowsOptionalFeature -Online | Where-Object { $_.State -eq 'Enabled' } | Select-Object FeatureName, DisplayName) | ConvertTo-Json -Compress
        """;

        var (success, output, error) = await PowerShellRunner.RunScriptAsync(script);
        if (!success)
        {
            progress?.Report($"Optional feature scan failed: {(string.IsNullOrWhiteSpace(error) ? "unknown PowerShell error" : error)}");
            return new List<AppItem>();
        }
        if (string.IsNullOrWhiteSpace(output)) return new List<AppItem>();

        var dtos = JsonSerializer.Deserialize<List<OptionalFeatureDto>>(output, JsonOptions) ?? new();
        var result = new List<AppItem>();

        foreach (var dto in dtos)
        {
            if (string.IsNullOrWhiteSpace(dto.FeatureName)) continue;
            if (!BloatwareCatalog.KnownOptionalFeatures.TryGetValue(dto.FeatureName, out var known)) continue;

            result.Add(new AppItem
            {
                Name = dto.DisplayName ?? dto.FeatureName,
                Publisher = "Microsoft Corporation",
                Type = AppType.SystemFeature,
                Risk = known.Risk,
                Category = known.Category,
                Notes = known.Notes,
                FeatureName = dto.FeatureName
            });
        }

        return result;
    }

    private async Task<List<AppItem>> ScanServicesAsync(IProgress<string>? progress)
    {
        var names = string.Join(",", BloatwareCatalog.KnownServices.Keys.Select(n => $"'{n}'"));
        var script = $$$"""
        @(Get-Service -Name {{{names}}} -ErrorAction SilentlyContinue | Select-Object Name, DisplayName, @{Name='Status';Expression={$_.Status.ToString()}}) | ConvertTo-Json -Compress
        """;

        var (success, output, error) = await PowerShellRunner.RunScriptAsync(script);
        if (!success)
        {
            progress?.Report($"Service scan failed: {(string.IsNullOrWhiteSpace(error) ? "unknown PowerShell error" : error)}");
            return new List<AppItem>();
        }
        if (string.IsNullOrWhiteSpace(output)) return new List<AppItem>();

        var dtos = JsonSerializer.Deserialize<List<ServiceDto>>(output, JsonOptions) ?? new();
        var result = new List<AppItem>();

        foreach (var dto in dtos)
        {
            if (string.IsNullOrWhiteSpace(dto.Name)) continue;
            if (!BloatwareCatalog.KnownServices.TryGetValue(dto.Name, out var known)) continue;

            var isRunning = string.Equals(dto.Status, "Running", StringComparison.OrdinalIgnoreCase);

            result.Add(new AppItem
            {
                Name = dto.DisplayName ?? dto.Name,
                Publisher = "Microsoft Corporation",
                Type = AppType.BackgroundService,
                Risk = known.Risk,
                Category = known.Category,
                Notes = known.Notes,
                ServiceName = dto.Name,
                IsRunning = isRunning,
                Status = isRunning ? "Running" : "Stopped"
            });
        }

        return result;
    }

    private static void MatchRunningProcesses(List<AppItem> items)
    {
        var pathsByPid = new List<(int Pid, string Path)>();

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                var path = process.MainModule?.FileName;
                if (!string.IsNullOrEmpty(path))
                {
                    pathsByPid.Add((process.Id, path));
                }
            }
            catch
            {
                // Access denied on protected/system processes, skip them.
            }
            finally
            {
                process.Dispose();
            }
        }

        foreach (var item in items)
        {
            if (item.Type == AppType.UwpApp && !string.IsNullOrEmpty(item.PackageFamilyName))
            {
                foreach (var (pid, path) in pathsByPid)
                {
                    if (path.Contains(item.PackageFamilyName!, StringComparison.OrdinalIgnoreCase))
                    {
                        item.ProcessIds.Add(pid);
                    }
                }
            }
            else if (item.Type == AppType.Win32App && !string.IsNullOrEmpty(item.InstallLocation))
            {
                foreach (var (pid, path) in pathsByPid)
                {
                    if (path.StartsWith(item.InstallLocation!, StringComparison.OrdinalIgnoreCase))
                    {
                        item.ProcessIds.Add(pid);
                    }
                }
            }

            if (item.ProcessIds.Count > 0)
            {
                item.IsRunning = true;
                item.Status = "Running";
            }
        }
    }

    private static string? ExtractCommonName(string? distinguishedName)
    {
        if (string.IsNullOrWhiteSpace(distinguishedName)) return null;

        var parts = distinguishedName.Split(',');
        var cn = parts.FirstOrDefault(p => p.TrimStart().StartsWith("CN=", StringComparison.OrdinalIgnoreCase));
        return cn?.Trim()[3..];
    }

    private sealed class UwpAppDto
    {
        public string? Name { get; set; }
        public string? PackageFullName { get; set; }
        public string? PackageFamilyName { get; set; }
        public string? Publisher { get; set; }
        public string? Version { get; set; }
        public bool NonRemovable { get; set; }
        public string? InstallLocation { get; set; }
        public string? SignatureKind { get; set; }
    }

    private sealed class ProvisionedDto
    {
        public string? DisplayName { get; set; }
        public string? PackageName { get; set; }
    }

    private sealed class OptionalFeatureDto
    {
        public string? FeatureName { get; set; }
        public string? DisplayName { get; set; }
    }

    private sealed class ServiceDto
    {
        public string? Name { get; set; }
        public string? DisplayName { get; set; }
        public string? Status { get; set; }
    }
}
