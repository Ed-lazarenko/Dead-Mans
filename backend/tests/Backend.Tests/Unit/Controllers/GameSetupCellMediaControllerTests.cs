using backend.Application.Abstractions;
using backend.Application.Features.GameSetup;
using backend.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Tests.Unit.Controllers;

public sealed class GameSetupCellMediaControllerTests
{
    [Fact]
    public async Task Upload_WhenFileExceedsLimit_ReturnsBadRequestWithoutCallingService()
    {
        var service = new TrackingGameSetupCellMediaService();
        var controller = CreateController(service);
        var file = CreateFile(GameSetupCellMediaLimits.MaxUploadBytes + 1);

        var result = await controller.Upload(Guid.NewGuid(), file, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
        Assert.False(service.UploadCalled);
    }

    private static GameSetupCellMediaController CreateController(IGameSetupCellMediaService service)
    {
        var controller = new GameSetupCellMediaController(service)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        return controller;
    }

    private static IFormFile CreateFile(long length)
    {
        var stream = new MemoryStream(new byte[Math.Min(length, 16)]);
        return new FormFile(stream, 0, length, "file", "cell.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png",
        };
    }

    private sealed class TrackingGameSetupCellMediaService : IGameSetupCellMediaService
    {
        public bool UploadCalled { get; private set; }

        public Task<UploadDraftGameSetupCellMediaResult> UploadAsync(Guid cellId, Stream content, string contentType, long contentLength, CancellationToken cancellationToken = default)
        {
            UploadCalled = true;
            throw new NotSupportedException();
        }

        public Task<DeleteDraftGameSetupCellMediaResult> DeleteAsync(Guid cellId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
