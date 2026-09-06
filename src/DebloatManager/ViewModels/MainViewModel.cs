using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DebloatManager.Models;
using DebloatManager.Services;

namespace DebloatManager.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private const int MaxLogEntries = 300;

    private readonly AppScannerService scannerService = new();
    private readonly AppActionService actionService = new();
    private readonly RestorePointService restorePointService = new();
    private readonly FileLogService fileLogService = new();
    private readonly ProcessMonitorService processMonitorService = new();
    private readonly IDialogService dialogService;

    public ObservableCollection<AppItem> Apps { get; } = new();
    public ObservableCollection<string> ActivityLog { get; } = new();
    public ObservableCollection<ProcessItem> Processes { get; } = new();
    public ICollectionView AppsView { get; }
    public ICollectionView ProcessesView { get; }

    public string[] TypeFilters { get; } = { "All", "UWP App", "Program", "System Feature", "Service" };
    public string[] RiskFilters { get; } = { "All", "Safe", "Caution", "Risky", "Unknown" };

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private string selectedTypeFilter = "All";

    [ObservableProperty]
    private string selectedRiskFilter = "All";

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string statusMessage = "Click Scan to detect installed apps and system components.";

    [ObservableProperty]
    private int totalCount;

    [ObservableProperty]
    private int runningCount;

    [ObservableProperty]
    private int selectedCount;

    [ObservableProperty]
    private bool isGroupedByCategory;

    [ObservableProperty]
    private bool isLogVisible;

    [ObservableProperty]
    private string processSearchText = string.Empty;

    [ObservableProperty]
    private int processCount;

    public MainViewModel(IDialogService dialogService)
    {
        this.dialogService = dialogService;
        AppsView = CollectionViewSource.GetDefaultView(Apps);
        AppsView.Filter = FilterApp;
        Apps.CollectionChanged += OnAppsCollectionChanged;

        ProcessesView = CollectionViewSource.GetDefaultView(Processes);
        ProcessesView.Filter = FilterProcess;

        AddLog($"DebloatManager started. Logs are saved to {fileLogService.LogDirectory}");
        _ = RefreshProcessesAsync();
        _ = ScanAsync();
    }

    partial void OnSearchTextChanged(string value) => AppsView.Refresh();
    partial void OnSelectedTypeFilterChanged(string value) => AppsView.Refresh();
    partial void OnSelectedRiskFilterChanged(string value) => AppsView.Refresh();
    partial void OnProcessSearchTextChanged(string value) => ProcessesView.Refresh();

    private bool FilterProcess(object obj)
    {
        if (obj is not ProcessItem item) return false;
        if (string.IsNullOrWhiteSpace(ProcessSearchText)) return true;

        return item.Name.Contains(ProcessSearchText, StringComparison.OrdinalIgnoreCase)
            || item.Pids.Any(pid => pid.ToString().Contains(ProcessSearchText, StringComparison.OrdinalIgnoreCase));
    }

    partial void OnIsGroupedByCategoryChanged(bool value)
    {
        AppsView.GroupDescriptions.Clear();
        if (value)
        {
            AppsView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(AppItem.Category)));
        }
    }

    private void OnAppsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (AppItem item in e.OldItems)
            {
                item.PropertyChanged -= OnAppItemPropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (AppItem item in e.NewItems)
            {
                item.PropertyChanged += OnAppItemPropertyChanged;
            }
        }

        RecalculateCounts();
    }

    private void OnAppItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(AppItem.IsSelected) or nameof(AppItem.IsRunning) or nameof(AppItem.IsRemoved))
        {
            RecalculateCounts();
        }
    }

    private void RecalculateCounts()
    {
        TotalCount = Apps.Count;
        RunningCount = Apps.Count(a => a.IsRunning);
        SelectedCount = Apps.Count(a => a.IsSelected);
    }

    private void AddLog(string message)
    {
        ActivityLog.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");
        while (ActivityLog.Count > MaxLogEntries)
        {
            ActivityLog.RemoveAt(ActivityLog.Count - 1);
        }

        fileLogService.Append(message);
    }

    [RelayCommand]
    private void OpenLogFolder() => fileLogService.OpenLogFolder();

    private bool FilterApp(object obj)
    {
        if (obj is not AppItem item) return false;

        if (!string.IsNullOrWhiteSpace(SearchText) &&
            !item.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) &&
            !item.Publisher.Contains(SearchText, StringComparison.OrdinalIgnoreCase) &&
            !item.Category.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (SelectedTypeFilter != "All")
        {
            var expectedType = SelectedTypeFilter switch
            {
                "UWP App" => AppType.UwpApp,
                "Program" => AppType.Win32App,
                "System Feature" => AppType.SystemFeature,
                "Service" => AppType.BackgroundService,
                _ => (AppType?)null
            };
            if (expectedType is not null && item.Type != expectedType) return false;
        }

        if (SelectedRiskFilter != "All" && !string.Equals(item.Risk.ToString(), SelectedRiskFilter, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    [RelayCommand]
    private async Task ScanAsync()
    {
        IsBusy = true;
        Apps.Clear();

        var progress = new Progress<string>(message =>
        {
            StatusMessage = message;
            AddLog(message);
        });

        try
        {
            var results = await scannerService.ScanAllAsync(progress);
            foreach (var item in results.OrderByDescending(i => i.Risk == RiskLevel.Safe).ThenBy(i => i.Name))
            {
                Apps.Add(item);
            }

            StatusMessage = $"Scan complete. {TotalCount} items found, {RunningCount} currently running.";
            AddLog($"Scan complete: {TotalCount} items found.");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Scan failed: {ex.Message}";
            AddLog($"Scan failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task EndTaskAsync(AppItem? item)
    {
        if (item is null) return;

        var confirmed = await dialogService.ConfirmAsync(
            "End Task",
            $"End all running processes for '{item.Name}'? Unsaved data in this app may be lost.",
            isDangerous: false);

        if (!confirmed) return;

        IsBusy = true;
        var result = await EndTaskCoreAsync(item);
        IsBusy = false;

        StatusMessage = result.Message;
        if (!result.Success)
        {
            await dialogService.ShowMessageAsync("End Task Failed", result.Message);
        }
    }

    private async Task<OperationResult> EndTaskCoreAsync(AppItem item)
    {
        var result = await actionService.EndTaskAsync(item);
        AddLog(result.Message);
        return result;
    }

    [RelayCommand]
    private async Task UninstallAsync(AppItem? item)
    {
        if (item is null) return;

        var warning = BuildRemovalWarning(item);
        var confirmed = await dialogService.ConfirmAsync("Confirm Removal", warning, isDangerous: item.Risk is RiskLevel.Risky or RiskLevel.Unknown);
        if (!confirmed) return;

        if (!await OfferRestorePointIfHighRiskAsync(item)) return;

        IsBusy = true;
        StatusMessage = $"Removing '{item.Name}'...";
        var result = await UninstallCoreAsync(item);
        IsBusy = false;

        StatusMessage = result.Message;
        if (result.Success)
        {
            AppsView.Refresh();
        }
        else
        {
            await dialogService.ShowMessageAsync("Removal Failed", result.Message);
        }
    }

    private async Task<OperationResult> UninstallCoreAsync(AppItem item)
    {
        var result = await actionService.UninstallAsync(item);
        AddLog(result.Message);
        return result;
    }

    private static string BuildRemovalWarning(AppItem item) => item.Risk switch
    {
        RiskLevel.Risky => $"'{item.Name}' is a core system component. Removing it can make Windows behave abnormally or break other apps.\n\nAre you sure you want to continue?",
        RiskLevel.Unknown => $"'{item.Name}' is not in the known bloatware list. Review it carefully before removing — it may be required by Windows or another app.\n\nContinue anyway?",
        RiskLevel.Caution => $"'{item.Name}' may still be useful to some users.\n\nContinue removing it?",
        _ => $"Remove '{item.Name}'?"
    };

    private async Task<bool> OfferRestorePointIfHighRiskAsync(AppItem item)
    {
        if (item.Risk != RiskLevel.Risky && item.Type != AppType.SystemFeature)
        {
            return true;
        }

        var createRestorePoint = await dialogService.ConfirmAsync(
            "Create Restore Point",
            "This is a high-risk action. Create a System Restore Point first so you can undo this change if something goes wrong?",
            isDangerous: false);

        if (!createRestorePoint) return true;

        IsBusy = true;
        StatusMessage = "Creating system restore point...";
        var restoreResult = await restorePointService.CreateRestorePointAsync($"DebloatManager before removing {item.Name}");
        StatusMessage = restoreResult.Message;
        AddLog(restoreResult.Message);
        IsBusy = false;

        if (restoreResult.Success) return true;

        return await dialogService.ConfirmAsync(
            "Restore Point Failed",
            $"{restoreResult.Message}\n\nContinue removing '{item.Name}' without a restore point?",
            isDangerous: true);
    }

    [RelayCommand]
    private async Task CreateRestorePointAsync()
    {
        IsBusy = true;
        StatusMessage = "Creating system restore point...";
        var result = await restorePointService.CreateRestorePointAsync("DebloatManager manual checkpoint");
        StatusMessage = result.Message;
        AddLog(result.Message);
        IsBusy = false;

        await dialogService.ShowMessageAsync(result.Success ? "Restore Point Created" : "Restore Point Failed", result.Message);
    }

    [RelayCommand]
    private void SelectAllSafe()
    {
        foreach (var item in Apps.Where(a => a.Risk == RiskLevel.Safe && a.CanRemove))
        {
            item.IsSelected = true;
        }
    }

    [RelayCommand]
    private void ClearSelection()
    {
        foreach (var item in Apps.Where(a => a.IsSelected))
        {
            item.IsSelected = false;
        }
    }

    [RelayCommand]
    private async Task EndTaskSelectedAsync()
    {
        var targets = Apps.Where(a => a.IsSelected && a.CanEndTask).ToList();
        if (targets.Count == 0) return;

        var confirmed = await dialogService.ConfirmAsync(
            "End Selected Tasks",
            $"End all running processes for {targets.Count} selected item(s)?",
            isDangerous: false);
        if (!confirmed) return;

        IsBusy = true;
        foreach (var item in targets)
        {
            StatusMessage = $"Ending '{item.Name}'...";
            await EndTaskCoreAsync(item);
        }
        IsBusy = false;
        StatusMessage = $"Ended {targets.Count} selected item(s).";
    }

    [RelayCommand]
    private async Task RemoveSelectedAsync()
    {
        var targets = Apps.Where(a => a.IsSelected && a.CanRemove).ToList();
        if (targets.Count == 0) return;

        var hasRiskyOrUnknown = targets.Any(t => t.Risk is RiskLevel.Risky or RiskLevel.Unknown);
        var hasRisky = targets.Any(t => t.Risk == RiskLevel.Risky);
        var warning = $"Remove {targets.Count} selected item(s)?" +
                      (hasRiskyOrUnknown
                          ? "\n\nThe selection includes items marked Risky or Unknown, which can make Windows behave abnormally. Review your selection carefully."
                          : string.Empty);

        var confirmed = await dialogService.ConfirmAsync("Confirm Bulk Removal", warning, isDangerous: hasRiskyOrUnknown);
        if (!confirmed) return;

        if (hasRisky || targets.Any(t => t.Type == AppType.SystemFeature))
        {
            var createRestorePoint = await dialogService.ConfirmAsync(
                "Create Restore Point",
                "Your selection includes high-risk items. Create a System Restore Point first?",
                isDangerous: false);

            if (createRestorePoint)
            {
                IsBusy = true;
                StatusMessage = "Creating system restore point...";
                var restoreResult = await restorePointService.CreateRestorePointAsync("DebloatManager before bulk removal");
                StatusMessage = restoreResult.Message;
                AddLog(restoreResult.Message);
                IsBusy = false;

                if (!restoreResult.Success)
                {
                    var proceedAnyway = await dialogService.ConfirmAsync(
                        "Restore Point Failed",
                        $"{restoreResult.Message}\n\nContinue with bulk removal without a restore point?",
                        isDangerous: true);
                    if (!proceedAnyway) return;
                }
            }
        }

        IsBusy = true;
        var failures = 0;
        foreach (var item in targets)
        {
            StatusMessage = $"Removing '{item.Name}'...";
            var result = await UninstallCoreAsync(item);
            if (!result.Success) failures++;
        }
        IsBusy = false;

        StatusMessage = failures == 0
            ? $"Removed {targets.Count} selected item(s)."
            : $"Removed {targets.Count - failures} of {targets.Count} item(s). {failures} failed — check the activity log.";

        AppsView.Refresh();
    }

    [RelayCommand]
    private async Task RefreshProcessesAsync()
    {
        IsBusy = true;
        StatusMessage = "Refreshing running processes...";

        try
        {
            var results = await Task.Run(() => processMonitorService.ScanProcesses());
            Processes.Clear();
            foreach (var process in results)
            {
                Processes.Add(process);
            }

            ProcessCount = Processes.Count;
            StatusMessage = $"{ProcessCount} running processes.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to list running processes: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task EndProcessAsync(ProcessItem? item)
    {
        if (item is null) return;

        var label = item.InstanceCount > 1 ? $"all {item.InstanceCount} instances of '{item.Name}'" : $"'{item.Name}'";
        var confirmed = await dialogService.ConfirmAsync(
            "End Process",
            $"End {label}? Unsaved data may be lost.\n\nEnding a critical system process can crash Windows or force a restart.",
            isDangerous: true);
        if (!confirmed) return;

        IsBusy = true;
        var failures = new List<string>();
        foreach (var pid in item.Pids)
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

        Processes.Remove(item);
        ProcessCount = Processes.Count;

        if (failures.Count == 0)
        {
            AddLog($"Ended {label}.");
            StatusMessage = $"Ended {label}.";
        }
        else
        {
            var message = $"Some processes in {label} could not be ended: {string.Join("; ", failures)}";
            AddLog(message);
            StatusMessage = message;
            await dialogService.ShowMessageAsync("End Process Failed", message);
        }

        IsBusy = false;
    }
}
