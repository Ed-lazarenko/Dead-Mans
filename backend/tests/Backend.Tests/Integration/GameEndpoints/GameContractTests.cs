using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using backend.Api.Contracts;
using backend.Application.Abstractions;
using backend.Application.Abstractions.Auth;
using backend.Application.Abstractions.Realtime;
using backend.Application.Contracts;
using backend.Application.Abstractions.Repositories;
using backend.Application.Features.GameQuestions;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.Persistence;
using backend.Infrastructure.Realtime;
using backend.Messaging;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Backend.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Backend.Tests.Integration.GameEndpoints;

public sealed class GameContractTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public GameContractTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetGame_WhenAnonymous_ReturnsJsonUnauthorizedError()
    {
        var response = await _client.GetAsync("/api/game");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());

        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.AuthenticationRequired, payload.Error);
    }

    [Fact]
    public async Task GetGame_WhenAnonymous_ReturnsSecurityHeaders()
    {
        var response = await _client.GetAsync("/api/game");

        Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
        Assert.Equal("DENY", Assert.Single(response.Headers.GetValues("X-Frame-Options")));
        Assert.Equal("no-referrer", Assert.Single(response.Headers.GetValues("Referrer-Policy")));
        Assert.Equal(
            "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'self'",
            Assert.Single(response.Headers.GetValues("Content-Security-Policy"))
        );
    }

    [Fact]
    public async Task CorsPreflight_WhenOriginAllowed_ReturnsAllowOriginHeader()
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/game");
        request.Headers.Add("Origin", "http://localhost:5180");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("http://localhost:5180", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.Contains("Origin", response.Headers.Vary.SelectMany(value => value.Split(',')));
    }

    [Fact]
    public async Task CorsPreflight_WhenOriginDisallowed_DoesNotReturnAllowOriginHeader()
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/game");
        request.Headers.Add("Origin", "https://evil.example");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task GetGame_WhenAuthenticatedButInactive_ReturnsUnauthorized()
    {
        var inactiveUserId = Guid.NewGuid();
        await SeedInactiveUserAsync(inactiveUserId);
        using var authenticatedClient = CreateAuthenticatedClient(
            [AuthRoleCodes.Viewer],
            userId: inactiveUserId
        );

        var response = await authenticatedClient.GetAsync("/api/game");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.UserMissingOrInactive, payload.Error);
    }

    [Fact]
    public async Task GetGame_WhenAuthenticated_ReturnsBoardSnapshot()
    {
        var finishedGameId = await SeedGamesAsync();
        await AssertRepositoryFallbackAsync(finishedGameId);
        using var authenticatedClient = CreateAuthenticatedClient([AuthRoleCodes.Viewer]);

        var response = await authenticatedClient.GetAsync("/api/game");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GameBoardSnapshotDto>();
        Assert.NotNull(payload);
        Assert.Equal(finishedGameId.ToString(), payload.GameId);
        Assert.Equal(GameStatusValue.Finished, payload.Status);
        Assert.True(payload.Version >= 1);
        Assert.Single(payload.Cells);
    }

    [Fact]
    public async Task Repository_WhenActiveGameHasNoBoard_FallsBackToLatestFinishedGameWithBoard()
    {
        var finishedGameId = await SeedGamesAsync();
        await AssertRepositoryFallbackAsync(finishedGameId);
    }

    [Fact]
    public async Task Repository_WhenNoBoardsExist_ReturnsNull()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var gameBoardService = scope.ServiceProvider.GetRequiredService<IGameBoardService>();

        dbContext.BoardCells.RemoveRange(dbContext.BoardCells);
        dbContext.GameBoards.RemoveRange(dbContext.GameBoards);
        dbContext.Games.RemoveRange(dbContext.Games);
        await dbContext.SaveChangesAsync();

        var snapshot = await gameBoardService.GetCurrentBoardAsync();
        Assert.Null(snapshot);
    }

    [Fact]
    public async Task OpenCell_WhenAuthenticatedButNotAdmin_ReturnsForbidden()
    {
        var cellId = await SeedSingleCellAsync();
        using var authenticatedClient = CreateAuthenticatedClient([AuthRoleCodes.Viewer]);

        var response = await authenticatedClient.PostAsync($"/api/game/cells/{cellId}/open", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OpenCell_WhenAdmin_OpensCellAndReturnsNoContent()
    {
        var cellId = await SeedSingleCellAsync();
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var response = await adminClient.PostAsync($"/api/game/cells/{cellId}/open", content: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var cell = await dbContext.BoardCells.FindAsync(cellId);
        Assert.NotNull(cell);
        Assert.Equal(BoardCellState.Open, cell!.State);

        var run = await dbContext.GameCardRuns
            .Include(x => x.Participants)
            .SingleAsync(x => x.BoardCellId == cellId);
        var activeTeamId = await dbContext.BoardCells
            .Where(x => x.Id == cellId)
            .Select(x => x.Board.Game.ActiveTeamId)
            .SingleAsync();
        Assert.Equal(GameCardRunStatusValue.AwaitingModifiers, run.Status);
        Assert.Equal(activeTeamId!.Value, run.TeamId);
        Assert.Single(run.Participants);
    }

    [Fact]
    public async Task OpenCell_WhenAdminAndNoActiveTeamSelected_ReturnsConflict()
    {
        var cellId = await SeedSingleCellAsync(selectActiveTeam: false);
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var response = await adminClient.PostAsync($"/api/game/cells/{cellId}/open", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.GameActiveTeamRequired, payload.Error);
        Assert.Equal(AppMessages.ErrorCodes.GameBoardActiveTeamRequired, payload.Code);
    }

    [Fact]
    public async Task SetActiveTeam_WhenModeratorAndConfirmedTeam_UpdatesCurrentGameSnapshot()
    {
        var cellId = await SeedSingleCellAsync(selectActiveTeam: false);
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var teamId = await dbContext.BoardCells
            .Where(cell => cell.Id == cellId)
            .SelectMany(cell => dbContext.GameTeams.Where(team => team.GameId == cell.Board.GameId))
            .Select(team => team.Id)
            .SingleAsync();
        using var moderatorClient = CreateAuthenticatedClient([AuthRoleCodes.Moderator]);

        var response = await moderatorClient.PutAsJsonAsync(
            "/api/game/active-team",
            new SetActiveGameTeamRequestDto(teamId.ToString())
        );

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var snapshotResponse = await moderatorClient.GetAsync("/api/game");
        Assert.Equal(HttpStatusCode.OK, snapshotResponse.StatusCode);
        var snapshot = await snapshotResponse.Content.ReadFromJsonAsync<GameBoardSnapshotDto>();
        Assert.NotNull(snapshot);
        Assert.Equal(teamId.ToString(), snapshot.ActiveTeamId);
    }

    [Fact]
    public async Task SetActiveTeam_WhenCardOpenedAndAwaitingModifiers_ReturnsConflictAndKeepsActiveTeam()
    {
        var cellId = await SeedSingleCellAsync();
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);
        var openResponse = await adminClient.PostAsync($"/api/game/cells/{cellId}/open", content: null);
        Assert.Equal(HttpStatusCode.NoContent, openResponse.StatusCode);
        using var moderatorClient = CreateAuthenticatedClient([AuthRoleCodes.Moderator]);

        var response = await moderatorClient.PutAsJsonAsync(
            "/api/game/active-team",
            new SetActiveGameTeamRequestDto(null)
        );

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.GameActiveTeamRoundInProgress, payload.Error);
        Assert.Equal(AppMessages.ErrorCodes.GameBoardActiveTeamRoundInProgress, payload.Code);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var activeGame = await dbContext.BoardCells
            .Where(cell => cell.Id == cellId)
            .Select(
                cell =>
                    new
                    {
                        cell.Board.GameId,
                        cell.Board.Game.ActiveTeamId
                    }
            )
            .SingleAsync();
        var persistedGame = await dbContext.Games.SingleAsync(game => game.Id == activeGame.GameId);
        Assert.Equal(activeGame.ActiveTeamId, persistedGame.ActiveTeamId);
    }

    [Fact]
    public async Task OpenCell_WhenRoundAwaitingModifiers_ReturnsConflict()
    {
        var cellId = await SeedSingleCellAsync();
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);
        var openResponse = await adminClient.PostAsync($"/api/game/cells/{cellId}/open", content: null);
        Assert.Equal(HttpStatusCode.NoContent, openResponse.StatusCode);

        var response = await adminClient.PostAsync($"/api/game/cells/{cellId}/open", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.GameCardRunAlreadyInProgress, payload.Error);
        Assert.Equal(AppMessages.ErrorCodes.GameCardRunAlreadyInProgress, payload.Code);
    }

    [Fact]
    public async Task OpenCell_WhenAdminAndCellMissing_ReturnsNotFound()
    {
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var response = await adminClient.PostAsync($"/api/game/cells/{Guid.NewGuid()}/open", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.GameCellNotFound, payload.Error);
        Assert.Equal(AppMessages.ErrorCodes.GameBoardCellNotFound, payload.Code);
    }

    [Fact]
    public async Task GetGame_WhenServiceThrows_ReturnsInternalServerErrorPayload()
    {
        using var authenticatedClient = CreateAuthenticatedClient(
            [AuthRoleCodes.Viewer],
            gameBoardService: new ThrowingGameBoardService()
        );

        var response = await authenticatedClient.GetAsync("/api/game");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.UnexpectedServerError, payload.Error);
        Assert.Equal(AppMessages.ErrorCodes.UnexpectedServerError, payload.Code);
        Assert.False(string.IsNullOrWhiteSpace(payload.RequestId));
    }

    [Fact]
    public async Task RealtimeSmoke_OpenCell_WhenAdmin_PublishesCellOpenedEvent()
    {
        var cellId = await SeedSingleCellAsync();
        var publisher = new RecordingGameBoardEventsPublisher();
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin], publisher: publisher);

        var response = await adminClient.PostAsync($"/api/game/cells/{cellId}/open", content: null);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var payload = Assert.Single(publisher.PublishedEvents);
        Assert.Equal(cellId.ToString(), payload.Cell.Id);
        Assert.Equal("open", payload.Cell.State.ToString().ToLowerInvariant());
        Assert.True(payload.Version >= 2);
    }

    [Fact]
    public async Task RealtimeSmoke_OpenCell_WhenCalledTwice_PublishesSingleEvent()
    {
        var cellId = await SeedSingleCellAsync();
        var publisher = new RecordingGameBoardEventsPublisher();
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin], publisher: publisher);

        var firstResponse = await adminClient.PostAsync($"/api/game/cells/{cellId}/open", content: null);
        var secondResponse = await adminClient.PostAsync($"/api/game/cells/{cellId}/open", content: null);

        Assert.Equal(HttpStatusCode.NoContent, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);

        var payload = Assert.Single(publisher.PublishedEvents);
        Assert.Equal(cellId.ToString(), payload.Cell.Id);
        Assert.Equal("open", payload.Cell.State.ToString().ToLowerInvariant());
    }

    [Fact]
    public async Task GetModifierCatalog_WhenAuthenticated_ReturnsSeededCatalog()
    {
        await EnsureModifierDefinitionsSeededAsync();
        using var authenticatedClient = CreateAuthenticatedClient([AuthRoleCodes.Viewer]);

        var response = await authenticatedClient.GetAsync("/api/game/modifiers/catalog");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<IReadOnlyList<GameModifierDefinitionDto>>();
        Assert.NotNull(payload);
        Assert.Contains(
            payload,
            modifier => modifier.Id == ModifierDefinitionSeedIds.Chirik.ToString()
        );
    }

    [Fact]
    public async Task CreateModifier_WhenAdminWithoutCode_ReturnsCreatedModifierWithId()
    {
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var response = await adminClient.PostAsJsonAsync(
            "/api/game/modifiers",
            new CreateGameModifierRequestDto(
                "Fresh modifier",
                "Created without a manual code.",
                GameModifierMechanicTypes.RuleOnly,
                GameModifierCategories.Round,
                false,
                5,
                new GameModifierActivationLimitDto(1),
                new GameModifierEffectDto(
                    GameModifierMechanicTypes.RuleOnly,
                    [],
                    null,
                    null,
                    null,
                    [],
                    [],
                    null,
                    null,
                    null
                ),
                [],
                1,
                "non_scoring",
                null,
                null
            )
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GameModifierDefinitionDto>();
        Assert.NotNull(payload);
        Assert.True(Guid.TryParse(payload.Id, out _));
        Assert.Equal("Fresh modifier", payload.Name);
    }

    [Fact]
    public async Task CreateModifier_WhenEffectDoesNotMatchMechanic_ReturnsBadRequest()
    {
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var request = CreateRuleOnlyModifierRequest("Broken modifier") with
        {
            MechanicType = GameModifierMechanicTypes.RestrictionWithReward,
            Effect = new GameModifierEffectDto(
                GameModifierMechanicTypes.RestrictionWithReward,
                [],
                null,
                null,
                null,
                [],
                [],
                null,
                null,
                null
            )
        };

        var response = await adminClient.PostAsJsonAsync("/api/game/modifiers", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateModifier_WhenEffectIsMissing_ReturnsBadRequest()
    {
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var response = await adminClient.PostAsJsonAsync(
            "/api/game/modifiers",
            new
            {
                name = "Broken modifier",
                description = "Missing effect should not throw.",
                mechanicType = GameModifierMechanicTypes.RuleOnly,
                activationCost = 5,
                activationLimit = new { count = 1 }
            }
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateModifier_WhenActivationLimitCountIsInvalid_ReturnsBadRequest()
    {
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var request = CreateRuleOnlyModifierRequest("Invalid-limit modifier") with
        {
            ActivationLimit = new GameModifierActivationLimitDto(0)
        };

        var response = await adminClient.PostAsJsonAsync("/api/game/modifiers", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.ErrorCodes.GameModifierInvalidRequest, payload.Code);
    }

    [Fact]
    public async Task UpdateModifier_WhenAdminSetsConflict_ActivationHonorsUpdatedConflict()
    {
        await EnsureModifierDefinitionsSeededAsync();
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);
        var updateResponse = await adminClient.PutAsJsonAsync(
            $"/api/game/modifiers/{ModifierDefinitionSeedIds.Chirik}",
            CreateRuleOnlyModifierRequest("Чирик") with
            {
                ConflictingModifierIds = [ModifierDefinitionSeedIds.Feyerverk.ToString()]
            }
        );

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<GameModifierDefinitionDto>();
        Assert.NotNull(updated);
        Assert.Contains(ModifierDefinitionSeedIds.Feyerverk.ToString(), updated.ConflictingModifierIds);

        await SeedActiveGameWithEnabledModifiersAsync(["chirik", "feyerverk"], ["feyerverk"]);
        using var moderatorClient = CreateAuthenticatedClient([AuthRoleCodes.Moderator]);

        var activateResponse = await moderatorClient.PostAsync(
            $"/api/game/modifiers/{ModifierDefinitionSeedIds.Chirik}/activate",
            content: null
        );

        Assert.Equal(HttpStatusCode.Conflict, activateResponse.StatusCode);
        var payload = await activateResponse.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.ErrorCodes.GameModifierConflictActive, payload.Code);
    }

    [Fact]
    public async Task ActivateModifier_WhenModeratorAndEnabledForActiveGame_ReturnsNoContent()
    {
        await EnsureModifierDefinitionsSeededAsync();
        await SeedActiveGameWithEnabledModifiersAsync(["chirik"]);
        var userId = Guid.NewGuid();
        await SeedQuizPointsAsync(userId, 100);
        using var moderatorClient = CreateAuthenticatedClient([AuthRoleCodes.Moderator], userId);

        var response = await moderatorClient.PostAsync(
            $"/api/game/modifiers/{ModifierDefinitionSeedIds.Chirik}/activate",
            content: null
        );

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(
            1,
            await dbContext.GameActiveModifiers.CountAsync(
                x =>
                    x.ModifierId == ModifierDefinitionSeedIds.Chirik
                    && x.ActivatedByUserId == userId
                    && x.ActivationCostSnapshot > 0
                )
        );
    }

    [Fact]
    public async Task GetAdminModifierPlayers_WhenAdmin_ReturnsPlayersWithBalances()
    {
        await EnsureModifierDefinitionsSeededAsync();
        await SeedActiveGameWithEnabledModifiersAsync(["chirik"]);
        var userId = Guid.NewGuid();
        await SeedQuizPointsAsync(userId, 25);
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var response = await adminClient.GetAsync("/api/game/modifiers/admin/players");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<IReadOnlyList<GameModifierAdminPlayerDto>>();
        Assert.NotNull(payload);
        var player = Assert.Single(payload, item => item.UserId == userId.ToString());
        Assert.Equal(25, player.AvailableQuizPoints);
        Assert.Equal(25, player.EarnedQuizPoints);
        Assert.Equal(0, player.SpentQuizPoints);
    }

    [Fact]
    public async Task AdminActivateModifier_WhenAdminTargetsPlayer_UsesSameRulesAndChargesPlayer()
    {
        await EnsureModifierDefinitionsSeededAsync();
        await SeedActiveGameWithEnabledModifiersAsync(["chirik"]);
        var userId = Guid.NewGuid();
        await SeedQuizPointsAsync(userId, 25);
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var response = await adminClient.PostAsJsonAsync(
            "/api/game/modifiers/admin/activate",
            new AdminActivateGameModifierRequestDto(
                ModifierDefinitionSeedIds.Chirik.ToString(),
                userId.ToString()
            )
        );

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var state = await adminClient.GetFromJsonAsync<GameModifierStateDto>(
            $"/api/game/modifiers/admin/state/{userId}"
        );
        Assert.NotNull(state);
        Assert.Equal(22, state.AvailableQuizPoints);
        Assert.Single(state.ActiveModifiers);
        Assert.Equal(userId.ToString(), state.ActiveModifiers[0].ActivatedByUserId);
        Assert.True(Guid.TryParse(state.ActiveModifiers[0].ActivationId, out _));
    }

    [Fact]
    public async Task CancelModifierActivation_WhenAdminAndUnused_RefundsPointsRemovesHistoryAndPublishesRealtime()
    {
        await EnsureModifierDefinitionsSeededAsync();
        await SeedActiveGameWithEnabledModifiersAsync(["chirik"]);
        var userId = Guid.NewGuid();
        await SeedQuizPointsAsync(userId, 25);
        var publisher = new RecordingGameBoardEventsPublisher();
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin], publisher: publisher);
        using var playerClient = CreateAuthenticatedClient([AuthRoleCodes.Viewer], userId);

        var activateResponse = await adminClient.PostAsJsonAsync(
            "/api/game/modifiers/admin/activate",
            new AdminActivateGameModifierRequestDto(
                ModifierDefinitionSeedIds.Chirik.ToString(),
                userId.ToString()
            )
        );
        Assert.Equal(HttpStatusCode.NoContent, activateResponse.StatusCode);

        var stateBeforeCancel = await adminClient.GetFromJsonAsync<GameModifierStateDto>(
            $"/api/game/modifiers/admin/state/{userId}"
        );
        Assert.NotNull(stateBeforeCancel);
        var activation = Assert.Single(stateBeforeCancel.ActiveModifiers);
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var persistedActivationId = await dbContext.GameActiveModifiers
            .Where(x => x.ActivatedByUserId == userId && x.ModifierId == ModifierDefinitionSeedIds.Chirik)
            .Select(x => x.Id)
            .SingleAsync();
        Assert.Equal(persistedActivationId.ToString(), activation.ActivationId);

        var cancelResponse = await adminClient.DeleteAsync(
            $"/api/game/modifiers/admin/activations/{persistedActivationId}"
        );

        Assert.Equal(HttpStatusCode.NoContent, cancelResponse.StatusCode);

        var stateAfterCancel = await adminClient.GetFromJsonAsync<GameModifierStateDto>(
            $"/api/game/modifiers/admin/state/{userId}"
        );
        Assert.NotNull(stateAfterCancel);
        Assert.Empty(stateAfterCancel.ActiveModifiers);
        Assert.Equal(25, stateAfterCancel.AvailableQuizPoints);

        var cancelledEvent = Assert.Single(publisher.PublishedModifierCancelledEvents);
        Assert.Equal(activation.ActivationId, cancelledEvent.ActivationId.ToString());
        var notificationEvent = Assert.Single(publisher.PublishedUserNotificationEvents);
        Assert.Equal(userId, notificationEvent.UserId);
        Assert.Equal(GameNotificationTypes.ModifierCancelled, notificationEvent.Notification.Type);
        Assert.Equal("Чирик", notificationEvent.Notification.ModifierName);
        Assert.Equal(activation.ActivationCost, notificationEvent.Notification.QuizPointsDelta);

        var notificationsResponse = await playerClient.GetAsync("/api/game/notifications");
        Assert.Equal(HttpStatusCode.OK, notificationsResponse.StatusCode);
        var notifications = await notificationsResponse.Content.ReadFromJsonAsync<IReadOnlyList<GameUserNotificationDto>>();
        Assert.NotNull(notifications);
        var notification = Assert.Single(notifications);
        Assert.Equal(GameNotificationTypes.ModifierCancelled, notification.Type);
        Assert.Equal("Чирик", notification.ModifierName);
        Assert.Equal(activation.ActivationCost, notification.QuizPointsDelta);

        var markReadResponse = await playerClient.PostAsync("/api/game/notifications/read", content: null);
        Assert.Equal(HttpStatusCode.NoContent, markReadResponse.StatusCode);

        var notificationsAfterRead = await playerClient.GetFromJsonAsync<IReadOnlyList<GameUserNotificationDto>>(
            "/api/game/notifications"
        );
        Assert.NotNull(notificationsAfterRead);
        Assert.Empty(notificationsAfterRead);

        var activeGameId = await dbContext.Games
            .Where(x => x.Status == GameStatusValue.Active && !x.IsDeleted)
            .Select(x => x.Id)
            .SingleAsync();
        var historyResponse = await adminClient.GetAsync($"/api/game/history/games/{activeGameId}");
        Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);
        var history = await historyResponse.Content.ReadFromJsonAsync<GameHistoryGameDetailsDto>();
        Assert.NotNull(history);
        Assert.Empty(history.MainGame.ModifierActivations);
    }

    [Fact]
    public async Task CancelModifierActivation_WhenAlreadyAppliedInRound_ReturnsConflict()
    {
        var seeded = await SeedModifierHistoryGameAsync();
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var response = await adminClient.DeleteAsync(
            $"/api/game/modifiers/admin/activations/{seeded.ActivationId}"
        );

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.ErrorCodes.GameModifierAlreadyAppliedInRound, payload.Code);
    }

    [Fact]
    public async Task ActivateModifier_WhenQuizPointsInsufficient_ReturnsConflictCode()
    {
        await EnsureModifierDefinitionsSeededAsync();
        await SeedActiveGameWithEnabledModifiersAsync(["chirik"]);
        var userId = Guid.NewGuid();
        await SeedQuizPointsAsync(userId, 0);
        using var authenticatedClient = CreateAuthenticatedClient([AuthRoleCodes.Viewer], userId);

        var response = await authenticatedClient.PostAsync(
            $"/api/game/modifiers/{ModifierDefinitionSeedIds.Chirik}/activate",
            content: null
        );

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.ErrorCodes.GameModifierInsufficientQuizPoints, payload.Code);
    }

    [Fact]
    public async Task ActivateModifier_WhenConflictAlreadyActive_ReturnsConflictCode()
    {
        await EnsureModifierDefinitionsSeededAsync();
        await SeedActiveGameWithEnabledModifiersAsync(["prokaznik", "mentorbait"], ["prokaznik"]);
        using var moderatorClient = CreateAuthenticatedClient([AuthRoleCodes.Moderator]);

        var response = await moderatorClient.PostAsync(
            $"/api/game/modifiers/{ModifierDefinitionSeedIds.Mentorbait}/activate",
            content: null
        );

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.ErrorCodes.GameModifierConflictActive, payload.Code);
    }

    [Fact]
    public async Task ActivateModifier_WhenLimitReached_ReturnsConflictCode()
    {
        await EnsureModifierDefinitionsSeededAsync();
        await SeedActiveGameWithEnabledModifiersAsync(["feyerverk"], ["feyerverk"]);
        using var moderatorClient = CreateAuthenticatedClient([AuthRoleCodes.Moderator]);

        var response = await moderatorClient.PostAsync(
            $"/api/game/modifiers/{ModifierDefinitionSeedIds.Feyerverk}/activate",
            content: null
        );

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.ErrorCodes.GameModifierLimitReached, payload.Code);
    }

    [Fact]
    public async Task GetModifierState_WhenLimitReached_ReturnsBlockedAvailability()
    {
        await EnsureModifierDefinitionsSeededAsync();
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var definition = await dbContext.ModifierDefinitions.SingleAsync(
                x => x.Id == ModifierDefinitionSeedIds.Zhazhda
            );
            definition.MetadataJson =
                "{\"effect\":{\"mechanicType\":\"rule_only\",\"traits\":[],\"durationSeconds\":null,\"ruleText\":null,\"scoreImpact\":null,\"conditions\":[],\"resolutionInputs\":[],\"killEffect\":null,\"multiplierEffect\":null,\"mentorEffect\":null},\"activationLimit\":{\"count\":null}}";
            definition.DefaultLimitPerGame = 2;
            await dbContext.SaveChangesAsync();
        }

        await SeedActiveGameWithEnabledModifiersAsync(["zhazhda"], ["zhazhda", "zhazhda"]);
        var userId = Guid.NewGuid();
        await SeedQuizPointsAsync(userId, 100);
        using var viewerClient = CreateAuthenticatedClient([AuthRoleCodes.Viewer], userId);

        var state = await viewerClient.GetFromJsonAsync<GameModifierStateDto>(
            "/api/game/modifiers/state"
        );

        Assert.NotNull(state);
        var availability = Assert.Single(
            state.AvailableModifiers,
            x => x.Modifier.Id == ModifierDefinitionSeedIds.Zhazhda.ToString()
        );
        Assert.False(availability.CanActivate);
        Assert.Equal("limit_reached", availability.BlockedReason);
        Assert.Equal(2, availability.ActivationsCount);
        Assert.Equal(2, availability.Limit);
        Assert.Equal(2, availability.Modifier.ActivationLimit.Count);
    }

    [Fact]
    public async Task ActivateModifier_WhenRoundInProgress_ReturnsOrderingClosedConflict()
    {
        await EnsureModifierDefinitionsSeededAsync();
        var cellId = await SeedSingleCellAsync();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var row = await dbContext.BoardCells
            .Where(cell => cell.Id == cellId)
            .Select(
                cell =>
                    new
                    {
                        cell.Board.GameId,
                        cell.Board.Game.ActiveTeamId,
                        cell.RowIndex,
                        cell.ColIndex,
                        cell.Title,
                        cell.Cost
                    }
            )
            .SingleAsync();
        Assert.NotNull(row.ActiveTeamId);

        var now = DateTime.UtcNow;
        var cell = await dbContext.BoardCells.SingleAsync(x => x.Id == cellId);
        cell.State = BoardCellState.Open;
        dbContext.GameModifierSelections.Add(
            new GameModifierSelection
            {
                GameId = row.GameId,
                ModifierId = ModifierDefinitionSeedIds.Chirik,
                EnabledAtUtc = now
            }
        );
        dbContext.GameCardRuns.Add(
            new GameCardRun
            {
                Id = Guid.NewGuid(),
                GameId = row.GameId,
                BoardCellId = cellId,
                TeamId = row.ActiveTeamId.Value,
                Status = GameCardRunStatusValue.InProgress,
                StartedAtUtc = now,
                BaseScore = row.Cost,
                TeamSlotIndexSnapshot = 1,
                CellRowIndex = row.RowIndex,
                CellColIndex = row.ColIndex,
                CellTitleSnapshot = row.Title,
                CellCostSnapshot = row.Cost,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        );
        await dbContext.SaveChangesAsync();

        using var moderatorClient = CreateAuthenticatedClient([AuthRoleCodes.Moderator]);
        var response = await moderatorClient.PostAsync(
            $"/api/game/modifiers/{ModifierDefinitionSeedIds.Chirik}/activate",
            content: null
        );

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.ErrorCodes.GameModifierOrderingClosed, payload.Code);
        Assert.Equal(0, await dbContext.GameActiveModifiers.CountAsync());
    }

    [Fact]
    public async Task GetQuestionCatalog_WhenAdmin_ReturnsCatalog()
    {
        await SeedQuestionCatalogWithQuestionsAsync(
            [
                new SeedQuestionItem("sample-q-1001", "lore", "Как называется демо вопрос?", "Демо", 1),
                new SeedQuestionItem("sample-q-1002", "locations", "Сколько точек эвакуации на демо карте?", "2", 2)
            ]
        );
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var response = await adminClient.GetAsync("/api/game/questions/catalog");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<IReadOnlyList<GameQuestionCatalogItemDto>>();
        Assert.NotNull(payload);
        Assert.Contains(payload, question => question.QuestionCode == "sample-q-1001");
        Assert.Contains(payload, question => question.QuestionCode == "sample-q-1002");
    }

    [Fact]
    public async Task GetQuestionCategories_WhenAdmin_ReturnsDistinctCategoriesWithQuestionCounts()
    {
        await SeedQuestionCatalogWithQuestionsAsync(
            [
                new SeedQuestionItem("sample-q-1001", "lore", "Как называется демо вопрос?", "Демо", 1),
                new SeedQuestionItem("sample-q-1002", "lore", "Второй вопрос категории?", "Да", 2),
                new SeedQuestionItem("sample-q-1003", "locations", "Сколько точек эвакуации?", "2", 3)
            ]
        );
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var response = await adminClient.GetAsync("/api/game/questions/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload =
            await response.Content.ReadFromJsonAsync<IReadOnlyList<GameQuestionCategoryItemDto>>();
        Assert.NotNull(payload);
        Assert.Contains(payload, category => category.Name == "lore" && category.QuestionCount == 2);
        Assert.Contains(
            payload,
            category => category.Name == "locations" && category.QuestionCount == 1
        );
        Assert.Contains(
            payload,
            category =>
                category.Name == QuestionCatalogDefaults.UncategorizedCategoryName
                && category.IsProtected
        );
        Assert.DoesNotContain(payload, category => category.Name == "lore" && category.IsProtected);
    }

    [Fact]
    public async Task CreateQuestionCategory_WhenAdmin_CreatesCategory()
    {
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);
        var categoryName = $"История-{Guid.NewGuid():N}";

        var response = await adminClient.PostAsJsonAsync(
            "/api/game/questions/categories",
            new CreateGameQuestionCategoryRequestDto(categoryName)
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GameQuestionCategoryItemDto>();
        Assert.NotNull(payload);
        Assert.Equal(categoryName, payload.Name);
        Assert.Equal(0, payload.QuestionCount);
    }

    [Fact]
    public async Task DeleteQuestionCategory_WhenEmpty_DeletesCategory()
    {
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var createResponse = await adminClient.PostAsJsonAsync(
            "/api/game/questions/categories",
            new CreateGameQuestionCategoryRequestDto("Удаляемая категория")
        );
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<GameQuestionCategoryItemDto>();
        Assert.NotNull(created);

        var deleteResponse = await adminClient.DeleteAsync(
            $"/api/game/questions/categories/{created.Id}"
        );

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var categoriesResponse = await adminClient.GetAsync("/api/game/questions/categories");
        Assert.Equal(HttpStatusCode.OK, categoriesResponse.StatusCode);
        var categories =
            await categoriesResponse.Content.ReadFromJsonAsync<IReadOnlyList<GameQuestionCategoryItemDto>>();
        Assert.NotNull(categories);
        Assert.DoesNotContain(categories, category => category.Id == created.Id);
    }

    [Fact]
    public async Task UpdateQuestionCategory_WhenAdmin_RenamesCategory()
    {
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var createResponse = await adminClient.PostAsJsonAsync(
            "/api/game/questions/categories",
            new CreateGameQuestionCategoryRequestDto("Старая категория")
        );
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<GameQuestionCategoryItemDto>();
        Assert.NotNull(created);

        var updateResponse = await adminClient.PutAsJsonAsync(
            $"/api/game/questions/categories/{created.Id}",
            new CreateGameQuestionCategoryRequestDto("Новая категория")
        );

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<GameQuestionCategoryItemDto>();
        Assert.NotNull(updated);
        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("Новая категория", updated.Name);
    }

    [Fact]
    public async Task UpdateQuestionCategory_WhenFallbackCategory_ReturnsProtectedConflict()
    {
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var categoriesResponse = await adminClient.GetAsync("/api/game/questions/categories");
        var categories =
            await categoriesResponse.Content.ReadFromJsonAsync<IReadOnlyList<GameQuestionCategoryItemDto>>();
        Assert.NotNull(categories);
        var fallback = categories.Single(category => category.Name == QuestionCatalogDefaults.UncategorizedCategoryName);

        var updateResponse = await adminClient.PutAsJsonAsync(
            $"/api/game/questions/categories/{fallback.Id}",
            new CreateGameQuestionCategoryRequestDto("Переименованная системная категория")
        );

        Assert.Equal(HttpStatusCode.Conflict, updateResponse.StatusCode);
        var payload = await updateResponse.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.ErrorCodes.GameQuestionCategoryProtected, payload.Code);
    }

    [Fact]
    public async Task DeleteQuestionCategory_WhenFallbackCategory_ReturnsProtectedConflict()
    {
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var categoriesResponse = await adminClient.GetAsync("/api/game/questions/categories");
        var categories =
            await categoriesResponse.Content.ReadFromJsonAsync<IReadOnlyList<GameQuestionCategoryItemDto>>();
        Assert.NotNull(categories);
        var fallback = categories.Single(category => category.Name == QuestionCatalogDefaults.UncategorizedCategoryName);

        var deleteResponse = await adminClient.DeleteAsync(
            $"/api/game/questions/categories/{fallback.Id}"
        );

        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
        var payload = await deleteResponse.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.ErrorCodes.GameQuestionCategoryProtected, payload.Code);
    }

    [Fact]
    public async Task DownloadQuestionImportTemplate_WhenAdmin_ReturnsCurrentCategoriesAndCommentedExample()
    {
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);
        var categoryName = $"История-{Guid.NewGuid():N}";
        await adminClient.PostAsJsonAsync(
            "/api/game/questions/categories",
            new CreateGameQuestionCategoryRequestDto(categoryName)
        );

        var response = await adminClient.GetAsync("/api/game/questions/import-template");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("// Required fields for each question: text, answer, reward.", content);
        Assert.Contains("// Available categories:", content);
        Assert.Contains("(БЕЗ КАТЕГОРИИ)", content);
        Assert.Contains($"({categoryName})", content);
        Assert.Contains("// Example:", content);
        Assert.Contains("\"questions\": [", content);
    }

    [Fact]
    public async Task DownloadQuestionImportTemplate_WhenRussianLocaleRequested_ReturnsRussianComments()
    {
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var response = await adminClient.GetAsync("/api/game/questions/import-template?locale=ru");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("// Шаблон JSONC для массового импорта вопросов.", content);
        Assert.Contains("// Обязательные поля у вопроса: text, answer, reward.", content);
        Assert.Contains(QuestionCatalogDefaults.UncategorizedCategoryName, content);
    }

    [Fact]
    public async Task ImportQuestions_WhenOptionalFieldsAreMissing_UsesFallbackCategoryAndDefaults()
    {
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var jsonc = $$"""
        {
          "questions": [
            {
              "text": "Импортированный вопрос?",
              "answer": "Да",
              "reward": 100,
              "externalCode": "import-q-1001"
            },
            {
              "text": "Вопрос с плохой категорией?",
              "answer": "Тоже да",
              "reward": 50,
              "categoryId": "not-a-guid",
              "externalCode": "import-q-1002"
            },
            {
              "text": "Вопрос без ответа",
              "reward": 10,
              "externalCode": "import-q-1003"
            }
          ]
        }
        """;

        var importResponse = await adminClient.PostAsync(
            "/api/game/questions/import",
            CreateJsonImportContent(jsonc, "questions.jsonc")
        );

        Assert.Equal(HttpStatusCode.OK, importResponse.StatusCode);
        var payload = await importResponse.Content.ReadFromJsonAsync<ImportGameQuestionsResultDto>();
        Assert.NotNull(payload);
        Assert.Equal(2, payload.ImportedCount);
        Assert.Single(payload.SkippedQuestions);
        Assert.Equal(3, payload.SkippedQuestions[0].RowNumber);
        Assert.Equal("Вопрос без ответа", payload.SkippedQuestions[0].QuestionText);
        Assert.Equal(
            AppMessages.ErrorCodes.GameQuestionImportInvalidFields,
            payload.SkippedQuestions[0].ReasonCode
        );
        Assert.Equal(
            "Missing or invalid required fields. Each question must include text, answer, and a non-negative reward.",
            payload.SkippedQuestions[0].Reason
        );
        var skippedSourceQuestion = Assert.IsType<ImportGameQuestionSourceDto>(
            payload.SkippedQuestions[0].SourceQuestion
        );
        Assert.Equal("Вопрос без ответа", skippedSourceQuestion.Text);
        Assert.Null(skippedSourceQuestion.Answer);
        Assert.Equal("import-q-1003", skippedSourceQuestion.ExternalCode);

        var catalogResponse = await adminClient.GetAsync("/api/game/questions/catalog");
        var catalog = await catalogResponse.Content.ReadFromJsonAsync<IReadOnlyList<GameQuestionCatalogItemDto>>();
        Assert.NotNull(catalog);
        var imported = Assert.Single(catalog, question => question.QuestionCode == "import-q-1001");
        Assert.Equal(QuestionCatalogDefaults.UncategorizedCategoryName, imported.CategoryName);
        Assert.False(imported.IsEnabled);
        Assert.Equal(0, imported.Priority);
        var importedWithBadCategory = Assert.Single(catalog, question => question.QuestionCode == "import-q-1002");
        Assert.Equal(QuestionCatalogDefaults.UncategorizedCategoryName, importedWithBadCategory.CategoryName);
        Assert.DoesNotContain(catalog, question => question.QuestionCode == "import-q-1003");
    }

    [Fact]
    public async Task ImportQuestions_WhenPayloadIsInvalidJson_ReturnsBadRequest()
    {
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);
        const string invalidJson = "{ \"questions\": [ { \"text\": \"Broken\" ";

        var response = await adminClient.PostAsync(
            "/api/game/questions/import",
            CreateJsonImportContent(invalidJson, "questions.jsonc")
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.ErrorCodes.GameQuestionInvalidRequest, payload.Code);
        Assert.Equal("The import file is not valid JSON/JSONC.", payload.Error);
    }

    [Fact]
    public async Task ImportQuestions_WhenFileExceedsLimit_ReturnsBadRequest()
    {
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);
        var oversizedJson = new string(' ', (int)GameQuestionImportLimits.MaxUploadBytes + 1);

        var response = await adminClient.PostAsync(
            "/api/game/questions/import",
            CreateJsonImportContent(oversizedJson, "questions.jsonc")
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
    }

    [Fact]
    public async Task DeleteQuestionCategory_WhenContainsQuestions_ReturnsConflictCode()
    {
        await SeedQuestionCatalogWithQuestionsAsync(
            [new SeedQuestionItem("keep-q-1001", "lore", "Удалять категорию нельзя?", "Да", 1)]
        );
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var categoriesResponse = await adminClient.GetAsync("/api/game/questions/categories");
        Assert.Equal(HttpStatusCode.OK, categoriesResponse.StatusCode);
        var categories =
            await categoriesResponse.Content.ReadFromJsonAsync<IReadOnlyList<GameQuestionCategoryItemDto>>();
        Assert.NotNull(categories);
        var loreCategoryId = categories.Single(category => category.Name == "lore").Id;

        var deleteResponse = await adminClient.DeleteAsync(
            $"/api/game/questions/categories/{loreCategoryId}"
        );

        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
        var payload = await deleteResponse.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.ErrorCodes.GameQuestionCategoryNotEmpty, payload.Code);
    }

    [Fact]
    public async Task SetQuestionCategoryEnabled_WhenCategoryMissing_ReturnsNotFoundCode()
    {
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var response = await adminClient.PatchAsJsonAsync(
            $"/api/game/questions/categories/{Guid.NewGuid()}/enabled",
            new SetGameQuestionCategoryEnabledRequestDto(false)
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.ErrorCodes.GameQuestionCategoryNotFound, payload.Code);
    }

    [Fact]
    public async Task SetQuestionCategoryEnabled_WhenCategoryExists_UpdatesMatchingQuestions()
    {
        await SeedQuestionCatalogWithQuestionsAsync(
            [
                new SeedQuestionItem("sample-q-1001", "lore", "Как называется демо вопрос?", "Демо", 1),
                new SeedQuestionItem("sample-q-1002", "lore", "Второй вопрос категории?", "Да", 2),
                new SeedQuestionItem("sample-q-1003", "locations", "Сколько точек эвакуации?", "2", 3)
            ]
        );
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var categoriesResponse = await adminClient.GetAsync("/api/game/questions/categories");
        var categories =
            await categoriesResponse.Content.ReadFromJsonAsync<IReadOnlyList<GameQuestionCategoryItemDto>>();
        Assert.NotNull(categories);
        var loreCategoryId = categories.Single(category => category.Name == "lore").Id;

        var updateResponse = await adminClient.PatchAsJsonAsync(
            $"/api/game/questions/categories/{loreCategoryId}/enabled",
            new SetGameQuestionCategoryEnabledRequestDto(false)
        );

        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var catalogResponse = await adminClient.GetAsync("/api/game/questions/catalog");
        Assert.Equal(HttpStatusCode.OK, catalogResponse.StatusCode);
        var catalog =
            await catalogResponse.Content.ReadFromJsonAsync<IReadOnlyList<GameQuestionCatalogItemDto>>();
        Assert.NotNull(catalog);
        Assert.All(
            catalog.Where(question => question.CategoryName == "lore"),
            question => Assert.False(question.IsEnabled)
        );
        Assert.All(
            catalog.Where(question => question.CategoryName == "locations"),
            question => Assert.True(question.IsEnabled)
        );
    }

    [Fact]
    public async Task DeleteQuestion_WhenAdmin_SoftDeletesAndHidesFromCatalog()
    {
        await SeedQuestionCatalogWithQuestionsAsync(
            [new SeedQuestionItem("delete-q-1001", "lore", "Вопрос для удаления?", "Да", 1)]
        );
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var catalogResponse = await adminClient.GetAsync("/api/game/questions/catalog");
        Assert.Equal(HttpStatusCode.OK, catalogResponse.StatusCode);
        var catalog = await catalogResponse.Content.ReadFromJsonAsync<IReadOnlyList<GameQuestionCatalogItemDto>>();
        var question = Assert.Single(catalog!, x => x.QuestionCode == "delete-q-1001");

        var deleteResponse = await adminClient.DeleteAsync($"/api/game/questions/{question.QuestionId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var afterDeleteResponse = await adminClient.GetAsync("/api/game/questions/catalog");
        Assert.Equal(HttpStatusCode.OK, afterDeleteResponse.StatusCode);
        var afterDeleteCatalog =
            await afterDeleteResponse.Content.ReadFromJsonAsync<IReadOnlyList<GameQuestionCatalogItemDto>>();
        Assert.DoesNotContain(afterDeleteCatalog!, x => x.QuestionCode == "delete-q-1001");
    }

    [Fact]
    public async Task AskNextQuestion_WhenOnlySingleQuestionAvailable_SecondCallReturnsNotFound()
    {
        await SeedActiveGameForQuestionsAsync();
        await SeedQuestionCatalogWithQuestionsAsync(
            [new SeedQuestionItem("single-q-0001", "lore", "Одиночный вопрос?", "Да", 2)]
        );
        using var moderatorClient = CreateAuthenticatedClient([AuthRoleCodes.Moderator]);

        var firstResponse = await moderatorClient.PostAsync("/api/game/questions/ask-next", content: null);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        var secondResponse = await moderatorClient.PostAsync("/api/game/questions/ask-next", content: null);
        Assert.Equal(HttpStatusCode.NotFound, secondResponse.StatusCode);
        var payload = await secondResponse.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.ErrorCodes.GameQuestionNoAvailableQuestions, payload.Code);
    }

    [Fact]
    public async Task AskNextQuestion_WhenLeastUsedQuestionsExist_PicksHighestPriorityAmongThem()
    {
        await SeedActiveGameForQuestionsAsync();
        await SeedQuestionCatalogWithQuestionsAsync(
            [
                new SeedQuestionItem("priority-q-0001", "lore", "Более использованный вопрос?", "Да", 1, -100),
                new SeedQuestionItem("priority-q-0002", "lore", "Высокий приоритет?", "Да", 1, 10),
                new SeedQuestionItem("priority-q-0003", "lore", "Низкий приоритет?", "Да", 1, 1)
            ]
        );

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var moreUsedQuestion = await dbContext.QuestionDefinitions.SingleAsync(
                question => question.ExternalCode == "priority-q-0001"
            );
            moreUsedQuestion.AskedTotalCount = 3;
            moreUsedQuestion.LastAskedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
        }

        using var moderatorClient = CreateAuthenticatedClient([AuthRoleCodes.Moderator]);

        var response = await moderatorClient.PostAsync("/api/game/questions/ask-next", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var asked = await response.Content.ReadFromJsonAsync<AskedGameQuestionDto>();
        Assert.NotNull(asked);
        Assert.Equal("priority-q-0002", asked.QuestionCode);
    }

    [Fact]
    public async Task AnswerQuestionRound_WhenAnswerCorrect_ReturnsAnsweredCorrectWithPoints()
    {
        await SeedActiveGameForQuestionsAsync();
        await SeedQuestionCatalogWithQuestionsAsync(
            [new SeedQuestionItem("answer-q-0001", "stats", "Сколько будет 1+1?", "2", 3)]
        );
        using var moderatorClient = CreateAuthenticatedClient([AuthRoleCodes.Moderator]);

        var askResponse = await moderatorClient.PostAsync("/api/game/questions/ask-next", content: null);
        Assert.Equal(HttpStatusCode.OK, askResponse.StatusCode);
        var asked = await askResponse.Content.ReadFromJsonAsync<AskedGameQuestionDto>();
        Assert.NotNull(asked);

        var answerResponse = await moderatorClient.PostAsJsonAsync(
            $"/api/game/questions/rounds/{asked.RoundId}/answer",
            new AnswerGameQuestionRequestDto("2", "Integration Tester", null)
        );

        Assert.Equal(HttpStatusCode.OK, answerResponse.StatusCode);
        var answered = await answerResponse.Content.ReadFromJsonAsync<GameQuestionRoundSummaryDto>();
        Assert.NotNull(answered);
        Assert.Equal("answered_correct", answered.Status);
        Assert.True(answered.IsCorrect);
        Assert.Equal(3, answered.AwardedPoints);
        Assert.Equal("Integration Tester", answered.AnsweredByDisplayName);
    }

    private async Task AssertRepositoryFallbackAsync(Guid finishedGameId)
    {
        using var scope = _factory.Services.CreateScope();
        var gameBoardService = scope.ServiceProvider.GetRequiredService<IGameBoardService>();

        var snapshot = await gameBoardService.GetCurrentBoardAsync();

        Assert.NotNull(snapshot);
        Assert.Equal(finishedGameId.ToString(), snapshot.GameId);
    }

    private async Task<Guid> SeedGamesAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.GameActiveModifiers.RemoveRange(dbContext.GameActiveModifiers);
        dbContext.GameModifierSelections.RemoveRange(dbContext.GameModifierSelections);
        dbContext.BoardCells.RemoveRange(dbContext.BoardCells);
        dbContext.GameBoards.RemoveRange(dbContext.GameBoards);
        dbContext.Games.RemoveRange(dbContext.Games);
        await dbContext.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var finishedGameId = Guid.NewGuid();
        var boardId = Guid.NewGuid();

        dbContext.Games.AddRange(
            new Game
            {
                Id = Guid.NewGuid(),
                Title = "Active without board",
                Status = GameStatusValue.Active,
                CreatedAtUtc = now,
                StartedAtUtc = now
            },
            new Game
            {
                Id = finishedGameId,
                Title = "Finished with board",
                Status = GameStatusValue.Finished,
                CreatedAtUtc = now.AddHours(-2),
                FinishedAtUtc = now.AddHours(-1)
            }
        );

        dbContext.GameBoards.Add(
            new GameBoard
            {
                Id = boardId,
                GameId = finishedGameId,
                Rows = 1,
                Cols = 1,
                RowLabels = ["A"],
                ColLabels = ["1"],
                CreatedAtUtc = now.AddHours(-1)
            }
        );

        dbContext.BoardCells.Add(
            new BoardCell
            {
                Id = Guid.NewGuid(),
                BoardId = boardId,
                RowIndex = 0,
                ColIndex = 0,
                Title = "Cell",
                Cost = 100,
                State = BoardCellState.Closed
            }
        );

        await dbContext.SaveChangesAsync();
        return finishedGameId;
    }

    private async Task<Guid> SeedSingleCellAsync(bool selectActiveTeam = true)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.GameActiveModifiers.RemoveRange(dbContext.GameActiveModifiers);
        dbContext.GameModifierSelections.RemoveRange(dbContext.GameModifierSelections);
        dbContext.GameCardRunModifierResults.RemoveRange(dbContext.GameCardRunModifierResults);
        dbContext.GameCardRunParticipants.RemoveRange(dbContext.GameCardRunParticipants);
        dbContext.GameCardRuns.RemoveRange(dbContext.GameCardRuns);
        dbContext.GameTeamMembers.RemoveRange(dbContext.GameTeamMembers);
        dbContext.GameTeams.RemoveRange(dbContext.GameTeams);
        dbContext.GameParticipationSlots.RemoveRange(dbContext.GameParticipationSlots);
        dbContext.BoardCells.RemoveRange(dbContext.BoardCells);
        dbContext.GameBoards.RemoveRange(dbContext.GameBoards);
        dbContext.Games.RemoveRange(dbContext.Games);
        dbContext.Users.RemoveRange(dbContext.Users);
        await dbContext.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var gameId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var cellId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        dbContext.Games.Add(
            new Game
            {
                Id = gameId,
                Title = "Game",
                Status = GameStatusValue.Active,
                ActiveTeamId = selectActiveTeam ? teamId : null,
                CreatedAtUtc = now,
                StartedAtUtc = now
            }
        );

        dbContext.Users.Add(
            new User
            {
                Id = userId,
                TwitchUserId = $"single-cell-user-{userId:N}",
                Login = "single-cell-user",
                DisplayName = "Single Cell Player",
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        );

        dbContext.GameParticipationSlots.Add(
            new GameParticipationSlot
            {
                Id = slotId,
                GameId = gameId,
                SlotIndex = 1,
                Availability = SlotAvailabilityValue.Public,
                CreatedAtUtc = now
            }
        );

        dbContext.GameTeams.Add(
            new GameTeam
            {
                Id = teamId,
                GameId = gameId,
                SlotId = slotId,
                RecruitmentOpen = false,
                Status = TeamStatusValue.Confirmed,
                CreatedByUserId = userId,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                ConfirmedAtUtc = now,
                ConfirmedByUserId = userId
            }
        );

        dbContext.GameTeamMembers.Add(
            new GameTeamMember
            {
                Id = Guid.NewGuid(),
                GameId = gameId,
                TeamId = teamId,
                UserId = userId,
                JoinedAtUtc = now
            }
        );

        dbContext.GameBoards.Add(
            new GameBoard
            {
                Id = boardId,
                GameId = gameId,
                Rows = 1,
                Cols = 1,
                RowLabels = ["A"],
                ColLabels = ["1"],
                CreatedAtUtc = now
            }
        );

        dbContext.BoardCells.Add(
            new BoardCell
            {
                Id = cellId,
                BoardId = boardId,
                RowIndex = 0,
                ColIndex = 0,
                Title = "Cell",
                Cost = 100,
                State = BoardCellState.Closed
            }
        );

        await dbContext.SaveChangesAsync();
        return cellId;
    }

    private async Task SeedActiveGameWithEnabledModifiersAsync(
        IReadOnlyList<string> enabledCodes,
        IReadOnlyList<string>? alreadyActiveCodes = null
    )
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.GameActiveModifiers.RemoveRange(dbContext.GameActiveModifiers);
        dbContext.GameModifierSelections.RemoveRange(dbContext.GameModifierSelections);
        dbContext.BoardCells.RemoveRange(dbContext.BoardCells);
        dbContext.GameBoards.RemoveRange(dbContext.GameBoards);
        dbContext.Games.RemoveRange(dbContext.Games);
        await dbContext.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var gameId = Guid.NewGuid();
        var boardId = Guid.NewGuid();

        dbContext.Games.Add(
            new Game
            {
                Id = gameId,
                Title = "Game with modifiers",
                Status = GameStatusValue.Active,
                CreatedAtUtc = now,
                StartedAtUtc = now
            }
        );
        dbContext.GameBoards.Add(
            new GameBoard
            {
                Id = boardId,
                GameId = gameId,
                Rows = 1,
                Cols = 1,
                RowLabels = ["A"],
                ColLabels = ["1"],
                CreatedAtUtc = now,
            }
        );
        dbContext.BoardCells.Add(
            new BoardCell
            {
                Id = Guid.NewGuid(),
                BoardId = boardId,
                RowIndex = 0,
                ColIndex = 0,
                Title = "Cell",
                Cost = 100,
                State = BoardCellState.Closed
            }
        );
        dbContext.GameModifierSelections.AddRange(
            enabledCodes.Select(
                code =>
                    new GameModifierSelection
                    {
                        GameId = gameId,
                        ModifierId = GetModifierId(code),
                        EnabledAtUtc = now
                    }
            )
        );
        dbContext.GameActiveModifiers.AddRange(
            (alreadyActiveCodes ?? [])
                .Select(
                    code =>
                        new GameActiveModifier
                        {
                            Id = Guid.NewGuid(),
                            GameId = gameId,
                            ModifierId = GetModifierId(code),
                            ActivatedByUserId = Guid.NewGuid(),
                            ActivatedAtUtc = now
                        }
                )
        );
        await dbContext.SaveChangesAsync();
    }

    private async Task SeedQuizPointsAsync(Guid userId, int points)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTime.UtcNow;
        var gameId = await dbContext.Games
            .Where(x => x.Status == GameStatusValue.Active && !x.IsDeleted)
            .Select(x => x.Id)
            .SingleAsync();

        if (!await dbContext.Users.AnyAsync(x => x.Id == userId))
        {
            dbContext.Users.Add(
                new User
                {
                    Id = userId,
                    TwitchUserId = $"modifier-user-{userId:N}",
                    Login = $"modifier-user-{userId:N}"[..32],
                    DisplayName = "Modifier Player",
                    IsActive = true,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                }
            );
        }

        if (points > 0)
        {
            dbContext.GameQuizManualAwards.Add(
                new GameQuizManualAward
                {
                    Id = Guid.NewGuid(),
                    GameId = gameId,
                    AwardedToUserId = userId,
                    AwardedByUserId = userId,
                    Points = points,
                    AwardedAtUtc = now
                }
            );
        }

        await dbContext.SaveChangesAsync();
    }

    private async Task EnsureModifierDefinitionsSeededAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var requiredModifierIds = new[]
        {
            ModifierDefinitionSeedIds.Chirik,
            ModifierDefinitionSeedIds.Zhazhda,
            ModifierDefinitionSeedIds.Prokaznik,
            ModifierDefinitionSeedIds.Mentorbait,
            ModifierDefinitionSeedIds.Feyerverk
        };

        var existingModifierIds = await dbContext.ModifierDefinitions
            .Where(x => requiredModifierIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync();

        if (existingModifierIds.Count == requiredModifierIds.Length)
        {
            return;
        }

        dbContext.ModifierConflicts.RemoveRange(dbContext.ModifierConflicts);
        dbContext.ModifierDefinitions.RemoveRange(dbContext.ModifierDefinitions);

        var now = DateTime.UtcNow;
        dbContext.ModifierDefinitions.AddRange(
            new ModifierDefinition
            {
                Id = ModifierDefinitionSeedIds.Chirik,
                Name = "Чирик",
                Description = "Test",
                ScoringType = "non_scoring",
                ActivationCost = 3,
                DefaultLimitPerGame = 5,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new ModifierDefinition
            {
                Id = ModifierDefinitionSeedIds.Zhazhda,
                Name = "Жажда",
                Description = "Test",
                ScoringType = "conditional_bonus_penalty",
                ActivationCost = 3,
                DefaultLimitPerGame = 2,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new ModifierDefinition
            {
                Id = ModifierDefinitionSeedIds.Prokaznik,
                Name = "Проказник",
                Description = "Test",
                ScoringType = "non_scoring",
                ActivationCost = 6,
                DefaultLimitPerGame = 2,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new ModifierDefinition
            {
                Id = ModifierDefinitionSeedIds.Mentorbait,
                Name = "Менторбайт",
                Description = "Test",
                ScoringType = "non_scoring",
                ActivationCost = 8,
                DefaultLimitPerGame = 1,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new ModifierDefinition
            {
                Id = ModifierDefinitionSeedIds.Feyerverk,
                Name = "Фейерверк",
                Description = "Test",
                ScoringType = "non_scoring",
                ActivationCost = 11,
                DefaultLimitPerGame = 1,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        );
        dbContext.ModifierConflicts.AddRange(
            new ModifierConflict
            {
                ModifierId = ModifierDefinitionSeedIds.Prokaznik,
                ConflictsWithModifierId = ModifierDefinitionSeedIds.Mentorbait
            },
            new ModifierConflict
            {
                ModifierId = ModifierDefinitionSeedIds.Mentorbait,
                ConflictsWithModifierId = ModifierDefinitionSeedIds.Prokaznik
            }
        );
        await dbContext.SaveChangesAsync();
    }

    private async Task<SeededModifierHistoryGame> SeedModifierHistoryGameAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.GameCardRunModifierResults.RemoveRange(dbContext.GameCardRunModifierResults);
        dbContext.GameCardRunParticipants.RemoveRange(dbContext.GameCardRunParticipants);
        dbContext.GameCardRuns.RemoveRange(dbContext.GameCardRuns);
        dbContext.GameQuestionRounds.RemoveRange(dbContext.GameQuestionRounds);
        dbContext.GameQuestionSelections.RemoveRange(dbContext.GameQuestionSelections);
        dbContext.QuestionDefinitions.RemoveRange(dbContext.QuestionDefinitions);
        dbContext.QuestionCategories.RemoveRange(dbContext.QuestionCategories);
        dbContext.GameActiveModifiers.RemoveRange(dbContext.GameActiveModifiers);
        dbContext.GameModifierSelections.RemoveRange(dbContext.GameModifierSelections);
        dbContext.ModifierConflicts.RemoveRange(dbContext.ModifierConflicts);
        dbContext.ModifierDefinitions.RemoveRange(dbContext.ModifierDefinitions);
        dbContext.BoardCells.RemoveRange(dbContext.BoardCells);
        dbContext.GameBoards.RemoveRange(dbContext.GameBoards);
        dbContext.Games.RemoveRange(dbContext.Games);
        dbContext.Users.RemoveRange(dbContext.Users);
        await dbContext.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var gameId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var cellId = Guid.NewGuid();
        var activationId = Guid.NewGuid();
        var modifierId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var moderatorId = Guid.NewGuid();
        var cardRunId = Guid.NewGuid();

        dbContext.Users.AddRange(
            new User
            {
                Id = playerId,
                TwitchUserId = "modifier-history-player",
                Login = "modifier-history-player",
                DisplayName = "Modifier History Player",
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new User
            {
                Id = moderatorId,
                TwitchUserId = "modifier-history-admin",
                Login = "modifier-history-admin",
                DisplayName = "Modifier History Admin",
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        );

        dbContext.Games.Add(
            new Game
            {
                Id = gameId,
                Title = "Modifier Applied Game",
                Status = GameStatusValue.Active,
                CreatedAtUtc = now.AddHours(-1),
                StartedAtUtc = now.AddMinutes(-45)
            }
        );

        dbContext.GameBoards.Add(
            new GameBoard
            {
                Id = boardId,
                GameId = gameId,
                Rows = 1,
                Cols = 1,
                RowLabels = ["A"],
                ColLabels = ["1"],
                Version = 1,
                CreatedAtUtc = now.AddHours(-1)
            }
        );

        dbContext.BoardCells.Add(
            new BoardCell
            {
                Id = cellId,
                BoardId = boardId,
                RowIndex = 0,
                ColIndex = 0,
                Title = "Applied Cell",
                Cost = 100,
                State = BoardCellState.Open
            }
        );

        dbContext.ModifierDefinitions.Add(
            new ModifierDefinition
            {
                Id = modifierId,
                Name = "Applied modifier",
                Description = "Used in round",
                ScoringType = GameModifierScoringTypes.NonScoring,
                Category = GameModifierCategories.Round,
                ActivationCost = 3,
                DefaultLimitPerGame = 1,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        );

        dbContext.GameModifierSelections.Add(
            new GameModifierSelection
            {
                GameId = gameId,
                ModifierId = modifierId,
                EnabledAtUtc = now.AddMinutes(-40)
            }
        );

        dbContext.GameActiveModifiers.Add(
            new GameActiveModifier
            {
                Id = activationId,
                GameId = gameId,
                ModifierId = modifierId,
                ActivatedByUserId = playerId,
                ActivationCostSnapshot = 3,
                ActivatedAtUtc = now.AddMinutes(-35)
            }
        );

        dbContext.GameCardRuns.Add(
            new GameCardRun
            {
                Id = cardRunId,
                GameId = gameId,
                BoardCellId = cellId,
                TeamId = Guid.NewGuid(),
                Status = GameCardRunStatusValue.Completed,
                StartedAtUtc = now.AddMinutes(-30),
                FinishedAtUtc = now.AddMinutes(-20),
                BaseScore = 100,
                FinalScore = 100,
                TeamSlotIndexSnapshot = 1,
                CellRowIndex = 0,
                CellColIndex = 0,
                CellTitleSnapshot = "Applied Cell",
                CellCostSnapshot = 100,
                ResolvedByUserId = moderatorId,
                CreatedAtUtc = now.AddMinutes(-30),
                UpdatedAtUtc = now.AddMinutes(-20)
            }
        );

        dbContext.GameCardRunModifierResults.Add(
            new GameCardRunModifierResult
            {
                Id = Guid.NewGuid(),
                CardRunId = cardRunId,
                GameActiveModifierId = activationId,
                ModifierId = modifierId,
                ModifierNameSnapshot = "Applied modifier",
                ModifierCategorySnapshot = GameModifierCategories.Round,
                ModifierMechanicTypeSnapshot = GameModifierMechanicTypes.RuleOnly,
                OutcomeStatus = "applied",
                ScoreDelta = 0,
                KillDelta = 0,
                ResolvedByUserId = moderatorId,
                ResolvedAtUtc = now.AddMinutes(-20),
                CreatedAtUtc = now.AddMinutes(-20),
                UpdatedAtUtc = now.AddMinutes(-20)
            }
        );

        await dbContext.SaveChangesAsync();
        return new SeededModifierHistoryGame(gameId, activationId);
    }

    private static Guid GetModifierId(string code) =>
        code switch
        {
            "chirik" => ModifierDefinitionSeedIds.Chirik,
            "zhazhda" => ModifierDefinitionSeedIds.Zhazhda,
            "prokaznik" => ModifierDefinitionSeedIds.Prokaznik,
            "mentorbait" => ModifierDefinitionSeedIds.Mentorbait,
            "feyerverk" => ModifierDefinitionSeedIds.Feyerverk,
            _ => throw new InvalidOperationException($"Unknown modifier seed code '{code}'.")
        };

    private static CreateGameModifierRequestDto CreateRuleOnlyModifierRequest(string name) =>
        new(
            name,
            "Rule-only modifier for integration tests.",
            GameModifierMechanicTypes.RuleOnly,
            GameModifierCategories.Round,
            false,
            5,
            new GameModifierActivationLimitDto(1),
            new GameModifierEffectDto(
                GameModifierMechanicTypes.RuleOnly,
                [],
                null,
                null,
                null,
                [],
                [],
                null,
                null,
                null
            ),
            [],
            1,
            GameModifierScoringTypes.NonScoring,
            null,
            null
        );

    private async Task SeedActiveGameForQuestionsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.GameQuestionRounds.RemoveRange(dbContext.GameQuestionRounds);
        dbContext.GameQuestionSelections.RemoveRange(dbContext.GameQuestionSelections);
        dbContext.QuestionDefinitions.RemoveRange(dbContext.QuestionDefinitions);
        dbContext.QuestionCategories.RemoveRange(dbContext.QuestionCategories);
        dbContext.GameActiveModifiers.RemoveRange(dbContext.GameActiveModifiers);
        dbContext.GameModifierSelections.RemoveRange(dbContext.GameModifierSelections);
        dbContext.BoardCells.RemoveRange(dbContext.BoardCells);
        dbContext.GameBoards.RemoveRange(dbContext.GameBoards);
        dbContext.Games.RemoveRange(dbContext.Games);
        await dbContext.SaveChangesAsync();

        var now = DateTime.UtcNow;
        dbContext.Games.Add(
            new Game
            {
                Id = Guid.NewGuid(),
                Title = "Questions Active Game",
                Status = GameStatusValue.Active,
                CreatedAtUtc = now,
                StartedAtUtc = now
            }
        );
        await dbContext.SaveChangesAsync();
    }

    private async Task SeedQuestionCatalogWithQuestionsAsync(IReadOnlyList<SeedQuestionItem> questions)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.GameQuestionRounds.RemoveRange(dbContext.GameQuestionRounds);
        dbContext.QuestionDefinitions.RemoveRange(dbContext.QuestionDefinitions);
        dbContext.QuestionCategories.RemoveRange(dbContext.QuestionCategories);
        await dbContext.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var categories = questions
            .Select(question => question.Category)
            .Distinct(StringComparer.Ordinal)
            .Select(
                category =>
                    new QuestionCategory
                    {
                        Id = Guid.NewGuid(),
                        Name = category,
                        CreatedAtUtc = now,
                        UpdatedAtUtc = now
                    }
            )
            .ToArray();
        dbContext.QuestionCategories.AddRange(categories);
        var categoryIdByName = categories.ToDictionary(
            category => category.Name,
            category => category.Id,
            StringComparer.Ordinal
        );

        var priority = 1;
        var seeded = new List<QuestionDefinition>();
        foreach (var question in questions)
        {
            var definition = new QuestionDefinition
            {
                Id = Guid.NewGuid(),
                ExternalCode = question.QuestionCode,
                CategoryId = categoryIdByName[question.Category],
                Text = question.Text,
                Answer = question.Answer,
                NormalizedAnswer = NormalizeAnswer(question.Answer),
                Reward = question.Reward,
                IsEnabled = true,
                Priority = question.Priority ?? priority++,
                AskedTotalCount = 0,
                CorrectTotalCount = 0,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            dbContext.QuestionDefinitions.Add(definition);
            seeded.Add(definition);
        }

        await dbContext.SaveChangesAsync();

        // Questions only become askable once selected for the active game
        // (per-game question selection). Mirror that here so ask-next tests
        // exercise a fully-configured active game.
        var activeGameId = await dbContext.Games
            .Where(game => game.Status == GameStatusValue.Active && !game.IsDeleted)
            .Select(game => (Guid?)game.Id)
            .FirstOrDefaultAsync();
        if (activeGameId is Guid gameId)
        {
            foreach (var definition in seeded)
            {
                dbContext.GameQuestionSelections.Add(
                    new GameQuestionSelection
                    {
                        GameId = gameId,
                        QuestionId = definition.Id,
                        EnabledAtUtc = now
                    }
                );
            }

            await dbContext.SaveChangesAsync();
        }
    }

    private static string NormalizeAnswer(string answer)
    {
        return string.Join(
            " ",
            answer.Trim().ToLowerInvariant().Replace('ё', 'е').Split(' ', StringSplitOptions.RemoveEmptyEntries)
        );
    }

    private sealed record SeedQuestionItem(
        string QuestionCode,
        string Category,
        string Text,
        string Answer,
        int Reward,
        int? Priority = null
    );

    private sealed record SeededModifierHistoryGame(Guid GameId, Guid ActivationId);

    private static MultipartFormDataContent CreateJsonImportContent(string content, string fileName)
    {
        var multipart = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        multipart.Add(fileContent, "file", fileName);
        return multipart;
    }

    private async Task SeedInactiveUserAsync(Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Users.Add(
            new User
            {
                Id = userId,
                TwitchUserId = $"inactive-{userId:N}",
                Login = "inactive-viewer",
                DisplayName = "Inactive Viewer",
                IsActive = false,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            }
        );
        await dbContext.SaveChangesAsync();
    }

    private HttpClient CreateAuthenticatedClient(
        string[] roles,
        Guid? userId = null,
        RecordingGameBoardEventsPublisher? publisher = null,
        IGameBoardService? gameBoardService = null
    )
    {
        var authenticatedFactory = CreateAuthenticatedFactory(roles, userId, publisher, gameBoardService);
        return authenticatedFactory.CreateClient();
    }

    private WebApplicationFactory<Program> CreateAuthenticatedFactory(
        string[] roles,
        Guid? userId = null,
        RecordingGameBoardEventsPublisher? publisher = null,
        IGameBoardService? gameBoardService = null
    )
    {
        var authenticatedFactory = _factory.WithWebHostBuilder(
            builder =>
                builder.ConfigureServices(
                    services =>
                    {
                        services
                            .RemoveAll<IClaimsTransformation>();
                        services.AddSingleton<IClaimsTransformation, PassthroughClaimsTransformation>();
                        if (publisher is not null)
                        {
                            services.RemoveAll<IGameBoardEventsPublisher>();
                            services.AddSingleton<IGameBoardEventsPublisher>(publisher);
                        }

                        if (gameBoardService is not null)
                        {
                            services.RemoveAll<IGameBoardService>();
                            services.AddSingleton(gameBoardService);
                        }

                        services
                            .AddAuthentication(options =>
                            {
                                options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                                options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
                                options.DefaultScheme = TestAuthenticationHandler.SchemeName;
                            })
                            .AddScheme<TestAuthSchemeOptions, TestAuthenticationHandler>(
                                TestAuthenticationHandler.SchemeName,
                                options =>
                                {
                                    options.ClaimsIssuer = string.Join(',', roles);
                                    options.UserId = userId;
                                }
                            );
                    }
                )
        );

        return authenticatedFactory;
    }

    private sealed class TestAuthSchemeOptions : AuthenticationSchemeOptions
    {
        public Guid? UserId { get; set; }
    }

    private sealed class TestAuthenticationHandler : AuthenticationHandler<TestAuthSchemeOptions>
    {
        public const string SchemeName = "TestAuth";

        public TestAuthenticationHandler(
            IOptionsMonitor<TestAuthSchemeOptions> options,
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
            var userId = Options.UserId ?? Guid.NewGuid();
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId.ToString()) };
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var identity = new ClaimsIdentity(
                claims,
                SchemeName
            );
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

    private sealed class RecordingGameBoardEventsPublisher : IGameBoardEventsPublisher
    {
        public List<GameCellOpenedEvent> PublishedEvents { get; } = [];
        public List<GameModifierActivatedEvent> PublishedModifierEvents { get; } = [];
        public List<GameModifierActivationCancelledEvent> PublishedModifierCancelledEvents { get; } =
            [];
        public List<GameUserNotificationCreatedEvent> PublishedUserNotificationEvents { get; } = [];

        public Task PublishCellOpenedAsync(
            GameCellOpenedEvent @event,
            CancellationToken cancellationToken = default
        )
        {
            PublishedEvents.Add(@event);
            return Task.CompletedTask;
        }

        public Task PublishModifierActivatedAsync(
            GameModifierActivatedEvent @event,
            CancellationToken cancellationToken = default
        )
        {
            PublishedModifierEvents.Add(@event);
            return Task.CompletedTask;
        }

        public Task PublishModifierActivationCancelledAsync(
            GameModifierActivationCancelledEvent @event,
            CancellationToken cancellationToken = default
        )
        {
            PublishedModifierCancelledEvents.Add(@event);
            return Task.CompletedTask;
        }

        public Task PublishUserNotificationCreatedAsync(
            GameUserNotificationCreatedEvent @event,
            CancellationToken cancellationToken = default
        )
        {
            PublishedUserNotificationEvents.Add(@event);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingGameBoardService : IGameBoardService
    {
        public Task<GameBoardSnapshot?> GetCurrentBoardAsync(CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Simulated game board failure.");

        public Task<IReadOnlyList<GameTeamQueueItem>> GetCurrentTeamQueueAsync(
            CancellationToken cancellationToken = default
        ) => throw new InvalidOperationException("Simulated game board failure.");

        public Task<SetActiveGameTeamOutcome> SetCurrentActiveTeamAsync(
            Guid? teamId,
            CancellationToken cancellationToken = default
        ) => throw new InvalidOperationException("Simulated game board failure.");

        public Task<bool> CurrentActiveGameHasSelectedTeamAsync(
            CancellationToken cancellationToken = default
        ) => throw new InvalidOperationException("Simulated game board failure.");

        public Task<bool> CurrentActiveGameHasActiveRoundAsync(
            CancellationToken cancellationToken = default
        ) => throw new InvalidOperationException("Simulated game board failure.");

        public Task<bool> IsCurrentActiveGameCellAsync(
            Guid cellId,
            CancellationToken cancellationToken = default
        ) => throw new InvalidOperationException("Simulated game board failure.");

        public Task<OpenGameCellResult?> TryOpenCellAsync(
            Guid cellId,
            CancellationToken cancellationToken = default
        ) => throw new InvalidOperationException("Simulated game board failure.");
    }
}
