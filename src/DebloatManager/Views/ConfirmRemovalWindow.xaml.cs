using System.Text;
using System.Windows;
using System.Windows.Data;
using DebloatManager.Models;
using DebloatManager.Services;
using Wpf.Ui.Controls;

namespace DebloatManager.Views;

public partial class ConfirmRemovalWindow : FluentWindow
{
    public bool CreateRestorePoint => RestorePointCheck.IsChecked == true;
    public bool CleanLeftovers => CleanFilesOption.IsChecked == true;

    public ConfirmRemovalWindow(IReadOnlyList<AppItem> items, IReadOnlyList<LeftoverFolder> leftovers)
    {
        InitializeComponent();

        ItemList.ItemsSource = items.OrderByDescending(i => i.Risk).ToList();
        ConfirmButton.Content = $"Continue with {items.Count} item{(items.Count == 1 ? string.Empty : "s")}";
        Summary.Text = "Review what will change. Store apps are removed for every user on this PC. "
            + "Windows features and services are turned off, not deleted.";

        CleanupPanel.Visibility = items.Any(i => i.Type == AppType.Win32App) ? Visibility.Visible : Visibility.Collapsed;
        FolderList.Text = DescribeFolders(leftovers);

        if (items.Any(i => i.Risk is RiskLevel.Risky or RiskLevel.Unknown))
        {
            ConfirmButton.SetBinding(IsEnabledProperty, new Binding(nameof(AcknowledgeCheck.IsChecked)) { Source = AcknowledgeCheck });
        }
        else
        {
            AcknowledgeCheck.Visibility = Visibility.Collapsed;
        }
    }

    private static string DescribeFolders(IReadOnlyList<LeftoverFolder> leftovers)
    {
        var text = new StringBuilder("Only these folders are deleted, and only after the uninstall is verified. Registry settings are left alone.\n");

        var deletable = leftovers.Where(f => f.CanDelete).ToList();
        if (deletable.Count == 0) text.Append("\nNo folders found to delete.\n");
        foreach (var folder in deletable) text.Append($"\nDelete: {folder.Path}");

        foreach (var folder in leftovers.Where(f => !f.CanDelete))
        {
            text.Append($"\nKeep: {folder.Path}. {folder.Reason}");
        }

        return text.ToString();
    }

    private void OnConfirm(object sender, RoutedEventArgs e) => DialogResult = true;

    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;
}
