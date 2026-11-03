using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;

namespace DebloatManager.Models;

public partial class AppItem : ObservableObject
{
    public required string Name { get; init; }
    public required string Identifier { get; init; }
    public required AppType Type { get; init; }
    public string Publisher { get; init; } = "Unknown";
    public string Version { get; init; } = "-";
    public RiskLevel Risk { get; init; } = RiskLevel.Unknown;
    public string Category { get; init; } = "Uncategorized";
    public string Notes { get; init; } = string.Empty;

    public OriginStatus Origin { get; init; } = OriginStatus.NotChecked;
    public string? Signer { get; init; }
    public string? SignatureProblem { get; init; }
    public string? Website { get; init; }
    public string? InstallDate { get; init; }

    public string? InstallLocation { get; init; }
    public string? UninstallCommand { get; init; }
    public bool IsWindowsInstaller { get; init; }
    public RegistryHive? UninstallHive { get; init; }
    public string? UninstallKeyPath { get; init; }

    [ObservableProperty]
    private string status = "Installed";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanRemove))]
    private bool isRemoved;

    [ObservableProperty]
    private bool isSelected;

    public bool CanRemove => !IsRemoved && Risk != RiskLevel.Protected;

    public string TypeLabel => Type switch
    {
        AppType.UwpApp => "Store app",
        AppType.Win32App => "Program",
        AppType.SystemFeature => "Windows feature",
        AppType.BackgroundService => "Service",
        _ => "Unknown"
    };

    public string ActionLabel => Type is AppType.UwpApp or AppType.Win32App ? "Remove" : "Disable";

    public string OriginLabel => Origin switch
    {
        OriginStatus.Windows => "Windows",
        OriginStatus.Store => "Store",
        OriginStatus.Signed => "Verified",
        OriginStatus.Unsigned => "Unsigned",
        OriginStatus.Invalid => "Invalid",
        _ => "Not checked"
    };

    public string OriginSummary => Origin switch
    {
        OriginStatus.Windows => "Origin: part of Windows.",
        OriginStatus.Store => $"Origin: Microsoft Store. Listed publisher: {Publisher}.",
        OriginStatus.Signed => $"Origin: signed by {Signer ?? "an unnamed publisher"}, and the signature is valid. This confirms who made it, not that it is safe.",
        OriginStatus.Unsigned => $"Origin: not signed. The publisher name \"{Publisher}\" cannot be confirmed. This does not mean it is harmful, so check it if you do not recognize it.",
        OriginStatus.Invalid => $"Origin: invalid signature{(Signer is null ? string.Empty : $" from {Signer}")}. {SignatureProblem}",
        _ => $"Origin: not checked, because no program file was available. The publisher name \"{Publisher}\" cannot be confirmed."
    };

    public string SourceDetails
    {
        get
        {
            var parts = new List<string>();
            if (FormatInstallDate() is { } date) parts.Add($"Installed {date}");
            if (!string.IsNullOrWhiteSpace(InstallLocation)) parts.Add(InstallLocation);
            if (!string.IsNullOrWhiteSpace(Website)) parts.Add($"Website listed by the program: {Website}");
            return string.Join("  |  ", parts);
        }
    }

    public string RiskSummary => Risk switch
    {
        RiskLevel.Safe => "Preinstalled and rarely needed. Removal is not expected to affect Windows.",
        RiskLevel.Caution => "Useful to some people. Other apps or Windows features may rely on it.",
        RiskLevel.Risky => "Windows features or other apps can stop working after this change.",
        RiskLevel.Protected => "Required by Windows. DebloatManager will not remove it.",
        _ => "Not in the catalog. Find out what it does before removing it."
    };

    public string RecoveryHint => Type switch
    {
        AppType.UwpApp => "Undo: reinstall from the Microsoft Store. System Restore does not reliably bring Store apps back.",
        AppType.Win32App => "Undo: reinstall from the publisher. Its settings and leftover files may be gone.",
        AppType.SystemFeature => "Undo: turn it back on in \"Turn Windows features on or off\".",
        AppType.BackgroundService => "Undo: set its startup type back in services.msc. The previous value is in the log.",
        _ => string.Empty
    };

    private string? FormatInstallDate()
        => DateTime.TryParseExact(InstallDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date.ToString("d MMM yyyy", CultureInfo.InvariantCulture)
            : null;
}
