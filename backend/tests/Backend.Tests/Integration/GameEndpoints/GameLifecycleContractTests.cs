using System.Net;
using System.Net.Http.Json;
using backend.Api.Contracts;
using backend.Application.Abstractions.Auth;
using backend.Application.Abstractions;
using backend.Application.Contracts;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.Persistence;
using backend.Messaging;
using Backend.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Backend.Tests.Integration.GameEndpoints;

public sealed class GameLifecycleContractTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public GameLifecycleContractTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task OpenRegistration_WhenAnonymous_ReturnsUnauthorized()
    {
        var response = await _client.PostAsync("/api/game/lifecycle/open-registration", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task OpenRegistration_WhenNoDraft_ReturnsNotFound()
    {
        await ClearGamesAsync();
        using var adminClient = TestAuthClientFactory.CreateClient(_factory, [AuthRoleCodes.Admin]);

        var response = await adminClient.PostAsync("/api/game/lifecycle/open-registration", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.NoDraftGameForSetup, payload.Error);
        Assert.Equal(AppMessages.ErrorCodes.GameLifecycleDraftNotFound, payload.Code);
    }

    [Fact]
    public async Task OpenRegistration_WhenDraftExists_MovesGameToReady()
    {
        await ClearGamesAsync();
        using var adminClient = TestAuthClientFactory.CreateClient(_factory, [AuthRoleCodes.Admin]);
        var setupResponse = await adminClient.PostAsJsonAsync(
            "/api/game/setup",
            new CreateGameSetupRequestDto("Draft for registration")
        );
        Assert.Equal(HttpStatusCode.Created, setupResponse.StatusCode);

        var response = await adminClient.PostAsync("/api/game/lifecycle/open-registration", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GameLifecycleStateDto>();
        Assert.NotNull(payload);
        Assert.Equal(GameStatusValue.Ready, payload.Status);
    }

    [Fact]
    public async Task Start_WhenReadyGameExists_MovesGameToActive()
    {
        await ClearGamesAsync();
        using var adminClient = TestAuthClientFactory.CreateClient(_factory, [AuthRoleCodes.Admin]);
        var setupResponse = await adminClient.PostAsJsonAsync(
            "/api/game/setup",
            new CreateGameSetupRequestDto("Ready to start")
        );
        Assert.Equal(HttpStatusCode.Created, setupResponse.StatusCode);

        var openResponse = await adminClient.PostAsync("/api/game/lifecycle/open-registration", content: null);
        Assert.Equal(HttpStatusCode.OK, openResponse.StatusCode);
        await SeedTeamForReadyGameAsync(TeamStatusValue.Confirmed, memberCount: 1);

        var response = await adminClient.PostAsync("/api/game/lifecycle/start", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GameLifecycleStateDto>();
        Assert.NotNull(payload);
        Assert.Equal(GameStatusValue.Active, payload.Status);
    }

    [Fact]
    public async Task Finish_WhenActiveGameExists_MovesGameToFinished()
    {
        await ClearGamesAsync();
        using var adminClient = TestAuthClientFactory.CreateClient(_factory, [AuthRoleCodes.Admin]);
        await adminClient.PostAsJsonAsync("/api/game/setup", new CreateGameSetupRequestDto("Active round"));
        await adminClient.PostAsync("/api/game/lifecycle/open-registration", content: null);
        await SeedTeamForReadyGameAsync(TeamStatusValue.Confirmed, memberCount: 1);
        await adminClient.PostAsync("/api/game/lifecycle/start", content: null);

        var response = await adminClient.PostAsync("/api/game/lifecycle/finish", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GameLifecycleStateDto>();
        Assert.NotNull(payload);
        Assert.Equal(GameStatusValue.Finished, payload.Status);
    }

    [Fact]
    public async Task ArchiveGame_WhenFinishedGameExists_SoftDeletesGameAndReturnsNoContent()
    {
        await ClearGamesAsync();
        using var adminClient = TestAuthClientFactory.CreateClient(_factory, [AuthRoleCodes.Admin]);
        await adminClient.PostAsJsonAsync("/api/game/setup", new CreateGameSetupRequestDto("Archive me"));
        await adminClient.PostAsync("/api/game/lifecycle/open-registration", content: null);
        await SeedTeamForReadyGameAsync(TeamStatusValue.Confirmed, memberCount: 1);
        await adminClient.PostAsync("/api/game/lifecycle/start", content: null);
        var finishResponse = await adminClient.PostAsync("/api/game/lifecycle/finish", content: null);
        Assert.Equal(HttpStatusCode.OK, finishResponse.StatusCode);
        var finished = await finishResponse.Content.ReadFromJsonAsync<GameLifecycleStateDto>();
        Assert.NotNull(finished);

        var archiveResponse = await adminClient.DeleteAsync($"/api/game/lifecycle/games/{finished!.GameId}");
        Assert.Equal(HttpStatusCode.NoContent, archiveResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var game = await db.Games.FirstAsync(x => x.Id == finished.GameId);
        Assert.True(game.IsDeleted);
        Assert.NotNull(game.DeletedAtUtc);
    }

    [Fact]
    public async Task ArchiveGame_WhenDraftGamePassed_ReturnsConflict()
    {
        await ClearGamesAsync();
        using var adminClient = TestAuthClientFactory.CreateClient(_factory, [AuthRoleCodes.Admin]);
        var setupResponse = await adminClient.PostAsJsonAsync(
            "/api/game/setup",
            new CreateGameSetupRequestDto("Draft should hard-delete only")
        );
        Assert.Equal(HttpStatusCode.Created, setupResponse.StatusCode);
        var setup = await setupResponse.Content.ReadFromJsonAsync<GameSetupSnapshotDto>();
        Assert.NotNull(setup);

        var archiveResponse = await adminClient.DeleteAsync($"/api/game/lifecycle/games/{setup!.GameId}");
        Assert.Equal(HttpStatusCode.Conflict, archiveResponse.StatusCode);
        var payload = await archiveResponse.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.ErrorCodes.GameLifecycleDraftDeleteNotAllowed, payload.Code);
    }

    [Fact]
    public async Task Start_WhenNoReadyGame_ReturnsNotFound()
    {
        await ClearGamesAsync();
        using var adminClient = TestAuthClientFactory.CreateClient(_factory, [AuthRoleCodes.Admin]);

        var response = await adminClient.PostAsync("/api/game/lifecycle/start", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.GameNotReadyForStart, payload.Error);
        Assert.Equal(AppMessages.ErrorCodes.GameLifecycleGameNotReady, payload.Code);
    }

    [Fact]
    public async Task Start_WhenNoConfirmedTeams_ReturnsConflict()
    {
        await ClearGamesAsync();
        using var adminClient = TestAuthClientFactory.CreateClient(_factory, [AuthRoleCodes.Admin]);
        await adminClient.PostAsJsonAsync("/api/game/setup", new CreateGameSetupRequestDto("No teams"));
        await adminClient.PostAsync("/api/game/lifecycle/open-registration", content: null);

        var response = await adminClient.PostAsync("/api/game/lifecycle/start", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.GameLifecycleNoConfirmedTeams, payload.Error);
        Assert.Equal(AppMessages.ErrorCodes.GameLifecycleNoConfirmedTeams, payload.Code);
    }

    [Fact]
    public async Task Start_WhenFormingTeamExists_ReturnsConflict()
    {
        await ClearGamesAsync();
        using var adminClient = TestAuthClientFactory.CreateClient(_factory, [AuthRoleCodes.Admin]);
        await adminClient.PostAsJsonAsync("/api/game/setup", new CreateGameSetupRequestDto("Forming team"));
        await adminClient.PostAsync("/api/game/lifecycle/open-registration", content: null);
        await SeedTeamForReadyGameAsync(TeamStatusValue.Confirmed, memberCount: 1, slotIndex: 1);
        await SeedTeamForReadyGameAsync(TeamStatusValue.Forming, memberCount: 1, slotIndex: 2);

        var response = await adminClient.PostAsync("/api/game/lifecycle/start", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.GameLifecycleUnconfirmedTeams, payload.Error);
        Assert.Equal(AppMessages.ErrorCodes.GameLifecycleUnconfirmedTeams, payload.Code);
    }

    [Fact]
    public async Task Start_WhenPendingInvitationExists_ReturnsConflict()
    {
        await ClearGamesAsync();
        using var adminClient = TestAuthClientFactory.CreateClient(_factory, [AuthRoleCodes.Admin]);
        await adminClient.PostAsJsonAsync("/api/game/setup", new CreateGameSetupRequestDto("Pending invite"));
        await adminClient.PostAsync("/api/game/lifecycle/open-registration", content: null);
        var teamId = await SeedTeamForReadyGameAsync(TeamStatusValue.Confirmed, memberCount: 1);
        await SeedPendingInvitationForReadyGameAsync(teamId);

        var response = await adminClient.PostAsync("/api/game/lifecycle/start", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.GameLifecyclePendingInvitations, payload.Error);
        Assert.Equal(AppMessages.ErrorCodes.GameLifecyclePendingInvitations, payload.Code);
    }

    [Fact]
    public async Task Start_WhenDisbandRequestExists_ReturnsConflict()
    {
        await ClearGamesAsync();
        using var adminClient = TestAuthClientFactory.CreateClient(_factory, [AuthRoleCodes.Admin]);
        await adminClient.PostAsJsonAsync("/api/game/setup", new CreateGameSetupRequestDto("Disband request"));
        await adminClient.PostAsync("/api/game/lifecycle/open-registration", content: null);
        await SeedTeamForReadyGameAsync(
            TeamStatusValue.Confirmed,
            memberCount: 1,
            disbandRequested: true
        );

        var response = await adminClient.PostAsync("/api/game/lifecycle/start", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.GameLifecyclePendingDisbandRequests, payload.Error);
        Assert.Equal(AppMessages.ErrorCodes.GameLifecyclePendingDisbandRequests, payload.Code);
    }

    [Fact]
    public async Task Start_WhenConfirmedTeamRosterInvalid_ReturnsConflict()
    {
        await ClearGamesAsync();
        using var adminClient = TestAuthClientFactory.CreateClient(_factory, [AuthRoleCodes.Admin]);
        await adminClient.PostAsJsonAsync("/api/game/setup", new CreateGameSetupRequestDto("Invalid roster"));
        await adminClient.PostAsync("/api/game/lifecycle/open-registration", content: null);
        await SeedTeamForReadyGameAsync(TeamStatusValue.Confirmed, memberCount: 0);

        var response = await adminClient.PostAsync("/api/game/lifecycle/start", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.GameLifecycleInvalidConfirmedTeamRoster, payload.Error);
        Assert.Equal(AppMessages.ErrorCodes.GameLifecycleInvalidConfirmedTeamRoster, payload.Code);
    }

    [Fact]
    public async Task OpenRegistration_WhenServiceThrows_ReturnsInternalServerErrorPayload()
    {
        using var adminClient = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Admin],
            configureServices: services =>
            {
                services.RemoveAll<IGameLifecycleService>();
                services.AddSingleton<IGameLifecycleService>(new ThrowingGameLifecycleService());
            }
        );

        var response = await adminClient.PostAsync("/api/game/lifecycle/open-registration", content: null);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.UnexpectedServerError, payload.Error);
        Assert.Equal(AppMessages.ErrorCodes.UnexpectedServerError, payload.Code);
        Assert.False(string.IsNullOrWhiteSpace(payload.RequestId));
    }

    [Fact]
    public async Task OpenRegistration_WhenParallelRequests_ProducesOkAndHandledError()
    {
        await ClearGamesAsync();
        using var adminClient = TestAuthClientFactory.CreateClient(_factory, [AuthRoleCodes.Admin]);
        var setupResponse = await adminClient.PostAsJsonAsync(
            "/api/game/setup",
            new CreateGameSetupRequestDto("Parallel lifecycle draft")
        );
        Assert.Equal(HttpStatusCode.Created, setupResponse.StatusCode);

        var firstOpen = adminClient.PostAsync("/api/game/lifecycle/open-registration", content: null);
        var secondOpen = adminClient.PostAsync("/api/game/lifecycle/open-registration", content: null);

        var responses = await Task.WhenAll(firstOpen, secondOpen);
        var statuses = responses.Select(response => response.StatusCode).ToArray();

        Assert.All(
            statuses,
            status =>
                Assert.Contains(
                    status,
                    new[] { HttpStatusCode.OK, HttpStatusCode.Conflict, HttpStatusCode.NotFound }
                )
        );
        Assert.DoesNotContain(HttpStatusCode.InternalServerError, statuses);
    }

    private async Task ClearGamesAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.GameTeamInvitations.RemoveRange(dbContext.GameTeamInvitations);
        dbContext.GameTeamMembers.RemoveRange(dbContext.GameTeamMembers);
        dbContext.GameTeams.RemoveRange(dbContext.GameTeams);
        dbContext.GameTeamSlots.RemoveRange(dbContext.GameTeamSlots);
        dbContext.BoardCellMedia.RemoveRange(dbContext.BoardCellMedia);
        dbContext.BoardCells.RemoveRange(dbContext.BoardCells);
        dbContext.GameBoards.RemoveRange(dbContext.GameBoards);
        dbContext.Games.RemoveRange(dbContext.Games);
        await dbContext.SaveChangesAsync();
    }

    private async Task<Guid> SeedTeamForReadyGameAsync(
        string status,
        int memberCount,
        int slotIndex = 1,
        bool disbandRequested = false
    )
    {
        var gameId = await GetReadyGameIdAsync();
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var slot = await dbContext.GameTeamSlots
            .FirstAsync(slot => slot.GameId == gameId && slot.SlotIndex == slotIndex);
        var utcNow = DateTime.UtcNow;
        var teamId = Guid.NewGuid();
        var createdByUserId = Guid.NewGuid();

        dbContext.Users.Add(CreateUser(createdByUserId, "team-owner"));
        dbContext.GameTeams.Add(
            new GameTeam
            {
                Id = teamId,
                GameId = gameId,
                SlotId = slot.Id,
                RecruitmentOpen = status == TeamStatusValue.Forming,
                Status = status,
                CreatedByUserId = createdByUserId,
                CreatedAtUtc = utcNow,
                UpdatedAtUtc = utcNow,
                ConfirmedAtUtc = status == TeamStatusValue.Confirmed ? utcNow : null,
                ConfirmedByUserId = status == TeamStatusValue.Confirmed ? createdByUserId : null,
                DisbandRequestedAtUtc = disbandRequested ? utcNow : null,
                DisbandRequestedByUserId = disbandRequested ? createdByUserId : null
            }
        );

        for (var index = 0; index < memberCount; index++)
        {
            var userId = index == 0 ? createdByUserId : Guid.NewGuid();
            if (index > 0)
            {
                dbContext.Users.Add(CreateUser(userId, $"team-member-{index}"));
            }

            dbContext.GameTeamMembers.Add(
                new GameTeamMember
                {
                    Id = Guid.NewGuid(),
                    GameId = gameId,
                    TeamId = teamId,
                    UserId = userId,
                    JoinedAtUtc = utcNow
                }
            );
        }

        await dbContext.SaveChangesAsync();
        return teamId;
    }

    private async Task SeedPendingInvitationForReadyGameAsync(Guid teamId)
    {
        var gameId = await GetReadyGameIdAsync();
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var team = await dbContext.GameTeams.FirstAsync(team => team.Id == teamId);
        var invitedByUserId = team.CreatedByUserId ?? Guid.NewGuid();
        var invitedUserId = Guid.NewGuid();
        var utcNow = DateTime.UtcNow;

        dbContext.Users.Add(CreateUser(invitedUserId, "pending-invite"));
        dbContext.GameTeamInvitations.Add(
            new GameTeamInvitation
            {
                Id = Guid.NewGuid(),
                GameId = gameId,
                SlotId = team.SlotId,
                TeamId = teamId,
                InvitedByUserId = invitedByUserId,
                InvitedUserId = invitedUserId,
                InvitedByKind = InvitedByKindValue.Admin,
                Status = TeamInvitationStatusValue.Pending,
                CreatedAtUtc = utcNow
            }
        );
        await dbContext.SaveChangesAsync();
    }

    private async Task<Guid> GetReadyGameIdAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await dbContext.Games
            .Where(game => game.Status == GameStatusValue.Ready && !game.IsDeleted)
            .Select(game => game.Id)
            .FirstAsync();
    }

    private static User CreateUser(Guid userId, string login) =>
        new()
        {
            Id = userId,
            TwitchUserId = userId.ToString("N"),
            Login = login,
            DisplayName = login,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

    private sealed class ThrowingGameLifecycleService : IGameLifecycleService
    {
        public Task<GameLifecycleResult> OpenRegistrationAsync(CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Simulated lifecycle failure.");

        public Task<GameLifecycleResult> StartGameAsync(CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Simulated lifecycle failure.");

        public Task<GameLifecycleResult> FinishGameAsync(CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Simulated lifecycle failure.");

        public Task<GameLifecycleResult> ArchiveGameAsync(
            Guid gameId,
            CancellationToken cancellationToken = default
        ) => throw new InvalidOperationException("Simulated lifecycle failure.");
    }
}
