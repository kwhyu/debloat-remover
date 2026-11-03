using DebloatManager.Models;
using DebloatManager.Services;
using Xunit;

namespace DebloatManager.Tests;

public class CatalogTests
{
    [Theory]
    [InlineData("Microsoft.BingWeather", RiskLevel.Safe)]
    [InlineData("Microsoft.Xbox.TCUI", RiskLevel.Caution)]
    [InlineData("Microsoft.XboxIdentityProvider", RiskLevel.Caution)]
    [InlineData("Microsoft.WindowsStore", RiskLevel.Risky)]
    [InlineData("Microsoft.SecHealthUI", RiskLevel.Protected)]
    [InlineData("SpotifyAB.SpotifyMusic", RiskLevel.Caution)]
    public void StoreAppsUseCatalogRisk(string name, RiskLevel expected)
    {
        Assert.Equal(expected, BloatwareCatalog.ClassifyStoreApp(name, "Microsoft Corporation", false, "Store")?.Risk);
    }

    [Theory]
    [InlineData("Microsoft.Windows.ShellExperienceHost")]
    [InlineData("MicrosoftWindows.Client.CBS")]
    [InlineData("windows.immersivecontrolpanel")]
    [InlineData("c5e2524a-ea46-4f67-841f-6a9465d9d515")]
    public void NonRemovableSystemPackagesAreProtected(string name)
    {
        Assert.Equal(RiskLevel.Protected, BloatwareCatalog.ClassifyStoreApp(name, "Microsoft Windows", true, "System")?.Risk);
    }

    [Theory]
    [InlineData("Electrum Bitcoin Wallet", "Electrum")]
    [InlineData("Family Tree Maker", "Ancestry")]
    [InlineData("People Finder", "Acme")]
    [InlineData("Teamspeak", "TeamSpeak Systems")]
    [InlineData("Spotify", "Spotify AB")]
    public void UnrelatedProgramsAreNeverMarkedSafe(string name, string publisher)
    {
        Assert.Null(BloatwareCatalog.ClassifyProgram(name, publisher));
    }

    [Theory]
    [InlineData("www.instagram.com-E4B7766F")]
    [InlineData("Claude")]
    [InlineData("MyCompany.WalletApp")]
    public void UnrelatedStoreAppsStayUnknown(string name)
    {
        Assert.Null(BloatwareCatalog.ClassifyStoreApp(name, "Someone Else", false, "Developer"));
    }

    [Theory]
    [InlineData("Microsoft Visual C++ 2015-2022 Redistributable (x64)", "Microsoft Corporation")]
    [InlineData("Intel(R) Graphics Software & Drivers", "Intel")]
    public void RuntimesAndDriversNeedCaution(string name, string publisher)
    {
        Assert.Equal(RiskLevel.Caution, BloatwareCatalog.ClassifyProgram(name, publisher)?.Risk);
    }

    [Fact]
    public void WebViewIsRisky()
    {
        Assert.Equal(RiskLevel.Risky, BloatwareCatalog.ClassifyProgram("Microsoft Edge WebView2 Runtime", "Microsoft Corporation")?.Risk);
    }

    [Fact]
    public void ProtectedItemsCannotBeRemoved()
    {
        var item = new AppItem { Name = "Settings", Identifier = "x", Type = AppType.UwpApp, Risk = RiskLevel.Protected };
        Assert.False(item.CanRemove);
    }

    [Fact]
    public async Task ActionServiceRefusesProtectedItems()
    {
        var item = new AppItem { Name = "Settings", Identifier = "windows.immersivecontrolpanel", Type = AppType.UwpApp, Risk = RiskLevel.Protected };
        var result = await new AppActionService().RemoveAsync(item);
        Assert.False(result.Success);
        Assert.False(item.IsRemoved);
    }

    [Fact]
    public void DuplicatePackagesAreMergedAndNonPresentOnesHidden()
    {
        var items = AppScannerService.BuildStoreApps(new[]
        {
            new AppScannerService.StoreAppRecord { Name = "wpsappext1", Publisher = "CN=Kingsoft", SignatureKind = "Developer" },
            new AppScannerService.StoreAppRecord { Name = "wpsappext1", Publisher = "CN=Kingsoft", SignatureKind = "Developer" },
            new AppScannerService.StoreAppRecord { Name = "Microsoft.BingNews", Present = false },
        });

        Assert.Equal("wpsappext1", Assert.Single(items).Identifier);
    }

    [Theory]
    [InlineData("CN=Microsoft Corporation, O=Microsoft Corporation, C=US", "Microsoft Corporation")]
    [InlineData("CN=A5B1C2D3-0000-4000-8000-123456789ABC", "Unknown")]
    [InlineData(null, "Unknown")]
    public void PublisherIsReadable(string? input, string expected)
    {
        Assert.Equal(expected, AppScannerService.ExtractPublisher(input));
    }
}
