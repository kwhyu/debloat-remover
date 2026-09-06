using CommunityToolkit.Mvvm.ComponentModel;

namespace DebloatManager.Models;

public partial class AppItem : ObservableObject
{
    public required string Name { get; init; }
    public string Publisher { get; init; } = "Unknown";
    public string Version { get; init; } = "-";
    public required AppType Type { get; init; }
    public RiskLevel Risk { get; init; } = RiskLevel.Unknown;
    public string Category { get; init; } = "Uncategorized";
    public string Notes { get; init; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEndTask))]
    private bool isRunning;

    [ObservableProperty]
    private string status = "Installed";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanRemove))]
    private bool isRemoved;

    [ObservableProperty]
    private bool isSelected;

    // UWP identifiers
    public string? PackageFullName { get; init; }
    public string? PackageFamilyName { get; init; }
    public bool IsProvisioned { get; set; }
    public bool NonRemovable { get; init; }

    // Win32 identifiers
    public string? UninstallCommand { get; init; }
    public bool UninstallIsQuiet { get; init; }
    public string? InstallLocation { get; init; }

    // System feature identifiers
    public string? FeatureName { get; init; }

    // Service identifiers
    public string? ServiceName { get; init; }

    // Runtime process matches, used for End Task
    public List<int> ProcessIds { get; } = new();
    public string? ProcessName { get; init; }

    public bool CanEndTask => Type == AppType.BackgroundService ? IsRunning : ProcessIds.Count > 0;
    public bool CanRemove => !IsRemoved && !NonRemovable;
}
