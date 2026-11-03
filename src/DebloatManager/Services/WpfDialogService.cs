using System.Windows;
using DebloatManager.Models;
using DebloatManager.Views;
using MessageBox = Wpf.Ui.Controls.MessageBox;
using MessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;

namespace DebloatManager.Services;

public sealed class WpfDialogService : IDialogService
{
    public RemovalPlan? ConfirmRemoval(IReadOnlyList<AppItem> items, IReadOnlyList<LeftoverFolder> leftovers)
    {
        var window = new ConfirmRemovalWindow(items, leftovers) { Owner = Application.Current.MainWindow };
        return window.ShowDialog() == true ? new RemovalPlan(window.CreateRestorePoint, window.CleanLeftovers) : null;
    }

    public async Task<bool> ConfirmAsync(string title, string message, string confirmLabel)
    {
        var box = new MessageBox
        {
            Title = title,
            Content = message,
            PrimaryButtonText = confirmLabel,
            CloseButtonText = "Cancel"
        };

        return await box.ShowDialogAsync() == MessageBoxResult.Primary;
    }

    public async Task ShowMessageAsync(string title, string message)
    {
        var box = new MessageBox { Title = title, Content = message, CloseButtonText = "OK" };
        await box.ShowDialogAsync();
    }

    public void ShowLegalDocuments()
    {
        new LegalWindow { Owner = Application.Current.MainWindow }.ShowDialog();
    }
}
