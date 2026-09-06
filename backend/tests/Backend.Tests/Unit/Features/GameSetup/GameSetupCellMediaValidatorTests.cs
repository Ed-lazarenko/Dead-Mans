using backend.Application.Abstractions.Repositories;
using backend.Application.Configuration;
using backend.Application.Features.GameSetup;
using backend.Domain.Persistence;

namespace Backend.Tests.Unit.Features.GameSetup;

public sealed class GameSetupCellMediaValidatorTests
{
    [Fact]
    public void BuildGameMediaPrefix_UsesConfiguredGamesPrefix()
    {
        var gameId = Guid.Parse("c6c6a0da-0bd1-4f0b-bb2f-9a4c9c8b7f6a");

        var prefix = GameMediaObjectKeyFormat.BuildGameMediaPrefix("games", gameId);

        Assert.Equal($"games/{gameId}/", prefix);
    }

    [Fact]
    public void BuildObjectKey_UsesConfiguredLayoutMatchingSeedMigration()
    {
        var settings = new MediaStorageSettings
        {
            PublicBaseUrl = "http://localhost:9000",
            BucketName = "project-bucket",
            GamesPrefix = "games",
            CardsGroup = "cards",
        };
        var gameId = Guid.Parse("c6c6a0da-0bd1-4f0b-bb2f-9a4c9c8b7f6a");
        var draftCell = new GameSetupDraftCellRef(gameId, Guid.NewGuid(), Guid.NewGuid(), 0, 0);

        var objectKey = GameSetupCellMediaValidator.BuildObjectKey(settings, draftCell, ".png");

        Assert.Equal($"games/{gameId}/cards/1-1.png", objectKey);
    }

    [Theory]
    [InlineData("image/png", 1024, true)]
    [InlineData("image/jpeg", 1024, true)]
    [InlineData("image/webp", 1024, true)]
    [InlineData("image/gif", 1024, true)]
    [InlineData("application/pdf", 1024, false)]
    [InlineData("image/png", 0, false)]
    [InlineData("image/png", GameSetupCellMediaLimits.MaxUploadBytes + 1, false)]
    public void IsAllowedUpload_EnforcesMimeAndSize(string mimeType, long length, bool expected)
    {
        var allowed = GameSetupCellMediaValidator.IsAllowedUpload(mimeType, length, out var normalized);

        Assert.Equal(expected, allowed);
        if (expected)
        {
            Assert.Equal(mimeType, normalized);
        }
    }

    public static TheoryData<string, byte[], bool> FileSignatures => new()
    {
        { "image/jpeg", [0xff, 0xd8, 0xff, 0xe0, 0x00], true },
        { "image/png", [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a], true },
        { "image/gif", "GIF87a"u8.ToArray(), true },
        { "image/gif", "GIF89a"u8.ToArray(), true },
        { "image/webp", [0x52, 0x49, 0x46, 0x46, 0x04, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50], true },
        { "image/png", "not-an-image"u8.ToArray(), false },
        { "image/jpeg", [0xff, 0xd8], false },
        { "application/octet-stream", [0xff, 0xd8, 0xff], false },
    };

    [Theory]
    [MemberData(nameof(FileSignatures))]
    public void HasMatchingFileSignature_ValidatesContentAndRestoresStreamPosition(
        string mimeType,
        byte[] content,
        bool expected
    )
    {
        using var stream = new MemoryStream(content);

        var matches = GameSetupCellMediaValidator.HasMatchingFileSignature(stream, mimeType);

        Assert.Equal(expected, matches);
        Assert.Equal(0, stream.Position);
    }
}
