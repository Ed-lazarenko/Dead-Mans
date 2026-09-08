using backend.Application.Configuration;

namespace Backend.Tests.Unit.Application.Configuration;

public sealed class MediaStorageSettingsValidationTests
{
    [Theory]
    [InlineData("games", true)]
    [InlineData("tenant/game-media", true)]
    [InlineData("../games", false)]
    [InlineData("games//cards", false)]
    [InlineData("games?token", false)]
    [InlineData("", false)]
    public void IsValidObjectKeyPrefix_RejectsAmbiguousOrUnsafeSegments(
        string prefix,
        bool expected
    )
    {
        Assert.Equal(expected, MediaStorageSettings.IsValidObjectKeyPrefix(prefix));
    }
}
