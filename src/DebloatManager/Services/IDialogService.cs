using DebloatManager.Models;

namespace DebloatManager.Services;

public sealed record RemovalPlan(bool CreateRestorePoint, bool CleanLeftovers);

public interface IDialogService
{
    RemovalPlan? ConfirmRemoval(IReadOnlyList<AppItem> items, IReadOnlyList<LeftoverFolder> leftovers);
    Task<bool> ConfirmAsync(string title, string message, string confirmLabel);
    Task ShowMessageAsync(string title, string message);
    void ShowLegalDocuments();
}
