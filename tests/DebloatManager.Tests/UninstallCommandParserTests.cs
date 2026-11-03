using DebloatManager.Services;
using Xunit;

namespace DebloatManager.Tests;

public class UninstallCommandParserTests
{
    private const string Code = "{12EE4C16-3A8B-49C1-BC34-D8F1BEFDCBA7}";

    [Theory]
    [InlineData("MsiExec.exe /I" + Code)]
    [InlineData("MsiExec.exe /X" + Code)]
    [InlineData("msiexec /i " + Code + " /qb")]
    public void MsiCommandsAlwaysUninstallNeverRepair(string command)
    {
        var result = UninstallCommandParser.Parse(command, Code, true);

        Assert.NotNull(result);
        Assert.True(result.IsMsi);
        Assert.EndsWith("msiexec.exe", result.FileName, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith($"/x {Code}", result.Arguments);
        Assert.DoesNotContain("/i", result.Arguments, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void QuotedPathKeepsArguments()
    {
        var result = UninstallCommandParser.Parse("\"C:\\Program Files\\Unity Hub\\Uninstall Unity Hub.exe\" /allusers /S", "x", false);

        Assert.Equal(@"C:\Program Files\Unity Hub\Uninstall Unity Hub.exe", result?.FileName);
        Assert.Equal("/allusers /S", result?.Arguments);
    }

    [Fact]
    public void UnquotedPathWithSpacesIsResolved()
    {
        const string exe = @"D:\Program Files\HoYoPlay\launcher.exe";
        var result = UninstallCommandParser.Parse($"{exe} --uninstall_game=hk4e_global", "x", false, path => path == exe);

        Assert.Equal(exe, result?.FileName);
        Assert.Equal("--uninstall_game=hk4e_global", result?.Arguments);
    }

    [Fact]
    public void UnquotedPathWithoutArgumentsIsResolved()
    {
        const string exe = @"C:\Program Files\AntiCheatExpert\Uninstaller.exe";
        var result = UninstallCommandParser.Parse(exe, "x", false, path => path == exe);

        Assert.Equal(exe, result?.FileName);
        Assert.Equal(string.Empty, result?.Arguments);
    }

    [Fact]
    public void BatchFilesRunThroughTheCommandProcessor()
    {
        var result = UninstallCommandParser.Parse("\"C:\\Program Data\\uninstall.bat\"", "x", false);

        Assert.EndsWith("cmd.exe", result?.FileName, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"C:\\Program Data\\uninstall.bat\"", result?.Arguments);
    }

    [Fact]
    public void MissingCommandUsesRegistryProductCodeForMsi()
    {
        Assert.StartsWith($"/x {Code}", UninstallCommandParser.Parse(null, Code, true)?.Arguments);
    }

    [Fact]
    public void MissingCommandWithoutMsiCannotBeParsed()
    {
        Assert.Null(UninstallCommandParser.Parse("  ", "SomeApp", false));
    }
}
