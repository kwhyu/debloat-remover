using DebloatManager.Models;

namespace DebloatManager.Services;

public sealed class RestorePointService
{
    public const string Script = """
        $ErrorActionPreference = 'Stop'
        $key = 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore'
        $value = 'SystemRestorePointCreationFrequency'
        $previous = (Get-ItemProperty -Path $key -Name $value -ErrorAction SilentlyContinue).$value

        function Get-LatestSequence {
            $latest = Get-ComputerRestorePoint -ErrorAction SilentlyContinue | Sort-Object SequenceNumber | Select-Object -Last 1
            if ($latest) { [int]$latest.SequenceNumber } else { 0 }
        }

        try {
            Enable-ComputerRestore -Drive "$env:SystemDrive\"
            $before = Get-LatestSequence

            # Windows silently skips new restore points within 24 hours unless this limit is lifted.
            Set-ItemProperty -Path $key -Name $value -Value 0 -Type DWord
            Checkpoint-Computer -Description 'DebloatManager checkpoint' -RestorePointType 'MODIFY_SETTINGS'

            if ((Get-LatestSequence) -gt $before) { 'OK' } else { 'ERROR: Windows did not create the restore point.' }
        }
        catch { "ERROR: $($_.Exception.Message)" }
        finally {
            if ($null -eq $previous) { Remove-ItemProperty -Path $key -Name $value -ErrorAction SilentlyContinue }
            else { Set-ItemProperty -Path $key -Name $value -Value $previous -Type DWord -ErrorAction SilentlyContinue }
        }
        """;

    public async Task<OperationResult> CreateAsync()
    {
        var result = await PowerShellRunner.RunAsync(Script);
        var line = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).LastOrDefault() ?? string.Empty;

        if (line == "OK") return OperationResult.Ok("Restore point created and verified.");

        return OperationResult.Fail(line.StartsWith("ERROR:", StringComparison.Ordinal)
            ? line[6..].Trim()
            : result.FailureText("Windows did not create the restore point. System Protection may be turned off."));
    }
}
