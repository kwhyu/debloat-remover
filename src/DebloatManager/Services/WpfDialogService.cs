using Wpf.Ui.Controls;

namespace DebloatManager.Services;

public sealed class WpfDialogService : IDialogService
{
    public async Task<bool> ConfirmAsync(string title, string message, bool isDangerous)
    {
        var box = new MessageBox
        {
            Title = title,
            Content = message,
            PrimaryButtonText = isDangerous ? "Continue Anyway" : "Confirm",
            CloseButtonText = "Cancel"
        };

        var result = await box.ShowDialogAsync();
        return result == MessageBoxResult.Primary;
    }

    public async Task ShowMessageAsync(string title, string message)
    {
        var box = new MessageBox
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK"
        };

        await box.ShowDialogAsync();
    }
}
