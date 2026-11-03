using DebloatManager.Services;
using Xunit;

namespace DebloatManager.Tests;

public class LeftoverCleanerTests
{
    private static readonly string[] Roots = { @"C:\Program Files", @"C:\Users\me\AppData\Local" };
    private static readonly string[] NoOthers = Array.Empty<string>();

    [Theory]
    [InlineData(@"C:\Program Files\VideoLAN\VLC")]
    [InlineData(@"C:\Program Files\VLC\")]
    [InlineData(@"C:\Users\me\AppData\Local\Programs\Foo")]
    public void ProgramFoldersAreEligible(string path)
    {
        Assert.True(LeftoverCleaner.IsSafeToDelete(path, Roots, NoOthers));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("relative\\path")]
    [InlineData(@"C:\")]
    [InlineData(@"C:\Windows")]
    [InlineData(@"C:\Windows\System32")]
    [InlineData(@"C:\xampp")]
    [InlineData(@"D:\Games\Foo")]
    [InlineData(@"C:\Users\me\Documents")]
    [InlineData(@"C:\Program Files")]
    [InlineData(@"C:\Program Files\Common Files")]
    [InlineData(@"C:\Program Files\Common Files\Vendor")]
    [InlineData(@"C:\Program Files\WindowsApps\Foo")]
    [InlineData(@"C:\Program Files\Windows Defender")]
    [InlineData(@"C:\Users\me\AppData\Local\Microsoft\OneDrive")]
    [InlineData(@"C:\Users\me\AppData\Local\Programs")]
    [InlineData(@"C:\Users\me\AppData\Local\Packages\Foo")]
    public void SharedOrUnknownLocationsAreRefused(string? path)
    {
        Assert.False(LeftoverCleaner.IsSafeToDelete(path, Roots, NoOthers));
    }

    [Theory]
    [InlineData(@"C:\Program Files\Vendor", @"C:\Program Files\Vendor\OtherProduct")]
    [InlineData(@"C:\Program Files\Vendor\Product", @"C:\Program Files\Vendor")]
    [InlineData(@"C:\Program Files\Vendor", @"C:\Program Files\Vendor\")]
    public void FoldersUsedByOtherProgramsAreRefused(string path, string other)
    {
        Assert.False(LeftoverCleaner.IsSafeToDelete(path, Roots, new[] { other }));
    }

    [Fact]
    public void SimilarlyNamedFolderDoesNotBlockDeletion()
    {
        Assert.True(LeftoverCleaner.IsSafeToDelete(@"C:\Program Files\Foo", Roots, new[] { @"C:\Program Files\FooBar" }));
    }
}
