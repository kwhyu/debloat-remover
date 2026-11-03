using System.Text.Json;
using DebloatManager.Models;

namespace DebloatManager.Services;

public sealed class AppScannerService
{
    public const string StoreAppsScript = """
        $provisioned = @{}
        try { Get-AppxProvisionedPackage -Online | ForEach-Object { $provisioned[$_.DisplayName] = $true } } catch { }
        try { $apps = Get-AppxPackage -AllUsers -ErrorAction Stop } catch { $apps = Get-AppxPackage }
        $result = foreach ($app in ($apps | Where-Object { -not $_.IsFramework -and -not $_.IsResourcePackage })) {
            $users = @($app.PackageUserInformation)
            $active = $users.Count -eq 0
            foreach ($user in $users) {
                if (@('Staged', 'NotInstalled') -notcontains "$($user.InstallState)") { $active = $true }
            }
            [PSCustomObject]@{
                Name = $app.Name
                Publisher = $app.Publisher
                PublisherDisplayName = $(try { ([xml](Get-Content -LiteralPath (Join-Path $app.InstallLocation 'AppxManifest.xml') -Raw -ErrorAction Stop)).Package.Properties.PublisherDisplayName } catch { $null })
                Version = "$($app.Version)"
                NonRemovable = [bool]$app.NonRemovable
                InstallLocation = $app.InstallLocation
                SignatureKind = "$($app.SignatureKind)"
                Present = ($active -or $provisioned.ContainsKey($app.Name))
            }
        }
        ConvertTo-Json -InputObject @($result) -Compress -Depth 3
        """;

    public const string FeaturesScript = """
        $enabled = @(Get-WindowsOptionalFeature -Online | Where-Object { "$($_.State)" -eq 'Enabled' } | ForEach-Object { "$($_.FeatureName)" })
        ConvertTo-Json -InputObject $enabled -Compress
        """;

    public const string ServicesScriptBody = """
        $result = foreach ($service in (Get-Service -Name $names -ErrorAction SilentlyContinue)) {
            [PSCustomObject]@{ Name = $service.Name; StartType = "$($service.StartType)" }
        }
        ConvertTo-Json -InputObject @($result) -Compress
        """;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static string ServicesScript()
    {
        var names = string.Join(",", BloatwareCatalog.Services.Keys.Select(PowerShellRunner.Quote));
        return $"$names = @({names})\n{ServicesScriptBody}";
    }

    public async Task<List<AppItem>> ScanAllAsync(IProgress<string>? progress = null)
    {
        var items = new List<AppItem>();

        await RunStepAsync("Store apps", items, progress, ScanStoreAppsAsync);
        await RunStepAsync("programs", items, progress, () => Task.Run(ScanPrograms));
        await RunStepAsync("Windows features", items, progress, ScanFeaturesAsync);
        await RunStepAsync("services", items, progress, ScanServicesAsync);

        return items;
    }

    public static List<AppItem> ScanPrograms()
    {
        var entries = InstalledProgramRegistry.ReadAll()
            .Where(entry => !entry.IsHidden && !string.IsNullOrWhiteSpace(entry.DisplayName))
            .Where(entry => !entry.DisplayName!.Equals("DebloatManager", StringComparison.OrdinalIgnoreCase))
            .Where(entry => !string.IsNullOrWhiteSpace(entry.UninstallCommand) || entry.IsWindowsInstaller);

        return entries
            .AsParallel()
            .WithDegreeOfParallelism(Math.Max(2, Environment.ProcessorCount / 2))
            .Select(BuildProgram)
            .ToList();
    }

    private static AppItem BuildProgram(UninstallEntry entry)
    {
        var signature = ProgramFileLocator.InspectBestFile(entry);
        var publisher = entry.Publisher == "Unknown" && signature.Signer is not null ? signature.Signer : entry.Publisher;
        var match = BloatwareCatalog.ClassifyProgram(entry.DisplayName!, publisher);

        return new AppItem
        {
            Name = entry.DisplayName!,
            Identifier = entry.KeyName,
            Type = AppType.Win32App,
            Publisher = publisher,
            Version = entry.Version,
            Risk = match?.Risk ?? RiskLevel.Unknown,
            Category = match?.Category ?? "Third-party program",
            Notes = match?.Notes ?? string.Empty,
            Origin = signature.Status,
            Signer = signature.Signer,
            SignatureProblem = signature.Problem,
            Website = entry.Website,
            InstallDate = entry.InstallDate,
            InstallLocation = entry.InstallLocation,
            UninstallCommand = entry.UninstallCommand,
            IsWindowsInstaller = entry.IsWindowsInstaller,
            UninstallHive = entry.Hive,
            UninstallKeyPath = entry.KeyPath
        };
    }

    public static List<AppItem> BuildStoreApps(IEnumerable<StoreAppRecord> records)
    {
        var items = new List<AppItem>();

        foreach (var group in records.Where(r => !string.IsNullOrWhiteSpace(r.Name) && r.Present)
                     .GroupBy(r => r.Name!, StringComparer.OrdinalIgnoreCase))
        {
            var record = group.First();
            var publisher = ChoosePublisher(record.PublisherDisplayName, record.Publisher);
            var nonRemovable = group.Any(r => r.NonRemovable);
            var match = BloatwareCatalog.ClassifyStoreApp(group.Key, publisher, nonRemovable, record.SignatureKind);

            items.Add(new AppItem
            {
                Name = match?.DisplayName ?? group.Key,
                Identifier = group.Key,
                Type = AppType.UwpApp,
                Publisher = publisher,
                Version = record.Version ?? "-",
                Risk = match?.Risk ?? RiskLevel.Unknown,
                Category = match?.Category ?? "Third-party app",
                Notes = match?.Notes ?? string.Empty,
                Origin = OriginOfPackage(record.SignatureKind),
                Signer = ExtractPublisher(record.Publisher) is var signer && signer != "Unknown" ? signer : null,
                InstallLocation = record.InstallLocation
            });
        }

        return items;
    }

    public static OriginStatus OriginOfPackage(string? signatureKind) => signatureKind switch
    {
        "System" => OriginStatus.Windows,
        "Store" => OriginStatus.Store,
        "Developer" or "Enterprise" => OriginStatus.Signed,
        "None" => OriginStatus.Unsigned,
        _ => OriginStatus.NotChecked
    };

    public static string ChoosePublisher(string? manifestName, string? distinguishedName)
        => !string.IsNullOrWhiteSpace(manifestName) && !manifestName.StartsWith("ms-resource:", StringComparison.OrdinalIgnoreCase)
            ? manifestName
            : ExtractPublisher(distinguishedName);

    public static string ExtractPublisher(string? distinguishedName)
    {
        var commonName = distinguishedName?.Split(',')
            .Select(part => part.Trim())
            .FirstOrDefault(part => part.StartsWith("CN=", StringComparison.OrdinalIgnoreCase))?[3..];

        return string.IsNullOrWhiteSpace(commonName) || Guid.TryParse(commonName, out _) ? "Unknown" : commonName;
    }

    private static async Task RunStepAsync(string label, List<AppItem> items, IProgress<string>? progress, Func<Task<List<AppItem>>> step)
    {
        progress?.Report($"Scanning {label}...");
        try
        {
            var found = await step();
            items.AddRange(found);
            progress?.Report($"Found {found.Count} {label}.");
        }
        catch (Exception ex)
        {
            progress?.Report($"Could not scan {label}: {ex.Message}");
        }
    }

    private static async Task<List<AppItem>> ScanStoreAppsAsync()
    {
        var output = await RunForOutputAsync(StoreAppsScript);
        return BuildStoreApps(JsonSerializer.Deserialize<List<StoreAppRecord>>(output, JsonOptions) ?? new());
    }

    private static async Task<List<AppItem>> ScanFeaturesAsync()
    {
        var output = await RunForOutputAsync(FeaturesScript);
        var enabled = JsonSerializer.Deserialize<List<string>>(output, JsonOptions) ?? new();

        return enabled
            .Where(BloatwareCatalog.Features.ContainsKey)
            .Select(name => BuildSystemItem(name, BloatwareCatalog.Features[name], AppType.SystemFeature, "Enabled"))
            .ToList();
    }

    private static async Task<List<AppItem>> ScanServicesAsync()
    {
        var output = await RunForOutputAsync(ServicesScript());
        var services = JsonSerializer.Deserialize<List<ServiceRecord>>(output, JsonOptions) ?? new();

        return services
            .Where(service => service.Name is not null && BloatwareCatalog.Services.ContainsKey(service.Name))
            .Select(service =>
            {
                var disabled = string.Equals(service.StartType, "Disabled", StringComparison.OrdinalIgnoreCase);
                var item = BuildSystemItem(service.Name!, BloatwareCatalog.Services[service.Name!], AppType.BackgroundService, disabled ? "Disabled" : "Enabled");
                item.IsRemoved = disabled;
                return item;
            })
            .ToList();
    }

    private static AppItem BuildSystemItem(string identifier, SystemEntry entry, AppType type, string status) => new()
    {
        Name = entry.DisplayName,
        Identifier = identifier,
        Type = type,
        Publisher = "Microsoft Corporation",
        Risk = entry.Risk,
        Category = entry.Category,
        Notes = entry.Notes,
        Origin = OriginStatus.Windows,
        Status = status
    };

    private static async Task<string> RunForOutputAsync(string script)
    {
        var result = await PowerShellRunner.RunAsync(script);
        if (!result.Success) throw new InvalidOperationException(result.FailureText("PowerShell reported an error."));

        var lastLine = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        return string.IsNullOrWhiteSpace(lastLine) ? "[]" : lastLine;
    }

    public sealed class StoreAppRecord
    {
        public string? Name { get; set; }
        public string? Publisher { get; set; }
        public string? PublisherDisplayName { get; set; }
        public string? Version { get; set; }
        public bool NonRemovable { get; set; }
        public string? InstallLocation { get; set; }
        public string? SignatureKind { get; set; }
        public bool Present { get; set; } = true;
    }

    private sealed class ServiceRecord
    {
        public string? Name { get; set; }
        public string? StartType { get; set; }
    }
}
