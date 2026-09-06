using System.Diagnostics;
using System.IO;

namespace DebloatManager.Services;

public sealed class FileLogService
{
    public string LogDirectory { get; }
    private readonly string logFilePath;

    public FileLogService()
    {
        LogDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DebloatManager", "logs");

        Directory.CreateDirectory(LogDirectory);
        logFilePath = Path.Combine(LogDirectory, $"debloatmanager-{DateTime.Now:yyyy-MM-dd}.log");
    }

    public void Append(string message)
    {
        try
        {
            File.AppendAllText(logFilePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
        }
        catch
        {
            // Best-effort logging; disk/IO failures should not crash the app.
        }
    }

    public void OpenLogFolder()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = LogDirectory,
            UseShellExecute = true
        });
    }
}
