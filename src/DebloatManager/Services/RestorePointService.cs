using DebloatManager.Models;

namespace DebloatManager.Services;

public sealed class RestorePointService
{
    public async Task<OperationResult> CreateRestorePointAsync(string description)
    {
        var script = $$"""
        try {
            Enable-ComputerRestore -Drive "$env:SystemDrive\" -ErrorAction SilentlyContinue
            Checkpoint-Computer -Description "{{description}}" -RestorePointType "MODIFY_SETTINGS" -ErrorAction Stop
            Write-Output "OK"
        } catch {
            Write-Output "ERROR: $($_.Exception.Message)"
        }
        """;

        var (success, output, error) = await PowerShellRunner.RunScriptAsync(script);

        if (success && output.StartsWith("OK"))
        {
            return OperationResult.Ok("System restore point created.");
        }

        var reason = output.StartsWith("ERROR:") ? output[6..].Trim() : (string.IsNullOrWhiteSpace(error) ? output : error);
        return OperationResult.Fail(string.IsNullOrWhiteSpace(reason)
            ? "Failed to create a restore point. Windows may only allow one restore point every 24 hours."
            : reason);
    }
}
