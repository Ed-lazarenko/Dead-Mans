using backend.Api.Configuration;

namespace Backend.Tests.Unit.Api.Configuration;

public sealed class DataProtectionSecurityOptionsTests
{
    [Fact]
    public void IsValidKeysDirectory_WhenOptionalAllowsEmptyValue()
    {
        Assert.True(DataProtectionSecurityOptions.IsValidKeysDirectory(string.Empty, required: false));
    }

    [Fact]
    public void IsValidKeysDirectory_WhenRequiredRejectsEmptyValue()
    {
        Assert.False(DataProtectionSecurityOptions.IsValidKeysDirectory(string.Empty, required: true));
    }

    [Fact]
    public void IsValidKeysDirectory_RejectsRelativeAndRootPaths()
    {
        Assert.False(DataProtectionSecurityOptions.IsValidKeysDirectory("keys", required: true));
        Assert.False(
            DataProtectionSecurityOptions.IsValidKeysDirectory(
                Path.GetPathRoot(Path.GetFullPath(Path.DirectorySeparatorChar.ToString())),
                required: true
            )
        );
    }

    [Fact]
    public void IsValidKeysDirectory_AcceptsAbsoluteNonRootDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "deadmans", "keys");

        Assert.True(DataProtectionSecurityOptions.IsValidKeysDirectory(path, required: true));
    }
}
