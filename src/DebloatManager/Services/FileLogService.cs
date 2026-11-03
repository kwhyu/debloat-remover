using System.Diagnostics;
using System.IO;

namespace DebloatManager.Services;

public sealed class FileLogService
{
    private const int RetentionDays = 30;

    private readonly string logFilePath;

    public string LogDirectory { get; }

    public FileLogService()
    {
        LogDirectory = Path.Combine(AppPaths.DataDirectory, "logs");
        Directory.CreateDirectory(LogDirectory);
        logFilePath = Path.Combine(LogDirectory, $"debloatmanager-{DateTime.Now:yyyy-MM-dd}.log");
        DeleteOldLogs();
    }

    public void Append(string message)
    {
        try
        {
            File.AppendAllText(logFilePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A failed log write must never interrupt a removal.
        }
    }

    public void OpenLogFolder() => Process.Start(new ProcessStartInfo { FileName = LogDirectory, UseShellExecute = true });

    private void DeleteOldLogs()
    {
        foreach (var file in Directory.EnumerateFiles(LogDirectory, "debloatmanager-*.log"))
        {
            try
            {
                if (File.GetLastWriteTime(file) < DateTime.Now.AddDays(-RetentionDays)) File.Delete(file);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Old logs can be removed on a later run.
            }
        }
    }
}
