using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DebloatManager.Models;
using DebloatManager.Services;

namespace DebloatManager.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly AppScannerService scannerService = new();
    private readonly AppActionService actionService = new();
    private readonly RestorePointService restorePointService = new();
    private readonly FileLogService log = new();
    private readonly IDialogService dialogs;

    public ObservableCollection<AppItem> Apps { get; } = new();
    public ICollectionView AppsView { get; }

    public string[] TypeFilters { get; } = { "All types", "Store app", "Program", "Windows feature", "Service" };
    public string[] RiskFilters { get; } = { "All risks", "Safe", "Caution", "Unknown", "Risky", "Protected" };

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private string selectedTypeFilter = "All types";

    [ObservableProperty]
    private string selectedRiskFilter = "All risks";

    [ObservableProperty]
    private AppItem? selectedItem;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ScanCommand), nameof(RemoveSelectedCommand), nameof(CreateRestorePointCommand), nameof(SelectRecommendedCommand), nameof(ClearSelectionCommand))]
    private bool isBusy;

    [ObservableProperty]
    private string statusMessage = "Ready.";

    [ObservableProperty]
    private int visibleCount;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveSelectedCommand))]
    private int selectedCount;

    public MainViewModel(IDialogService dialogs)
    {
        this.dialogs = dialogs;
        AppsView = CollectionViewSource.GetDefaultView(Apps);
        AppsView.Filter = FilterApp;
    }

    partial void OnSearchTextChanged(string value) => RefreshView();
    partial void OnSelectedTypeFilterChanged(string value) => RefreshView();
    partial void OnSelectedRiskFilterChanged(string value) => RefreshView();

    private bool CanAct() => !IsBusy;
    private bool CanRemoveSelected() => !IsBusy && SelectedCount > 0;

    private void RefreshView()
    {
        AppsView.Refresh();
        VisibleCount = AppsView.Cast<object>().Count();
    }

    private void Report(string message)
    {
        StatusMessage = message;
        log.Append(message);
    }

    private bool FilterApp(object obj)
    {
        if (obj is not AppItem item) return false;

        if (!string.IsNullOrWhiteSpace(SearchText) &&
            !item.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) &&
            !item.Identifier.Contains(SearchText, StringComparison.OrdinalIgnoreCase) &&
            !item.Publisher.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (SelectedTypeFilter != TypeFilters[0] && item.TypeLabel != SelectedTypeFilter) return false;
        return SelectedRiskFilter == RiskFilters[0] || item.Risk.ToString() == SelectedRiskFilter;
    }

    [RelayCommand(CanExecute = nameof(CanAct))]
    private async Task ScanAsync()
    {
        IsBusy = true;
        foreach (var item in Apps) item.PropertyChanged -= OnItemChanged;
        Apps.Clear();
        SelectedItem = null;
        SelectedCount = 0;

        try
        {
            var results = await scannerService.ScanAllAsync(new Progress<string>(Report));
            foreach (var item in results.OrderBy(i => i.Risk).ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase))
            {
                item.PropertyChanged += OnItemChanged;
                Apps.Add(item);
            }

            RefreshView();
            Report($"Scan complete. {Apps.Count} items found.");
        }
        catch (Exception ex)
        {
            Report($"Scan failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OnItemChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppItem.IsSelected))
        {
            SelectedCount = Apps.Count(a => a.IsSelected);
        }
    }

    [RelayCommand(CanExecute = nameof(CanAct))]
    private void SelectRecommended()
    {
        foreach (var item in Apps.Where(a => a.Risk == RiskLevel.Safe && a.CanRemove))
        {
            item.IsSelected = true;
        }
    }

    [RelayCommand(CanExecute = nameof(CanAct))]
    private void ClearSelection()
    {
        foreach (var item in Apps.Where(a => a.IsSelected))
        {
            item.IsSelected = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRemoveSelected))]
    private async Task RemoveSelectedAsync()
    {
        var targets = Apps.Where(a => a.IsSelected && a.CanRemove).ToList();
        if (targets.Count == 0) return;

        var leftovers = targets
            .Where(item => item.Type == AppType.Win32App)
            .ToDictionary(item => item, LeftoverPlanner.ForItem);

        var plan = dialogs.ConfirmRemoval(targets, leftovers.Values.SelectMany(folders => folders).ToList());
        if (plan is null) return;

        IsBusy = true;
        try
        {
            if (plan.CreateRestorePoint && !await TryCreateRestorePointAsync()) return;
            await RemoveTargetsAsync(targets, plan.CleanLeftovers ? leftovers : null);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task<bool> TryCreateRestorePointAsync()
    {
        Report("Creating a restore point. This can take a few minutes...");
        var result = await restorePointService.CreateAsync();
        Report(result.Success ? result.Message : $"Restore point failed: {result.Message}");
        if (result.Success) return true;

        return await dialogs.ConfirmAsync(
            "Restore point failed",
            $"{result.Message}\n\nWithout a restore point you may not be able to undo these changes. Continue anyway?",
            "Continue without restore point");
    }

    private async Task RemoveTargetsAsync(List<AppItem> targets, IReadOnlyDictionary<AppItem, IReadOnlyList<LeftoverFolder>>? foldersToClean)
    {
        var failures = new List<string>();
        var remainingFiles = new List<string>();
        var restartRequired = false;

        for (var index = 0; index < targets.Count; index++)
        {
            var item = targets[index];
            StatusMessage = $"{item.ActionLabel} {index + 1} of {targets.Count}: {item.Name}...";
            var cleanup = foldersToClean is not null && foldersToClean.TryGetValue(item, out var folders) ? folders : null;
            var result = await actionService.RemoveAsync(item, cleanup);
            log.Append(result.Message);
            foreach (var issue in result.Issues) log.Append(issue);
            restartRequired |= result.RestartRequired;
            if (!result.Success) failures.Add($"{item.Name}: {result.Message}");
            remainingFiles.AddRange(result.Issues.Select(issue => $"{item.Name}: {issue}"));
        }

        var completed = targets.Count - failures.Count;
        var summary = $"{completed} of {targets.Count} completed and verified.";
        if (remainingFiles.Count > 0) summary += " Some leftover files remain.";
        if (restartRequired) summary += " Restart Windows to finish.";
        Report(summary);

        if (failures.Count > 0 || remainingFiles.Count > 0)
        {
            var title = failures.Count > 0 ? "Some items were not removed" : "Some leftover files remain";
            var details = string.Join("\n\n", failures.Concat(remainingFiles));
            await dialogs.ShowMessageAsync(title, $"{summary}\n\n{details}");
        }
        else if (restartRequired)
        {
            await dialogs.ShowMessageAsync("Restart required", summary);
        }
    }

    [RelayCommand(CanExecute = nameof(CanAct))]
    private async Task CreateRestorePointAsync()
    {
        IsBusy = true;
        Report("Creating a restore point. This can take a few minutes...");
        var result = await restorePointService.CreateAsync();
        Report(result.Message);
        IsBusy = false;

        await dialogs.ShowMessageAsync(result.Success ? "Restore point created" : "Restore point failed", result.Message);
    }

    [RelayCommand]
    private void OpenLogFolder() => log.OpenLogFolder();

    [RelayCommand]
    private void ShowLegal() => dialogs.ShowLegalDocuments();
}
