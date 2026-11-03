using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DebloatManager.Services;

public sealed record PowerShellResult(bool Success, string Output, string Error)
{
    public string FailureText(string fallback)
    {
        var text = string.IsNullOrWhiteSpace(Error) ? Output : Error;
        return string.IsNullOrWhiteSpace(text) ? fallback : text;
    }
}

public static partial class PowerShellRunner
{
    private const string Preamble =
        "$ProgressPreference='SilentlyContinue';$WarningPreference='SilentlyContinue';" +
        "[Console]::OutputEncoding=New-Object System.Text.UTF8Encoding($false);";

    private static readonly string PowerShellPath =
        Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\powershell.exe");

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    // PowerShell also treats typographic single quotes as string delimiters.
    public static string Quote(string value) => $"'{SingleQuotePattern().Replace(value, "$0$0")}'";

    public static async Task<PowerShellResult> RunAsync(string script)
    {
        var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(Preamble + script));
        var startInfo = new ProcessStartInfo
        {
            FileName = PowerShellPath,
            Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -OutputFormat Text -EncodedCommand {encoded}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        // An inherited PowerShell 7 module path breaks the Appx and DISM modules in Windows PowerShell.
        startInfo.Environment.Remove("PSModulePath");

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return new PowerShellResult(process.ExitCode == 0, (await outputTask).Trim(), (await errorTask).Trim());
    }

    public static T? ParseJson<T>(string output) where T : class
    {
        var lastLine = output.Split('\n', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        return string.IsNullOrWhiteSpace(lastLine) ? null : JsonSerializer.Deserialize<T>(lastLine, JsonOptions);
    }

    [GeneratedRegex("['‘’‚‛]")]
    private static partial Regex SingleQuotePattern();
}
