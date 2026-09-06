using System.Diagnostics;
using DebloatManager.Models;

namespace DebloatManager.Services;

public sealed class ProcessMonitorService
{
    private sealed record RawProcess(int Pid, string Name, string? FilePath, double MemoryMb);

    public List<ProcessItem> ScanProcesses()
    {
        var raw = new List<RawProcess>();
        var currentPid = Environment.ProcessId;

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                if (process.Id == currentPid || process.Id == 0)
                {
                    continue;
                }

                string? path = null;
                try { path = process.MainModule?.FileName; } catch { /* protected or inaccessible process */ }

                raw.Add(new RawProcess(
                    process.Id,
                    process.ProcessName,
                    path,
                    Math.Round(process.WorkingSet64 / 1024d / 1024d, 1)));
            }
            catch
            {
                // Process exited or became inaccessible mid-scan, skip it.
            }
            finally
            {
                process.Dispose();
            }
        }

        var grouped = raw
            .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => new ProcessItem
            {
                Name = g.Key,
                Pids = g.Select(p => p.Pid).ToList(),
                FilePath = g.Select(p => p.FilePath).FirstOrDefault(p => p is not null),
                TotalMemoryMb = Math.Round(g.Sum(p => p.MemoryMb), 1)
            })
            .OrderByDescending(p => p.TotalMemoryMb)
            .ToList();

        return grouped;
    }
}
