using DebloatManager.Services;
using Xunit;

namespace DebloatManager.Tests;

public class LeftoverPlannerTests
{
    private static LeftoverContext Context(IEnumerable<string> existing, IEnumerable<string>? otherLocations = null, IEnumerable<string>? otherNames = null)
    {
        var folders = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return new LeftoverContext(
            new[] { @"C:\Program Files", @"C:\ProgramData", @"C:\Data\Local", @"C:\Data\Roaming" },
            new[] { @"C:\Data\Local", @"C:\Data\Roaming", @"C:\ProgramData" },
            (otherLocations ?? Array.Empty<string>()).ToList(),
            (otherNames ?? Array.Empty<string>()).ToHashSet(StringComparer.OrdinalIgnoreCase),
            folders.Contains);
    }

    [Theory]
    [InlineData("7-Zip 23.01 (x64)", "7-Zip")]
    [InlineData("Cheat Engine 7.5", "Cheat Engine")]
    [InlineData("Unity 6000.2.8f1", "Unity")]
    [InlineData("VLC media player", "VLC media player")]
    [InlineData("Git", "Git")]
    public void NamesLoseVersionAndArchitecture(string input, string expected)
    {
        Assert.Equal(expected, LeftoverPlanner.CleanName(input));
    }

    [Fact]
    public void FindsInstallAndDataFolders()
    {
        var context = Context(new[] { @"C:\Program Files\Foo App", @"C:\Data\Local\Foo App", @"C:\Data\Roaming\FooCorp\Foo App" });

        var plan = LeftoverPlanner.Plan("Foo App 1.2", "FooCorp", @"C:\Program Files\Foo App", null, context);

        Assert.Equal(3, plan.Count);
        Assert.All(plan, folder => Assert.True(folder.CanDelete));
        Assert.True(plan[0].IsInstallFolder);
    }

    [Fact]
    public void FindsInstallFolderFromUninstallerWhenNotRegistered()
    {
        var context = Context(new[] { @"C:\Program Files\VideoLAN\VLC" });

        var plan = LeftoverPlanner.Plan("VLC media player", "VideoLAN", null, @"C:\Program Files\VideoLAN\VLC\uninstall.exe", context);

        Assert.True(Assert.Single(plan).CanDelete);
    }

    [Fact]
    public void LauncherFolderIsNeverTreatedAsTheProgramFolder()
    {
        var context = Context(new[] { @"C:\Program Files\Steam" });

        var plan = LeftoverPlanner.Plan("Counter-Strike 2", "Valve", null, @"C:\Program Files\Steam\steam.exe", context);

        Assert.Empty(plan);
    }

    [Fact]
    public void FolderUsedByAnotherProgramIsKeptWithAReason()
    {
        var context = Context(new[] { @"C:\Program Files\Vendor" }, new[] { @"C:\Program Files\Vendor\Other" });

        var folder = Assert.Single(LeftoverPlanner.Plan("Vendor Tool", "Vendor", @"C:\Program Files\Vendor", null, context));

        Assert.False(folder.CanDelete);
        Assert.NotNull(folder.Reason);
    }

    [Fact]
    public void DataFolderSharedByNameWithAnotherProgramIsKept()
    {
        var context = Context(new[] { @"C:\Data\Local\Python" }, otherNames: new[] { "Python" });

        var folder = Assert.Single(LeftoverPlanner.Plan("Python 3.12", "PSF", null, null, context));

        Assert.False(folder.CanDelete);
    }

    [Fact]
    public void SharedVendorDataFolderIsKept()
    {
        var context = Context(new[] { @"C:\Data\Local\Microsoft\Teams" });

        Assert.Empty(LeftoverPlanner.Plan("Teams", "Microsoft", null, null, context).Where(f => f.CanDelete));
    }

    [Theory]
    [InlineData(@"D:\Program Files\HoYoPlay", "HoYoPlay", true)]
    [InlineData(@"C:\xampp", "XAMPP", true)]
    [InlineData(@"D:\Games", "Some Game", false)]
    [InlineData(@"D:\", "Some Game", false)]
    public void FolderOutsideKnownLocationsMustMatchTheProgramName(string path, string name, bool expected)
    {
        var folder = Assert.Single(LeftoverPlanner.Plan(name, "Unknown", path, null, Context(new[] { path })));

        Assert.Equal(expected, folder.CanDelete);
    }

    [Theory]
    [InlineData(@"C:\Windows\Foo", "Foo")]
    [InlineData(@"C:\Users\someone\Foo", "Foo")]
    public void SystemAndProfileLocationsAreKept(string path, string name)
    {
        var folder = Assert.Single(LeftoverPlanner.Plan(name, "Unknown", path, null, Context(new[] { path })));

        Assert.False(folder.CanDelete);
    }

    [Fact]
    public void VendorInstallFolderDoesNotExposeVendorDataFolders()
    {
        var context = Context(new[] { @"C:\Program Files\Adobe", @"C:\Data\Local\Adobe", @"C:\Data\Roaming\Adobe" });

        var folder = Assert.Single(LeftoverPlanner.Plan("Adobe Premiere Pro 2020", "Adobe Inc.", @"C:\Program Files\Adobe", null, context));

        Assert.False(folder.CanDelete);
    }

    [Fact]
    public void LauncherFolderNamedAfterItsPublisherIsKept()
    {
        var context = Context(new[] { @"C:\Program Files\Epic Games", @"C:\Data\Local\Epic Games" });

        var folder = Assert.Single(LeftoverPlanner.Plan("Epic Games Launcher", "Epic Games, Inc.", @"C:\Program Files\Epic Games", null, context));

        Assert.False(folder.CanDelete);
    }

    [Fact]
    public void ProgramNamedLikeItsPublisherIsStillCleaned()
    {
        var context = Context(new[] { @"C:\Program Files\AnyDesk", @"C:\Data\Roaming\AnyDesk" });

        var plan = LeftoverPlanner.Plan("AnyDesk", "AnyDesk Software GmbH", @"C:\Program Files\AnyDesk", null, context);

        Assert.Equal(2, plan.Count);
        Assert.All(plan, folder => Assert.True(folder.CanDelete));
    }

    [Fact]
    public void GenericInstallFolderNameIsNotUsedToFindData()
    {
        var context = Context(new[] { @"C:\Program Files\Unity\Hub\Editor\6000\Editor", @"C:\Data\Local\Unity\Editor", @"C:\Data\Local\Editor" });

        var plan = LeftoverPlanner.Plan("Unity 6000.2.8f1", "Unity Technologies", @"C:\Program Files\Unity\Hub\Editor\6000\Editor", null, context);

        Assert.DoesNotContain(plan, folder => folder.Path.StartsWith(@"C:\Data", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PluginInsideAnotherProgramDoesNotTouchItsData()
    {
        var context = Context(new[] { @"C:\Program Files\obs-studio", @"C:\Data\Roaming\obs-studio" }, new[] { @"C:\Program Files\obs-studio" });

        var plan = LeftoverPlanner.Plan("obs-virtualcam version 2.1.2", "OBS", @"C:\Program Files\obs-studio", null, context);

        Assert.False(Assert.Single(plan).CanDelete);
    }

    [Fact]
    public void UninstallerFolderMustMatchTheProgramName()
    {
        var context = Context(new[] { @"C:\ProgramData\Red Giant\uninstall" });

        var plan = LeftoverPlanner.Plan("Magic Bullet Suite", "Maxon", null, @"C:\ProgramData\Red Giant\uninstall\uninstall-magicbullet.bat", context);

        Assert.Empty(plan);
    }

    [Fact]
    public void MissingFoldersAreIgnored()
    {
        Assert.Empty(LeftoverPlanner.Plan("Foo", "Bar", @"C:\Program Files\Foo", null, Context(Array.Empty<string>())));
    }
}
