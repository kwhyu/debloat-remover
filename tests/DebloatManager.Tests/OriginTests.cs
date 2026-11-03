using System.IO;
using DebloatManager.Models;
using DebloatManager.Services;
using Xunit;

namespace DebloatManager.Tests;

// Reads existing signed files and only modifies throwaway copies in a temp folder.
public sealed class OriginTests : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(), $"DebloatManagerTest-{Guid.NewGuid():N}");

    public OriginTests() => Directory.CreateDirectory(folder);

    public void Dispose() => Directory.Delete(folder, recursive: true);

    private static string SignedFile()
    {
        var dotnet = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet", "dotnet.exe");
        Assert.True(File.Exists(dotnet), "The test needs the signed dotnet.exe.");
        return dotnet;
    }

    [Fact]
    public void ValidSignatureNamesTheSigner()
    {
        var result = SignatureInspector.Inspect(SignedFile());

        Assert.Equal(OriginStatus.Signed, result.Status);
        Assert.False(string.IsNullOrWhiteSpace(result.Signer));
    }

    [Fact]
    public void ProductCodeConvertsToInstallerKey()
    {
        Assert.Equal("6D9B2FEB63D49973DA8C6FF1916290C2", ProgramFileLocator.ToInstallerProductKey("{BEF2B9D6-4D36-3799-ADC8-F61F1926092C}"));
        Assert.Null(ProgramFileLocator.ToInstallerProductKey("7-Zip"));
    }

    [Fact]
    public void UnsignedFileInWindowsFolderIsNotReportedAsUnsigned()
    {
        var cached = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "notepad.exe");
        var entry = new UninstallEntry(Microsoft.Win32.RegistryHive.CurrentUser, "x", "x", "x", "x", "x", null, null, false, false, cached);

        var result = ProgramFileLocator.InspectBestFile(entry, _ => new SignatureResult(OriginStatus.Invalid, null, "Stripped."));

        Assert.Equal(OriginStatus.NotChecked, result.Status);
    }

    [Fact]
    public void UnsignedFileOutsideWindowsIsReported()
    {
        var path = Path.Combine(folder, "tool.exe");
        File.WriteAllBytes(path, new byte[] { 0x4D, 0x5A, 0, 0 });
        var entry = new UninstallEntry(Microsoft.Win32.RegistryHive.CurrentUser, "x", "x", "x", "x", "x", null, null, false, false, path);

        Assert.Equal(OriginStatus.Unsigned, ProgramFileLocator.InspectBestFile(entry).Status);
    }

    [Fact]
    public void UnsignedFileIsReportedAsUnsigned()
    {
        var path = Path.Combine(folder, "plain.exe");
        File.WriteAllBytes(path, new byte[] { 0x4D, 0x5A, 0, 0 });

        Assert.Equal(OriginStatus.Unsigned, SignatureInspector.Inspect(path).Status);
    }

    [Fact]
    public void ChangedFileFailsTheSignatureCheck()
    {
        var path = Path.Combine(folder, "changed.exe");
        File.Copy(SignedFile(), path);
        var bytes = File.ReadAllBytes(path);
        bytes[bytes.Length / 4] ^= 0xFF;
        File.WriteAllBytes(path, bytes);

        var result = SignatureInspector.Inspect(path);

        Assert.Equal(OriginStatus.Invalid, result.Status);
        Assert.NotNull(result.Problem);
    }

    [Fact]
    public void MissingFileIsNotChecked()
    {
        Assert.Equal(OriginStatus.NotChecked, SignatureInspector.Inspect(Path.Combine(folder, "missing.exe")).Status);
    }

    [Theory]
    [InlineData("System", OriginStatus.Windows)]
    [InlineData("Store", OriginStatus.Store)]
    [InlineData("Developer", OriginStatus.Signed)]
    [InlineData("None", OriginStatus.Unsigned)]
    [InlineData(null, OriginStatus.NotChecked)]
    public void PackageSignatureKindMapsToOrigin(string? kind, OriginStatus expected)
    {
        Assert.Equal(expected, AppScannerService.OriginOfPackage(kind));
    }

    [Theory]
    [InlineData("Clipchamp", "CN=A5B1C2D3-0000-4000-8000-123456789ABC", "Clipchamp")]
    [InlineData("ms-resource:PublisherName", "CN=Contoso", "Contoso")]
    [InlineData(null, "CN=Contoso, O=Contoso", "Contoso")]
    public void StoreAppsPreferTheReadablePublisherName(string? manifest, string certificate, string expected)
    {
        Assert.Equal(expected, AppScannerService.ChoosePublisher(manifest, certificate));
    }

    [Fact]
    public void UnsignedSummaryDoesNotCallTheProgramHarmful()
    {
        var item = new AppItem { Name = "x", Identifier = "x", Type = AppType.Win32App, Publisher = "Acme", Origin = OriginStatus.Unsigned };

        Assert.Contains("cannot be confirmed", item.OriginSummary);
        Assert.Contains("does not mean it is harmful", item.OriginSummary);
    }

    [Fact]
    public void SignedSummaryDoesNotClaimTheProgramIsSafe()
    {
        var item = new AppItem { Name = "x", Identifier = "x", Type = AppType.Win32App, Origin = OriginStatus.Signed, Signer = "Acme" };

        Assert.Contains("not that it is safe", item.OriginSummary);
    }

    [Fact]
    public void SourceDetailsShowDateAndLocation()
    {
        var item = new AppItem { Name = "x", Identifier = "x", Type = AppType.Win32App, InstallDate = "20260115", InstallLocation = @"C:\Apps\X" };

        Assert.Contains("15 Jan 2026", item.SourceDetails);
        Assert.Contains(@"C:\Apps\X", item.SourceDetails);
    }
}
