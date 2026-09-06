namespace DebloatManager.Services;

public interface IDialogService
{
    Task<bool> ConfirmAsync(string title, string message, bool isDangerous);
    Task ShowMessageAsync(string title, string message);
}
