using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using backend.Api.Contracts;
using backend.Application.Abstractions;
using backend.Application.Abstractions.Auth;
using backend.Application.Features.GameSetup;
using backend.Data;
using backend.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using backend.Data.Entities;
using backend.Domain.Models;
using backend.Domain.Persistence;
using Backend.Tests.Support;
using backend.Messaging;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Backend.Tests.Integration.GameEndpoints;

public sealed class GameSetupCellMediaContractTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public GameSetupCellMediaContractTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task UploadCellMedia_WhenAdminAndDraftExists_ReturnsMediaUrl()
    {
        await ClearGamesAsync();
        var (gameId, cellId) = await SeedDraftWithSingleCellAsync();
        using var authenticatedFactory = CreateAuthenticatedFactory([AuthRoleCodes.Admin]);
        using var adminClient = authenticatedFactory.CreateClient();
        using var content = CreatePngUploadContent();

        var response = await adminClient.PostAsync($"/api/game/setup/cells/{cellId}/media", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GameBoardCellMediaDto>();
        Assert.NotNull(payload);
        Guid mediaAssetId;
        using (var scope = authenticatedFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            mediaAssetId = await db.BoardCellMedia
                .Where(link => link.CellId == cellId)
                .Select(link => link.MediaAssetId)
                .SingleAsync();
        }
        var expectedObjectKey = GameMediaObjectKeyFormat.BuildCardImageKey(
            "games",
            gameId,
            "cards",
            rowIndex: 0,
            colIndex: 0,
            mediaAssetId,
            ".png"
        );
        Assert.Contains(expectedObjectKey, payload!.Url, StringComparison.OrdinalIgnoreCase);

        var setupResponse = await adminClient.GetAsync("/api/game/setup");
        var setup = await setupResponse.Content.ReadFromJsonAsync<GameSetupSnapshotDto>();
        Assert.NotNull(setup);
        Assert.Contains(setup!.Cells, cell => cell.Id == cellId.ToString() && cell.Media.Count == 1);
    }

    [Fact]
    public async Task UploadCellMedia_WhenReplacingSameType_PreservesOnlyNewObject()
    {
        await ClearGamesAsync();
        var (gameId, cellId) = await SeedDraftWithSingleCellAsync();
        using var authenticatedFactory = CreateAuthenticatedFactory([AuthRoleCodes.Admin]);
        using var adminClient = authenticatedFactory.CreateClient();

        using (var firstContent = CreatePngUploadContent())
        {
            var firstResponse = await adminClient.PostAsync(
                $"/api/game/setup/cells/{cellId}/media",
                firstContent
            );
            Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        }

        string firstObjectKey;
        using (var scope = authenticatedFactory.Services.CreateScope())
        {
            var storage = Assert.IsType<InMemoryObjectStorage>(
                scope.ServiceProvider.GetRequiredService<IObjectStorage>()
            );
            firstObjectKey = Assert.Single(
                storage.ListObjectKeys("deadman-test", $"games/{gameId}/")
            );
        }

        using (var replacementContent = CreatePngUploadContent())
        {
            var replacementResponse = await adminClient.PostAsync(
                $"/api/game/setup/cells/{cellId}/media",
                replacementContent
            );
            Assert.Equal(HttpStatusCode.OK, replacementResponse.StatusCode);
        }

        using (var scope = authenticatedFactory.Services.CreateScope())
        {
            var storage = Assert.IsType<InMemoryObjectStorage>(
                scope.ServiceProvider.GetRequiredService<IObjectStorage>()
            );
            var replacementObjectKey = Assert.Single(
                storage.ListObjectKeys("deadman-test", $"games/{gameId}/")
            );
            Assert.NotEqual(firstObjectKey, replacementObjectKey);

            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var persistedObjectKey = await db.MediaAssets
                .Select(asset => asset.ObjectKey)
                .SingleAsync();
            Assert.Equal(replacementObjectKey, persistedObjectKey);
        }
    }

    [Fact]
    public async Task DeleteCellMedia_WhenAdminAndMediaExists_ReturnsNoContent()
    {
        await ClearGamesAsync();
        var (_, cellId) = await SeedDraftWithSingleCellAsync();
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);
        using var uploadContent = CreatePngUploadContent();
        var uploadResponse = await adminClient.PostAsync($"/api/game/setup/cells/{cellId}/media", uploadContent);
        Assert.Equal(HttpStatusCode.OK, uploadResponse.StatusCode);

        var deleteResponse = await adminClient.DeleteAsync($"/api/game/setup/cells/{cellId}/media");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var setupResponse = await adminClient.GetAsync("/api/game/setup");
        var setup = await setupResponse.Content.ReadFromJsonAsync<GameSetupSnapshotDto>();
        Assert.NotNull(setup);
        Assert.Contains(setup!.Cells, cell => cell.Id == cellId.ToString() && cell.Media.Count == 0);
    }

    [Fact]
    public async Task DeleteSetup_WhenDraftHasUploadedMedia_RemovesStoredObjects()
    {
        await ClearGamesAsync();
        var (gameId, cellId) = await SeedDraftWithSingleCellAsync();
        using var authenticatedFactory = CreateAuthenticatedFactory([AuthRoleCodes.Admin]);
        using var adminClient = authenticatedFactory.CreateClient();
        using var uploadContent = CreatePngUploadContent();
        var uploadResponse = await adminClient.PostAsync($"/api/game/setup/cells/{cellId}/media", uploadContent);
        Assert.Equal(HttpStatusCode.OK, uploadResponse.StatusCode);

        using (var scope = authenticatedFactory.Services.CreateScope())
        {
            var storage = scope.ServiceProvider.GetRequiredService<IObjectStorage>();
            var inMemory = Assert.IsType<InMemoryObjectStorage>(storage);
            Assert.NotEmpty(inMemory.ListObjectKeys("deadman-test", $"games/{gameId}/"));
        }

        var deleteResponse = await adminClient.DeleteAsync("/api/game/setup");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        using (var scope = authenticatedFactory.Services.CreateScope())
        {
            var storage = scope.ServiceProvider.GetRequiredService<IObjectStorage>();
            var inMemory = Assert.IsType<InMemoryObjectStorage>(storage);
            Assert.Empty(inMemory.ListObjectKeys("deadman-test", $"games/{gameId}/"));

            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(0, await db.Games.CountAsync());
            Assert.Equal(0, await db.MediaAssets.CountAsync());
        }

        var getSetupResponse = await adminClient.GetAsync("/api/game/setup");
        Assert.Equal(HttpStatusCode.NoContent, getSetupResponse.StatusCode);
    }

    [Fact]
    public async Task UploadCellMedia_WhenAnonymous_ReturnsUnauthorized()
    {
        using var content = CreatePngUploadContent();
        var response = await _client.PostAsync(
            $"/api/game/setup/cells/{Guid.NewGuid()}/media",
            content
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UploadCellMedia_WhenFileExceedsLimit_ReturnsBadRequest()
    {
        await ClearGamesAsync();
        var (_, cellId) = await SeedDraftWithSingleCellAsync();
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);
        using var content = CreateUploadContent(
            new byte[GameSetupCellMediaLimits.MaxUploadBytes + 1],
            "image/png",
            "oversized.png"
        );

        var response = await adminClient.PostAsync($"/api/game/setup/cells/{cellId}/media", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
    }

    [Fact]
    public async Task UploadCellMedia_WhenDeclaredMimeDoesNotMatchContent_ReturnsBadRequest()
    {
        await ClearGamesAsync();
        var (_, cellId) = await SeedDraftWithSingleCellAsync();
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);
        using var content = CreateUploadContent(
            "this-is-not-a-png"u8.ToArray(),
            "image/png",
            "spoofed.png"
        );

        var response = await adminClient.PostAsync($"/api/game/setup/cells/{cellId}/media", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.ErrorCodes.GameSetupInvalidCellMediaUpload, payload.Code);
    }

    private async Task<(Guid GameId, Guid CellId)> SeedDraftWithSingleCellAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var gameId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var cellId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        db.Games.Add(
            new Game
            {
                Id = gameId,
                Title = "Draft",
                Status = GameStatusValue.Draft,
                CreatedAtUtc = createdAt,
            }
        );
        db.GameBoards.Add(
            new GameBoard
            {
                Id = boardId,
                GameId = gameId,
                Version = 1,
                Rows = 1,
                Cols = 1,
                RowLabels = ["100"],
                ColLabels = ["1"],
                CreatedAtUtc = createdAt,
            }
        );
        db.BoardCells.Add(
            new BoardCell
            {
                Id = cellId,
                BoardId = boardId,
                RowIndex = 0,
                ColIndex = 0,
                State = BoardCellState.Closed,
                CellType = BoardCellPersistence.DefaultCellType,
                Cost = 100,
            }
        );
        await db.SaveChangesAsync();
        return (gameId, cellId);
    }

    private static MultipartFormDataContent CreatePngUploadContent()
    {
        var bytes = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg=="
        );
        return CreateUploadContent(bytes, "image/png", "cell.png");
    }

    private static MultipartFormDataContent CreateUploadContent(
        byte[] bytes,
        string contentType,
        string fileName
    )
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", fileName);
        return content;
    }

    private async Task ClearGamesAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.BoardCellMedia.RemoveRange(db.BoardCellMedia);
        db.MediaAssets.RemoveRange(db.MediaAssets);
        db.BoardCells.RemoveRange(db.BoardCells);
        db.GameBoards.RemoveRange(db.GameBoards);
        db.Games.RemoveRange(db.Games);
        await db.SaveChangesAsync();
    }

    private WebApplicationFactory<Program> CreateAuthenticatedFactory(string[] roles)
    {
        return _factory.WithWebHostBuilder(
            builder =>
                builder.ConfigureServices(
                    services =>
                    {
                        services.RemoveAll<IClaimsTransformation>();
                        services.AddSingleton<IClaimsTransformation, PassthroughClaimsTransformation>();
                        services
                            .AddAuthentication(options =>
                            {
                                options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                                options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
                                options.DefaultScheme = TestAuthenticationHandler.SchemeName;
                            })
                            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                                TestAuthenticationHandler.SchemeName,
                                options => options.ClaimsIssuer = string.Join(',', roles)
                            );
                    }
                )
        );
    }

    private HttpClient CreateAuthenticatedClient(string[] roles)
    {
        return CreateAuthenticatedFactory(roles).CreateClient();
    }

    private sealed class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "TestAuth";

        public TestAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder
        )
            : base(options, logger, encoder) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var roles = (Options.ClaimsIssuer ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) };
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    private sealed class PassthroughClaimsTransformation : IClaimsTransformation
    {
        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal) => Task.FromResult(principal);
    }
}
