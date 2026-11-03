using System.IO;
using System.Text.RegularExpressions;

namespace DebloatManager.Services;

public sealed record UninstallInvocation(string FileName, string Arguments, bool IsMsi, string? TargetFile);

public static partial class UninstallCommandParser
{
    public static UninstallInvocation? Parse(string? command, string? registryKeyName, bool isWindowsInstaller, Func<string, bool>? fileExists = null)
    {
        fileExists ??= File.Exists;
        var expanded = Environment.ExpandEnvironmentVariables(command ?? string.Empty).Trim();

        if (expanded.Length == 0)
        {
            var code = isWindowsInstaller ? FindProductCode(registryKeyName) : null;
            return code is null ? null : Msi(code);
        }

        var (file, arguments) = SplitCommand(expanded, fileExists);
        if (Path.GetFileNameWithoutExtension(file).Equals("msiexec", StringComparison.OrdinalIgnoreCase))
        {
            var code = FindProductCode(arguments) ?? FindProductCode(registryKeyName);
            return code is null ? null : Msi(code);
        }

        if (file.EndsWith(".bat", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase))
        {
            var shell = Path.Combine(Environment.SystemDirectory, "cmd.exe");
            return new UninstallInvocation(shell, $"/d /c \"\"{file}\" {arguments}\"", false, file);
        }

        return new UninstallInvocation(file, arguments, false, file);
    }

    // Registry entries often use msiexec /I, which repairs the product, so always force /x.
    private static UninstallInvocation Msi(string productCode) =>
        new(Path.Combine(Environment.SystemDirectory, "msiexec.exe"), $"/x {productCode} /passive /norestart", true, null);

    private static (string File, string Arguments) SplitCommand(string command, Func<string, bool> fileExists)
    {
        if (command[0] == '"')
        {
            var closing = command.IndexOf('"', 1);
            return closing < 0
                ? (command.Trim('"'), string.Empty)
                : (command[1..closing], command[(closing + 1)..].Trim());
        }

        // Unquoted paths may contain spaces, so try each prefix the way CreateProcess does.
        for (var end = command.IndexOf(' '); end > 0; end = command.IndexOf(' ', end + 1))
        {
            var candidate = command[..end];
            if (IsExistingFile(candidate, fileExists)) return (candidate, command[(end + 1)..].Trim());
            if (IsExistingFile(candidate + ".exe", fileExists)) return (candidate + ".exe", command[(end + 1)..].Trim());
        }

        if (IsExistingFile(command, fileExists)) return (command, string.Empty);

        var firstSpace = command.IndexOf(' ');
        return firstSpace < 0 ? (command, string.Empty) : (command[..firstSpace], command[(firstSpace + 1)..].Trim());
    }

    private static bool IsExistingFile(string path, Func<string, bool> fileExists)
        => Path.IsPathRooted(path) && fileExists(path);

    private static string? FindProductCode(string? text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        var match = ProductCodePattern().Match(text);
        return match.Success ? match.Value : null;
    }

    [GeneratedRegex(@"\{[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}\}")]
    private static partial Regex ProductCodePattern();
}
