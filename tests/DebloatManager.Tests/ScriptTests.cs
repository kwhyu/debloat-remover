using System.Text.RegularExpressions;
using DebloatManager.Services;
using Xunit;

namespace DebloatManager.Tests;

// Scripts are only parsed or run with harmless output here, never executed against the system.
public class ScriptTests
{
    public static TheoryData<string> AllScripts() => new()
    {
        RemovalScripts.StoreApp("Microsoft.BingNews"),
        RemovalScripts.Feature("TelnetClient"),
        RemovalScripts.Service("Fax"),
        RestorePointService.Script,
        AppScannerService.StoreAppsScript,
        AppScannerService.FeaturesScript,
        AppScannerService.ServicesScript(),
    };

    [Theory]
    [MemberData(nameof(AllScripts))]
    public async Task ScriptsHaveValidPowerShellSyntax(string script)
    {
        var quoted = PowerShellRunner.Quote(script);
        var check = "$errors = $null; [void][System.Management.Automation.Language.Parser]::ParseInput(" + quoted + ", [ref]$null, [ref]$errors); $errors.Count";

        var result = await PowerShellRunner.RunAsync(check);

        Assert.True(result.Success, result.Error);
        Assert.Equal("0", result.Output);
    }

    [Theory]
    [InlineData("O'Brien App")]
    [InlineData("App’s Tool")]
    [InlineData("$(Write-Output hacked)")]
    [InlineData("'; Write-Output hacked; '")]
    [InlineData("‘; Write-Output hacked; ‘")]
    [InlineData("Name with `backtick and \"quotes\"")]
    [InlineData("Aplikasi éè 日本語")]
    public async Task QuotedValuesAreNeverExecuted(string value)
    {
        var result = await PowerShellRunner.RunAsync($"$name = {PowerShellRunner.Quote(value)}; [Console]::Out.Write($name)");

        Assert.True(result.Success, result.Error);
        Assert.Equal(NormalizeTypographicQuotes(value), NormalizeTypographicQuotes(result.Output));
    }

    [Fact]
    public async Task SingleItemListsStayJsonArrays()
    {
        var result = await PowerShellRunner.RunAsync("ConvertTo-Json -InputObject @([PSCustomObject]@{ A = 1 }) -Compress");
        Assert.Equal("[{\"A\":1}]", result.Output);
    }

    [Fact]
    public void ParseJsonUsesLastLine()
    {
        var parsed = PowerShellRunner.ParseJson<Dictionary<string, int>>("noise\n{\"a\":1}\n");
        Assert.Equal(1, parsed?["a"]);
    }

    [Fact]
    public void StoreAppScriptVerifiesBeforeReportingSuccess()
    {
        var script = RemovalScripts.StoreApp("X");

        Assert.Contains("Remove-AppxProvisionedPackage", script);
        Assert.Contains("$_.DisplayName -eq $name", script);
        Assert.Contains("$remaining.Count -eq 0 -and $stillProvisioned.Count -eq 0", script);
        Assert.True(script.IndexOf("$remaining =", StringComparison.Ordinal) > script.LastIndexOf("Remove-TargetPackage $package", StringComparison.Ordinal));
    }

    private static string NormalizeTypographicQuotes(string text) => Regex.Replace(text, "['‘’]", "'");
}
