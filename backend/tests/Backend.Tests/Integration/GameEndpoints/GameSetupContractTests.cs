using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using backend.Api.Contracts;
using backend.Application.Abstractions.Auth;
using backend.Application.Abstractions.Realtime;
using backend.Application.Abstractions;
using backend.Application.Configuration;
using backend.Application.Contracts;
using backend.Data;
using backend.Domain.Persistence;
using backend.Messaging;
using Backend.Tests.Support;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Backend.Tests.Integration.GameEndpoints;

public sealed class GameSetupContractTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public GameSetupContractTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetSetup_WhenAnonymous_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/game/setup");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetSetup_WhenAdminAndNoDraft_ReturnsNoContent()
    {
        await ClearGamesAsync();
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var response = await adminClient.GetAsync("/api/game/setup");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task CreateSetup_WhenAdmin_CreatesDraftSnapshot()
    {
        await ClearGamesAsync();
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var response = await adminClient.PostAsJsonAsync(
            "/api/game/setup",
            new CreateGameSetupRequestDto("Stream setup")
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GameSetupSnapshotDto>();
        Assert.NotNull(payload);
        Assert.Equal("Stream setup", payload.Title);
        Assert.Equal(GameStatusValue.Draft, payload.Status);
        Assert.Equal(25, payload.Cells.Count);
        Assert.Equal(5, payload.Rows);
        Assert.Equal(["100", "125", "150", "175", "200"], payload.RowLabels);
        Assert.All(payload.Cells, cell => Assert.Null(cell.Title));
        Assert.Equal(
            GameSetupDefaults.DefaultRowCosts,
            payload.Cells.GroupBy(cell => cell.Row).OrderBy(group => group.Key).Select(group => group.First().Cost).ToArray()
        );
    }

    [Fact]
    public async Task CreateSetup_WhenRealtimePublishFails_StillReturnsCreated()
    {
        await ClearGamesAsync();
        using var adminClient = CreateAuthenticatedClient(
            [AuthRoleCodes.Admin],
            new FailingGameSetupEventsPublisher()
        );

        var response = await adminClient.PostAsJsonAsync(
            "/api/game/setup",
            new CreateGameSetupRequestDto("Stream setup")
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task DeleteSetup_WhenAdmin_DeletesDraft()
    {
        await ClearGamesAsync();
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var createResponse = await adminClient.PostAsJsonAsync(
            "/api/game/setup",
            new CreateGameSetupRequestDto("Draft to delete")
        );
        var created = await createResponse.Content.ReadFromJsonAsync<GameSetupSnapshotDto>();
        Assert.NotNull(created);

        var customizedRowLabels = created.RowLabels.ToArray();
        customizedRowLabels[0] = "Custom row";

        var updateResponse = await adminClient.PutAsJsonAsync(
            "/api/game/setup",
            new UpdateGameSetupRequestDto(
                created.Version,
                "Draft to delete",
                customizedRowLabels,
                created.ColLabels.ToArray(),
                created.Cells
                    .Select(
                        cell =>
                            new UpdateGameSetupCellDto(
                                cell.Id,
                                cell.Row,
                                cell.Col,
                                "Custom title",
                                cell.Cost + 99
                            )
                    )
                    .ToArray()
            )
        );
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var deleteResponse = await adminClient.DeleteAsync("/api/game/setup");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        var getResponse = await adminClient.GetAsync("/api/game/setup");
        Assert.Equal(HttpStatusCode.NoContent, getResponse.StatusCode);
    }

    [Fact]
    public async Task GetSetup_WhenDraftExists_ReturnsDraftSnapshot()
    {
        await ClearGamesAsync();
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var createResponse = await adminClient.PostAsJsonAsync(
            "/api/game/setup",
            new CreateGameSetupRequestDto("Existing draft")
        );
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var getResponse = await adminClient.GetAsync("/api/game/setup");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var payload = await getResponse.Content.ReadFromJsonAsync<GameSetupSnapshotDto>();
        Assert.NotNull(payload);
        Assert.Equal("Existing draft", payload.Title);
        Assert.Equal(GameStatusValue.Draft, payload.Status);
    }

    [Fact]
    public async Task CreateSetup_WhenDraftAlreadyExists_ReturnsConflict()
    {
        await ClearGamesAsync();
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var firstResponse = await adminClient.PostAsJsonAsync(
            "/api/game/setup",
            new CreateGameSetupRequestDto("First draft")
        );
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var secondResponse = await adminClient.PostAsJsonAsync(
            "/api/game/setup",
            new CreateGameSetupRequestDto("Second draft")
        );

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
        var payload = await secondResponse.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.DraftGameAlreadyExists, payload.Error);
        Assert.Equal(AppMessages.ErrorCodes.GameSetupDraftExists, payload.Code);
    }

    [Fact]
    public async Task CreateSetup_WhenTitleIsEmpty_ReturnsBadRequest()
    {
        await ClearGamesAsync();
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var response = await adminClient.PostAsJsonAsync(
            "/api/game/setup",
            new CreateGameSetupRequestDto(" ")
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.InvalidGameSetupTitle, payload.Error);
        Assert.Equal(AppMessages.ErrorCodes.InvalidGameSetupTitle, payload.Code);
    }

    [Fact]
    public async Task GetSetup_WhenViewer_ReturnsForbidden()
    {
        using var viewerClient = CreateAuthenticatedClient([AuthRoleCodes.Viewer]);

        var response = await viewerClient.GetAsync("/api/game/setup");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateSetup_WhenAdmin_SavesDraftChanges()
    {
        await ClearGamesAsync();
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var createResponse = await adminClient.PostAsJsonAsync(
            "/api/game/setup",
            new CreateGameSetupRequestDto("Before save")
        );
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<GameSetupSnapshotDto>();
        Assert.NotNull(created);

        var updateResponse = await adminClient.PutAsJsonAsync(
            "/api/game/setup",
            new UpdateGameSetupRequestDto(
                created.Version,
                "After save",
                ["100", "125", "150", "175", "200"],
                ["Col A", "Col B", "Col C", "Col D", "Col E"],
                created.Cells
                    .Select(
                        cell =>
                            new UpdateGameSetupCellDto(
                                cell.Id,
                                cell.Row,
                                cell.Col,
                                $"Saved {cell.Row}-{cell.Col}",
                                cell.Cost + 10
                            )
                    )
                    .ToArray()
            )
        );

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var saved = await updateResponse.Content.ReadFromJsonAsync<GameSetupSnapshotDto>();
        Assert.NotNull(saved);
        Assert.Equal("After save", saved.Title);
        Assert.Equal(["Col A", "Col B", "Col C", "Col D", "Col E"], saved.ColLabels);
        Assert.All(saved.Cells, cell => Assert.Equal(cell.Cost - 10, created.Cells.Single(c => c.Id == cell.Id).Cost));
        Assert.Equal(2, saved.Version);

        var getResponse = await adminClient.GetAsync("/api/game/setup");
        var loaded = await getResponse.Content.ReadFromJsonAsync<GameSetupSnapshotDto>();
        Assert.NotNull(loaded);
        Assert.Equal("After save", loaded.Title);
        Assert.Equal("Saved 0-0", loaded.Cells[0].Title);
    }

    [Fact]
    public async Task UpdateSetup_WhenNoDraft_ReturnsNotFound()
    {
        await ClearGamesAsync();
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var response = await adminClient.PutAsJsonAsync(
            "/api/game/setup",
            new UpdateGameSetupRequestDto(
                1,
                "Missing draft",
                ["1"],
                ["Col 1"],
                [new UpdateGameSetupCellDto(Guid.NewGuid().ToString(), 0, 0, "Card", 0)]
            )
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.ErrorCodes.GameSetupNoDraft, payload.Code);
    }

    [Fact]
    public async Task UpdateSetup_WhenTitleIsEmpty_ReturnsBadRequest()
    {
        await ClearGamesAsync();
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);
        var created = await adminClient.PostAsJsonAsync(
            "/api/game/setup",
            new CreateGameSetupRequestDto("Draft")
        );
        var snapshot = await created.Content.ReadFromJsonAsync<GameSetupSnapshotDto>();
        Assert.NotNull(snapshot);

        var response = await adminClient.PutAsJsonAsync(
            "/api/game/setup",
            new UpdateGameSetupRequestDto(
                snapshot.Version,
                " ",
                snapshot.RowLabels.ToArray(),
                snapshot.ColLabels.ToArray(),
                snapshot.Cells
                    .Select(cell => new UpdateGameSetupCellDto(cell.Id, cell.Row, cell.Col, cell.Title, cell.Cost))
                    .ToArray()
            )
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.InvalidGameSetupTitle, payload.Error);
        Assert.Equal(AppMessages.ErrorCodes.InvalidGameSetupTitle, payload.Code);
    }

    [Fact]
    public async Task UpdateSetup_WhenVersionIsStale_ReturnsConflict()
    {
        await ClearGamesAsync();
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var createResponse = await adminClient.PostAsJsonAsync(
            "/api/game/setup",
            new CreateGameSetupRequestDto("Versioned draft")
        );
        var created = await createResponse.Content.ReadFromJsonAsync<GameSetupSnapshotDto>();
        Assert.NotNull(created);

        var firstSave = await adminClient.PutAsJsonAsync(
            "/api/game/setup",
            new UpdateGameSetupRequestDto(
                created.Version,
                "First save",
                created.RowLabels.ToArray(),
                created.ColLabels.ToArray(),
                created.Cells
                    .Select(cell => new UpdateGameSetupCellDto(cell.Id, cell.Row, cell.Col, cell.Title, cell.Cost))
                    .ToArray()
            )
        );
        Assert.Equal(HttpStatusCode.OK, firstSave.StatusCode);

        var staleSave = await adminClient.PutAsJsonAsync(
            "/api/game/setup",
            new UpdateGameSetupRequestDto(
                created.Version,
                "Stale save",
                created.RowLabels.ToArray(),
                created.ColLabels.ToArray(),
                created.Cells
                    .Select(cell => new UpdateGameSetupCellDto(cell.Id, cell.Row, cell.Col, "Stale", cell.Cost))
                    .ToArray()
            )
        );

        Assert.Equal(HttpStatusCode.Conflict, staleSave.StatusCode);
        var payload = await staleSave.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.GameSetupDraftVersionConflict, payload.Error);
        Assert.Equal(AppMessages.ErrorCodes.GameSetupDraftVersionConflict, payload.Code);
    }

    [Fact]
    public async Task CreateSetup_WhenParallelRequests_ProducesCreatedAndConflictWithoutServerError()
    {
        await ClearGamesAsync();
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var firstCreate = adminClient.PostAsJsonAsync(
            "/api/game/setup",
            new CreateGameSetupRequestDto("Parallel draft")
        );
        var secondCreate = adminClient.PostAsJsonAsync(
            "/api/game/setup",
            new CreateGameSetupRequestDto("Parallel draft")
        );

        var responses = await Task.WhenAll(firstCreate, secondCreate);
        var statuses = responses.Select(response => response.StatusCode).ToArray();

        Assert.All(
            statuses,
            status =>
                Assert.Contains(
                    status,
                    new[] { HttpStatusCode.Created, HttpStatusCode.Conflict }
                )
        );
        Assert.DoesNotContain(HttpStatusCode.InternalServerError, statuses);
    }

    [Fact]
    public async Task UpdateSetup_WhenAdmin_RemovesLastRowAndDeletesCells()
    {
        await ClearGamesAsync();
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var createResponse = await adminClient.PostAsJsonAsync(
            "/api/game/setup",
            new CreateGameSetupRequestDto("Resize draft")
        );
        var created = await createResponse.Content.ReadFromJsonAsync<GameSetupSnapshotDto>();
        Assert.NotNull(created);
        Assert.Equal(25, created.Cells.Count);

        var rowLabels = created.RowLabels.Take(4).ToArray();
        var cells = created.Cells
            .Where(cell => cell.Row < 4)
            .Select(cell => new UpdateGameSetupCellDto(cell.Id, cell.Row, cell.Col, cell.Title, cell.Cost))
            .ToArray();

        var updateResponse = await adminClient.PutAsJsonAsync(
            "/api/game/setup",
            new UpdateGameSetupRequestDto(
                created.Version,
                "Resize draft",
                rowLabels,
                created.ColLabels.ToArray(),
                cells
            )
        );

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var saved = await updateResponse.Content.ReadFromJsonAsync<GameSetupSnapshotDto>();
        Assert.NotNull(saved);
        Assert.Equal(4, saved.Rows);
        Assert.Equal(20, saved.Cells.Count);
        Assert.DoesNotContain(saved.Cells, cell => cell.Row == 4);
    }

    [Fact]
    public async Task GetSetup_WhenServiceThrows_ReturnsInternalServerErrorPayload()
    {
        using var adminClient = CreateAuthenticatedClient(
            [AuthRoleCodes.Admin],
            gameSetupService: new ThrowingGameSetupService()
        );

        var response = await adminClient.GetAsync("/api/game/setup");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.UnexpectedServerError, payload.Error);
        Assert.Equal(AppMessages.ErrorCodes.UnexpectedServerError, payload.Code);
        Assert.False(string.IsNullOrWhiteSpace(payload.RequestId));
    }

    private async Task ClearGamesAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.GameModifierActivations.RemoveRange(dbContext.GameModifierActivations);
        dbContext.GameEnabledModifiers.RemoveRange(dbContext.GameEnabledModifiers);
        dbContext.BoardCellMedia.RemoveRange(dbContext.BoardCellMedia);
        dbContext.BoardCells.RemoveRange(dbContext.BoardCells);
        dbContext.GameBoards.RemoveRange(dbContext.GameBoards);
        dbContext.Games.RemoveRange(dbContext.Games);
        await dbContext.SaveChangesAsync();
    }

    private HttpClient CreateAuthenticatedClient(
        string[] roles,
        IGameSetupEventsPublisher? eventsPublisher = null,
        IGameSetupService? gameSetupService = null
    )
    {
        var authenticatedFactory = _factory.WithWebHostBuilder(
            builder =>
                builder.ConfigureServices(
                    services =>
                    {
                        if (eventsPublisher is not null)
                        {
                            services.RemoveAll<IGameSetupEventsPublisher>();
                            services.AddSingleton(eventsPublisher);
                        }

                        if (gameSetupService is not null)
                        {
                            services.RemoveAll<IGameSetupService>();
                            services.AddSingleton(gameSetupService);
                        }

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

        return authenticatedFactory.CreateClient();
    }

    private sealed class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "TestAuth";

        public TestAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder
        )
            : base(options, logger, encoder)
        {
        }

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
        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            return Task.FromResult(principal);
        }
    }

    private sealed class FailingGameSetupEventsPublisher : IGameSetupEventsPublisher
    {
        public Task PublishDraftChangedAsync(CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Simulated SignalR publish failure.");
        }
    }

    private sealed class ThrowingGameSetupService : IGameSetupService
    {
        public Task<GameBoardSnapshot?> GetDraftSetupAsync(CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Simulated setup failure.");

        public Task<CreateDraftGameSetupResult> CreateDraftSetupAsync(
            string title,
            CancellationToken cancellationToken = default
        ) => throw new InvalidOperationException("Simulated setup failure.");

        public Task<UpdateDraftGameSetupResult> UpdateDraftSetupAsync(
            GameSetupDraftUpdate update,
            CancellationToken cancellationToken = default
        ) => throw new InvalidOperationException("Simulated setup failure.");

        public Task<DeleteDraftGameSetupResult> DeleteDraftSetupAsync(
            CancellationToken cancellationToken = default
        ) => throw new InvalidOperationException("Simulated setup failure.");
    }
}
