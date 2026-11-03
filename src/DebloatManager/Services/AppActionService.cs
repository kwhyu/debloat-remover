using DebloatManager.Models;

namespace DebloatManager.Services;

public sealed class AppActionService
{
    private readonly ProgramUninstaller programUninstaller = new();

    public async Task<OperationResult> RemoveAsync(AppItem item, IReadOnlyList<LeftoverFolder>? foldersToClean = null)
    {
        if (!item.CanRemove)
        {
            return OperationResult.Fail($"'{item.Name}' cannot be changed by DebloatManager.");
        }

        OperationResult result;
        try
        {
            result = item.Type switch
            {
                AppType.UwpApp => await RemoveStoreAppAsync(item),
                AppType.Win32App => await programUninstaller.RunAsync(item, foldersToClean),
                AppType.SystemFeature => await DisableFeatureAsync(item),
                AppType.BackgroundService => await DisableServiceAsync(item),
                _ => OperationResult.Fail("Unknown item type.")
            };
        }
        catch (Exception ex)
        {
            result = OperationResult.Fail(ex.Message);
        }

        item.IsSelected = false;
        item.IsRemoved = result.Success;
        item.Status = !result.Success ? "Failed"
            : result.Issues.Count > 0 ? "Files remain"
            : item.ActionLabel == "Remove" ? "Removed"
            : "Disabled";

        return result;
    }

    private static async Task<OperationResult> RemoveStoreAppAsync(AppItem item)
    {
        var run = await PowerShellRunner.RunAsync(RemovalScripts.StoreApp(item.Identifier));
        var outcome = PowerShellRunner.ParseJson<StoreAppOutcome>(run.Output);
        if (outcome is null)
        {
            return OperationResult.Fail(FirstLine(run.FailureText("The removal script returned no result.")));
        }

        if (outcome.Removed)
        {
            return OperationResult.Ok($"'{item.Name}' was removed for all users and verified.");
        }

        return OperationResult.Fail(DescribeStoreFailure(outcome));
    }

    private static string DescribeStoreFailure(StoreAppOutcome outcome)
    {
        var error = outcome.Errors?.FirstOrDefault() ?? string.Empty;
        var reason = error switch
        {
            _ when error.Contains("0x80073CFA", StringComparison.OrdinalIgnoreCase) => "Windows blocks removing this package.",
            _ when error.Contains("0x80073D02", StringComparison.OrdinalIgnoreCase) => "The app is in use. Close it and try again.",
            _ when error.Contains("0x80070005", StringComparison.OrdinalIgnoreCase) => "Access was denied.",
            _ => FirstLine(error)
        };

        return $"Still present after removal: {outcome.Remaining} package(s), {outcome.Provisioned} provisioned copy(ies). {reason}".TrimEnd();
    }

    private static async Task<OperationResult> DisableFeatureAsync(AppItem item)
    {
        var run = await PowerShellRunner.RunAsync(RemovalScripts.Feature(item.Identifier));
        var outcome = PowerShellRunner.ParseJson<FeatureOutcome>(run.Output);
        if (outcome is null)
        {
            return OperationResult.Fail(FirstLine(run.FailureText("Windows did not report a result.")));
        }

        if (outcome.State is null || !outcome.State.StartsWith("Disable", StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult.Fail($"The feature is still {outcome.State ?? "enabled"}.");
        }

        var pending = outcome.RestartNeeded || outcome.State.EndsWith("Pending", StringComparison.OrdinalIgnoreCase);
        return OperationResult.Ok(
            pending ? $"'{item.Name}' will be off after a restart." : $"'{item.Name}' was turned off and verified.",
            pending);
    }

    private static async Task<OperationResult> DisableServiceAsync(AppItem item)
    {
        var run = await PowerShellRunner.RunAsync(RemovalScripts.Service(item.Identifier));
        var outcome = PowerShellRunner.ParseJson<ServiceOutcome>(run.Output);
        if (outcome is null)
        {
            return OperationResult.Fail(FirstLine(run.FailureText("Windows did not report a result.")));
        }

        if (!string.Equals(outcome.StartType, "Disabled", StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult.Fail($"Windows did not allow the change. Startup type is still {outcome.StartType}.");
        }

        var stillRunning = string.Equals(outcome.Status, "Running", StringComparison.OrdinalIgnoreCase);
        var message = $"'{item.Name}' ({item.Identifier}) was disabled and verified. Previous startup type: {outcome.Previous}.";
        return OperationResult.Ok(stillRunning ? message + " It stops after a restart." : message, stillRunning);
    }

    private static string FirstLine(string text)
        => text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? string.Empty;

    private sealed class StoreAppOutcome
    {
        public bool Removed { get; set; }
        public int Remaining { get; set; }
        public int Provisioned { get; set; }
        public List<string>? Errors { get; set; }
    }

    private sealed class FeatureOutcome
    {
        public string? State { get; set; }
        public bool RestartNeeded { get; set; }
    }

    private sealed class ServiceOutcome
    {
        public string? Previous { get; set; }
        public string? StartType { get; set; }
        public string? Status { get; set; }
    }
}
